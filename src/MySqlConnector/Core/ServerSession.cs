using System.Buffers;
using System.Buffers.Text;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipes;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using MySqlConnector.Authentication;
using MySqlConnector.Logging;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable

internal sealed partial class ServerSession : IServerCapabilities
{
	public ServerSession(ILogger logger, IConnectionPoolMetadata pool)
	{
		m_logger = logger;
		m_lock = new();
		m_payloadCache = new();
		Id = pool.Id + "." + pool.GetNewSessionId();
		ServerVersion = ServerVersion.Empty;
		CreatedTimestamp = Stopwatch.GetTimestamp();
		Pool = pool.ConnectionPool;
		PoolGeneration = pool.Generation;
		HostName = "";
		m_activityTags = [];
		DataReader = new();
		Log.CreatedNewSession(m_logger, Id);
	}

	public string Id { get; }
	public ServerVersion ServerVersion { get; set; }
	public bool SupportsPerQueryVariables => ServerVersion.IsMariaDb && ServerVersion.Version >= ServerVersions.MariaDbSupportsPerQueryVariables;
	public int ActiveCommandId { get; private set; }
	public int CancellationTimeout { get; private set; }
	public int ConnectionId { get; set; }
	public byte[]? AuthPluginData { get; set; }
	public long CreatedTimestamp { get; }
	public ConnectionPool? Pool { get; }
	public int PoolGeneration { get; }
	public long LastLeasedTimestamp { get; set; }
	public long LastReturnedTimestamp { get; private set; }
	public string? DatabaseOverride { get; set; }
	public string HostName { get; private set; }
	public IPEndPoint? IPEndPoint => m_tcpClient?.Client.RemoteEndPoint as IPEndPoint;
	public string? UserID { get; private set; }
	public WeakReference<MySqlConnection>? OwningConnection { get; set; }
	public bool SupportsDeprecateEof { get; private set; }
	public bool SupportsCachedPreparedMetadata { get; private set; }
	public bool SupportsQueryAttributes { get; private set; }
	public bool SupportsSessionTrack { get; private set; }
	public bool ProcAccessDenied { get; set; }
	public ICollection<KeyValuePair<string, object?>> ActivityTags => m_activityTags;
	public MySqlDataReader DataReader { get; set; }
	public MySqlConnectionOpenedConditions Conditions { get; private set; }

	public ValueTask ReturnToPoolAsync(IOBehavior ioBehavior, MySqlConnection? owningConnection)
	{
		Log.ReturningToPool(m_logger, Id, Pool?.Id ?? 0);
		Conditions = MySqlConnectionOpenedConditions.None;
		LastReturnedTimestamp = Stopwatch.GetTimestamp();
		if (Pool is null)
			return default;

		// if the payload cache is too large, discard it rather than keeping it alive in the pool; see https://github.com/mysql-net/MySqlConnector/issues/1587
		if (m_payloadCache.Array?.Length > ProtocolUtility.MaxPacketSize + 1)
			m_payloadCache.ArraySegment = default;

		MetricsReporter.RecordUseTime(Pool, Utility.GetElapsedSeconds(LastLeasedTimestamp, LastReturnedTimestamp));
		LastLeasedTimestamp = 0;
		return Pool.ReturnAsync(ioBehavior, this);
	}

	public bool IsConnected
	{
		get
		{
			lock (m_lock)
				return m_state == State.Connected;
		}
	}

	public bool TryStartCancel(ICancellableCommand command)
	{
		lock (m_lock)
		{
			if (ActiveCommandId != command.CommandId)
				return false;
			VerifyState(State.Querying, State.CancelingQuery, State.ClearingPendingCancellation, State.Closing, State.Closed, State.Failed);
			if (m_state != State.Querying)
				return false;
			if (command.CancelAttemptCount++ >= 10)
				return false;
			m_state = State.CancelingQuery;
		}

		Log.WillCancelCommand(m_logger, Id, command.CommandId, command.CancelAttemptCount, (command as MySqlCommand)?.CommandText);
		return true;
	}

	public void DoCancel(ICancellableCommand commandToCancel, MySqlCommand killCommand)
	{
		Log.CancelingCommandFromSession(m_logger, Id, commandToCancel.CommandId, killCommand.Connection!.Session.Id, (commandToCancel as MySqlCommand)?.CommandText);
		lock (m_lock)
		{
			if (ActiveCommandId != commandToCancel.CommandId)
			{
				Log.IgnoringCancellationForInactiveCommand(m_logger, Id, ActiveCommandId, commandToCancel.CommandId);
				return;
			}

			// NOTE: This command is executed while holding the lock to prevent race conditions during asynchronous cancellation.
			// For example, if the lock weren't held, the current command could finish and the other thread could set ActiveCommandId
			// to zero, then start executing a new command. By the time this "KILL QUERY" command reached the server, the wrong
			// command would be killed (because "KILL QUERY" specifies the connection whose command should be killed, not
			// a unique identifier of the command itself). As a mitigation, we set the CommandTimeout to a low value to avoid
			// blocking the other thread for an extended duration.
			Log.CancelingCommand(m_logger, killCommand.Connection!.Session.Id, commandToCancel.CommandId, killCommand.CommandText);
			_ = killCommand.ExecuteNonQuery();
		}
	}

	public void AbortCancel(ICancellableCommand command)
	{
		lock (m_lock)
		{
			if (ActiveCommandId == command.CommandId && m_state == State.CancelingQuery)
				m_state = State.Querying;
		}
	}

	public bool IsCancelingQuery => m_state == State.CancelingQuery;

	public async Task PrepareAsync(IMySqlCommand command, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// caller has validated this already
		var commandText = command.CommandText!;

		// for a stored procedure, the statement to be prepared is "CALL commandText(?,?,?,...);"
		string commandToPrepare;
		if (command.CommandType == CommandType.StoredProcedure)
		{
			var cachedProcedure = await command.Connection!.GetCachedProcedure(commandText, revalidateMissing: false, ioBehavior, cancellationToken).ConfigureAwait(false);
			if (cachedProcedure is null)
			{
				var name = NormalizedSchema.MustNormalize(command.CommandText!, command.Connection.Database);
				throw new MySqlException($"Procedure or function '{name.Component}' cannot be found in database '{name.Schema}'.");
			}

			var parameterCount = cachedProcedure.Parameters.Count;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
			commandToPrepare = string.Create(commandText.Length + 7 + (parameterCount * 2) + (parameterCount == 0 ? 1 : 0), (commandText, parameterCount), static (buffer, state) =>
			{
				buffer[0] = 'C';
				buffer[1] = 'A';
				buffer[2] = 'L';
				buffer[3] = 'L';
				buffer[4] = ' ';
				buffer = buffer[5..];
				state.commandText.AsSpan().CopyTo(buffer);
				buffer = buffer[state.commandText.Length..];
				buffer[0] = '(';
				buffer = buffer[1..];
				if (state.parameterCount > 0)
				{
					buffer[0] = '?';
					buffer = buffer[1..];
					for (var i = 1; i < state.parameterCount; i++)
					{
						buffer[0] = ',';
						buffer[1] = '?';
						buffer = buffer[2..];
					}
				}
				buffer[0] = ')';
				buffer[1] = ';';
			});
#else
			var callStatement = new StringBuilder("CALL ", commandText.Length + 8 + (parameterCount * 2));
			callStatement.Append(commandText);
			callStatement.Append('(');
			for (int i = 0; i < parameterCount; i++)
				callStatement.Append("?,");
			if (parameterCount == 0)
				callStatement.Append(')');
			else
				callStatement[^1] = ')';
			callStatement.Append(';');
			commandToPrepare = callStatement.ToString();
#endif
		}
		else
		{
			commandToPrepare = commandText;
		}

		var statementPreparer = new StatementPreparer(commandToPrepare, command.RawParameters, command.CreateStatementPreparerOptions());
		var parsedStatements = statementPreparer.SplitStatements();

		var columnsAndParameters = new ResizableArray<byte>();
		var columnsAndParametersSize = 0;

		var preparedStatements = new List<PreparedStatement>(parsedStatements.Statements.Count);
		foreach (var statement in parsedStatements.Statements)
		{
			await SendAsync(new PayloadData(statement.StatementBytes), ioBehavior, cancellationToken).ConfigureAwait(false);
			PayloadData payload;
			try
			{
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			}
			catch (MySqlException ex)
			{
				ThrowIfStatementContainsDelimiter(ex, command);
				throw;
			}

			var response = StatementPrepareResponsePayload.Create(payload.Span);

			ColumnDefinitionPayload[]? parameters = null;
			if (response.ParameterCount > 0)
			{
				parameters = new ColumnDefinitionPayload[response.ParameterCount];
				for (var i = 0; i < response.ParameterCount; i++)
				{
					payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					var payloadLength = payload.Span.Length;
					Utility.Resize(ref columnsAndParameters, columnsAndParametersSize + payloadLength);
					payload.Span.CopyTo(columnsAndParameters.AsSpan(columnsAndParametersSize));
					ColumnDefinitionPayload.Initialize(ref parameters[i], new(columnsAndParameters, columnsAndParametersSize, payloadLength));
					columnsAndParametersSize += payloadLength;
				}
				if (!SupportsDeprecateEof)
				{
					payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					EofPayload.Create(payload.Span);
				}
			}

			ColumnDefinitionPayload[]? columns = null;
			if (response.ColumnCount > 0)
			{
				columns = new ColumnDefinitionPayload[response.ColumnCount];
				for (var i = 0; i < response.ColumnCount; i++)
				{
					payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					var payloadLength = payload.Span.Length;
					Utility.Resize(ref columnsAndParameters, columnsAndParametersSize + payloadLength);
					payload.Span.CopyTo(columnsAndParameters.AsSpan(columnsAndParametersSize));
					ColumnDefinitionPayload.Initialize(ref columns[i], new(columnsAndParameters, columnsAndParametersSize, payloadLength));
					columnsAndParametersSize += payloadLength;
				}
				if (!SupportsDeprecateEof)
				{
					payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					EofPayload.Create(payload.Span);
				}
			}

			preparedStatements.Add(new(response.StatementId, statement, columns, parameters));
		}

		m_preparedStatements ??= [];
		m_preparedStatements.Add(commandText, new(preparedStatements, parsedStatements));
	}

	public PreparedStatements? TryGetPreparedStatement(string commandText) =>
		m_preparedStatements is not null && m_preparedStatements.TryGetValue(commandText, out var statement) ? statement : null;

	public void StartQuerying(ICancellableCommand command)
	{
		lock (m_lock)
		{
			if (m_state is State.Querying or State.CancelingQuery)
			{
				CannotExecuteNewCommandInState(m_logger, Id, m_state);
				throw new InvalidOperationException("This MySqlConnection is already in use. See https://mysqlconnector.net/conn-reuse");
			}

			VerifyState(State.Connected);
			m_state = State.Querying;

			command.CancelAttemptCount = 0;
			ActiveCommandId = command.CommandId;
		}
	}

	public void FinishQuerying()
	{
		EnteringFinishQuerying(m_logger, Id, m_state);
		var clearConnection = false;
		lock (m_lock)
		{
			// entering this method, we expect to be in Querying, CancelingQuery, or Failed state
			if (m_state == State.CancelingQuery)
			{
				m_state = State.ClearingPendingCancellation;
				clearConnection = true;
			}
			else
			{
				if (m_state is State.Querying)
					m_state = State.Connected;
				else
					VerifyState(State.Failed);
				ActiveCommandId = 0;
			}
		}

		if (clearConnection)
		{
			// KILL QUERY will kill a subsequent query if the command it was intended to cancel has already completed.
			// In order to handle this case, we issue a dummy query that will consume the pending cancellation.
			// See https://bugs.mysql.com/bug.php?id=45679
			Log.SendingSleepToClearPendingCancellation(m_logger, Id);
			var payload = SupportsQueryAttributes ? s_sleepWithAttributesPayload : s_sleepNoAttributesPayload;
#pragma warning disable CA2012 // Safe because method completes synchronously
			SendAsync(payload, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
			payload = ReceiveReplyAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
			OkPayload.Verify(payload.Span, this);

			lock (m_lock)
			{
				VerifyState(State.ClearingPendingCancellation);
				m_state = State.Connected;
				ActiveCommandId = 0;
			}
		}
	}

	public void SetTimeout(int timeoutMilliseconds) => m_payloadHandler!.ByteHandler.RemainingTimeout = timeoutMilliseconds;

	public Activity? StartActivity(string name, string? tagName1 = null, object? tagValue1 = null)
	{
		var activity = ActivitySourceHelper.StartActivity(name, m_activityTags);
		if (activity is { IsAllDataRequested: true })
		{
			if (DatabaseOverride is not null)
				activity.SetTag(ActivitySourceHelper.DatabaseNameTagName, DatabaseOverride);
			if (tagName1 is not null)
				activity.SetTag(tagName1, tagValue1);
		}
		return activity;
	}

	public async Task DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (m_payloadHandler is not null)
		{
			// attempt to gracefully close the connection, ignoring any errors (it may have been closed already by the server, etc.)
			State state;
			lock (m_lock)
			{
				if (m_state is State.Connected or State.Failed)
					m_state = State.Closing;
				state = m_state;
			}

			if (state == State.Closing)
			{
				try
				{
					Log.SendingQuitCommand(m_logger, Id);
					m_payloadHandler.StartNewConversation();
					await m_payloadHandler.WritePayloadAsync(QuitPayload.Instance.Memory, ioBehavior).ConfigureAwait(false);
				}
				catch (IOException)
				{
				}
				catch (NotSupportedException)
				{
				}
				catch (ObjectDisposedException)
				{
				}
				catch (SocketException)
				{
				}
			}
		}

		ClearPreparedStatements();

		ShutdownSocket();
		lock (m_lock)
			m_state = State.Closed;
	}

	private async Task<string?> ConnectAsync(ConnectionSettings cs, MySqlConnection connection, long startingTimestamp, ILoadBalancer? loadBalancer, Activity? activity, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		try
		{
			lock (m_lock)
			{
				VerifyState(State.Created);
				m_state = State.Connecting;
			}

			// set activity tags
			{
				var connectionString = cs.ConnectionStringBuilder.GetConnectionString(cs.ConnectionStringBuilder.PersistSecurityInfo);
				m_activityTags.Add(ActivitySourceHelper.DatabaseSystemTagName, ActivitySourceHelper.DatabaseSystemValue);
				m_activityTags.Add(ActivitySourceHelper.DatabaseConnectionStringTagName, connectionString);
				m_activityTags.Add(ActivitySourceHelper.DatabaseUserTagName, cs.UserID);
				if (cs.Database.Length != 0)
					m_activityTags.Add(ActivitySourceHelper.DatabaseNameTagName, cs.Database);
				if (activity is { IsAllDataRequested: true })
				{
					activity.SetTag(ActivitySourceHelper.DatabaseSystemTagName, ActivitySourceHelper.DatabaseSystemValue)
						.SetTag(ActivitySourceHelper.DatabaseConnectionStringTagName, connectionString)
						.SetTag(ActivitySourceHelper.DatabaseUserTagName, cs.UserID);
					if (cs.Database.Length != 0)
						activity.SetTag(ActivitySourceHelper.DatabaseNameTagName, cs.Database);
				}
			}

			Conditions = MySqlConnectionOpenedConditions.New;
			var connected = cs.ConnectionProtocol switch
			{
				MySqlConnectionProtocol.Sockets => await OpenTcpSocketAsync(cs, loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer)), activity, ioBehavior, cancellationToken).ConfigureAwait(false),
				MySqlConnectionProtocol.UnixSocket => await OpenUnixSocketAsync(cs, activity, ioBehavior, cancellationToken).ConfigureAwait(false),
				MySqlConnectionProtocol.NamedPipe => await OpenNamedPipeAsync(cs, startingTimestamp, activity, ioBehavior, cancellationToken).ConfigureAwait(false),
				_ => false,
			};
			if (!connected)
			{
				lock (m_lock)
					m_state = State.Failed;
				Log.ConnectingFailed(m_logger, Id);
				throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Unable to connect to any of the specified MySQL hosts.");
			}

			var byteHandler = m_socket is null ? new StreamByteHandler(m_stream!) : (IByteHandler) new SocketByteHandler(m_socket);
			if (cs.ConnectionTimeout != 0)
				byteHandler.RemainingTimeout = Math.Max(1, cs.ConnectionTimeoutMilliseconds - Utility.GetElapsedMilliseconds(startingTimestamp));
			m_payloadHandler = new StandardPayloadHandler(byteHandler);

			var payload = await ReceiveAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			var initialHandshake = InitialHandshakePayload.Create(payload.Span);

			// if PluginAuth is supported, then use the specified auth plugin; else, fall back to protocol capabilities to determine the auth type to use
			var currentAuthenticationMethod = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.PluginAuth) != 0 ? initialHandshake.AuthPluginName! :
				(initialHandshake.ProtocolCapabilities & ProtocolCapabilities.SecureConnection) == 0 ? "mysql_old_password" :
				"mysql_native_password";
			Log.ServerSentAuthPluginName(m_logger, Id, currentAuthenticationMethod);
			if (currentAuthenticationMethod is not "mysql_native_password" and not "sha256_password" and not "caching_sha2_password")
			{
				Log.UnsupportedAuthenticationMethod(m_logger, Id, currentAuthenticationMethod);
				throw new NotSupportedException($"Authentication method '{initialHandshake.AuthPluginName}' is not supported.");
			}

			ServerVersion = new(initialHandshake.ServerVersion);
			ConnectionId = initialHandshake.ConnectionId;
			AuthPluginData = initialHandshake.AuthPluginData;
			m_compressionMethod = !cs.UseCompression ? CompressionMethod.None :
				((initialHandshake.ProtocolCapabilities & ProtocolCapabilities.ZstandardCompressionAlgorithm) != 0 && connection.ZstandardPlugin is not null) ? CompressionMethod.Zstandard :
				((initialHandshake.ProtocolCapabilities & ProtocolCapabilities.Compress) != 0) ? CompressionMethod.Zlib : CompressionMethod.None;
			CancellationTimeout = cs.CancellationTimeout;
			UserID = cs.UserID;

			// set activity tags
			{
				var connectionId = ConnectionId.ToString(CultureInfo.InvariantCulture);
				m_activityTags[ActivitySourceHelper.DatabaseConnectionIdTagName] = connectionId;
				if (activity is { IsAllDataRequested: true })
					activity.SetTag(ActivitySourceHelper.DatabaseConnectionIdTagName, connectionId);
			}

			m_supportsConnectionAttributes = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.ConnectionAttributes) != 0;
			SupportsDeprecateEof = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.DeprecateEof) != 0;
			SupportsCachedPreparedMetadata = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.MariaDbCacheMetadata) != 0;
			SupportsQueryAttributes = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.QueryAttributes) != 0;
			SupportsSessionTrack = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.SessionTrack) != 0;
			var serverSupportsSsl = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.Ssl) != 0;
			m_characterSet = ServerVersion.Version >= ServerVersions.SupportsUtf8Mb4 ? CharacterSet.Utf8Mb4GeneralCaseInsensitive : CharacterSet.Utf8Mb3GeneralCaseInsensitive;
			m_setNamesPayload = ServerVersion.Version >= ServerVersions.SupportsUtf8Mb4 ?
				(SupportsQueryAttributes ? s_setNamesUtf8mb4WithAttributesPayload : s_setNamesUtf8mb4NoAttributesPayload) :
				(SupportsQueryAttributes ? s_setNamesUtf8WithAttributesPayload : s_setNamesUtf8NoAttributesPayload);

			// disable pipelining for RDS MySQL 5.7 (assuming Aurora); otherwise take it from the connection string or default to true
			if (!cs.Pipelining.HasValue && ServerVersion.Version.Major == 5 && ServerVersion.Version.Minor == 7 && HostName.EndsWith(".rds.amazonaws.com", StringComparison.OrdinalIgnoreCase))
			{
				Log.AutoDetectedAurora57(m_logger, Id, HostName);
				m_supportsPipelining = false;
			}
			else
			{
				// pipelining is not currently compatible with compression
				m_supportsPipelining = m_compressionMethod == CompressionMethod.None && cs.Pipelining is not false;

				// for pipelining, concatenate reset connection and SET NAMES query into one buffer
				if (m_supportsPipelining)
				{
					m_pipelinedResetConnectionBytes = new byte[m_setNamesPayload.Span.Length + 9];

					// first packet: reset connection
					m_pipelinedResetConnectionBytes[0] = 1;
					m_pipelinedResetConnectionBytes[4] = (byte) CommandKind.ResetConnection;

					// second packet: SET NAMES query
					m_pipelinedResetConnectionBytes[5] = (byte) m_setNamesPayload.Span.Length;
					m_setNamesPayload.Span.CopyTo(m_pipelinedResetConnectionBytes.AsSpan()[9..]);
				}
			}

			Log.SessionMadeConnection(m_logger, Id, ServerVersion.OriginalString, ConnectionId, m_compressionMethod != CompressionMethod.None, m_supportsConnectionAttributes, SupportsDeprecateEof, SupportsCachedPreparedMetadata, serverSupportsSsl, SupportsSessionTrack, m_supportsPipelining, SupportsQueryAttributes);

			if (cs.SslMode != MySqlSslMode.None && (cs.SslMode != MySqlSslMode.Preferred || serverSupportsSsl))
			{
				if (!serverSupportsSsl)
				{
					Log.ServerDoesNotSupportSsl(m_logger, Id);
					throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Server does not support SSL");
				}

				await InitSslAsync(initialHandshake.ProtocolCapabilities, cs, connection, cs.TlsVersions, ioBehavior, cancellationToken).ConfigureAwait(false);
			}

			if (m_supportsConnectionAttributes && cs.ConnectionAttributes is null)
				cs.ConnectionAttributes = CreateConnectionAttributes(cs.ApplicationName);

			var password = GetPassword(cs, connection);

			// send a caching_sha2_password response if the server advertised support in the initial handshake
			var useCachingSha2 = initialHandshake.AuthPluginName == "caching_sha2_password";
			byte[] authenticationResponse;
			if (useCachingSha2)
				authenticationResponse = AuthenticationUtility.CreateScrambleResponse(Utility.TrimZeroByte(initialHandshake.AuthPluginData.AsSpan()), password);
			else
				AuthenticationUtility.CreateResponseAndPasswordHash(password, initialHandshake.AuthPluginData, out authenticationResponse, out m_passwordHash);

			using (var handshakeResponsePayload = HandshakeResponse41Payload.Create(initialHandshake, cs, authenticationResponse, m_compressionMethod, connection.ZstandardPlugin?.CompressionLevel, m_characterSet, m_supportsConnectionAttributes ? cs.ConnectionAttributes : null))
				await SendReplyAsync(handshakeResponsePayload, ioBehavior, cancellationToken).ConfigureAwait(false);
			payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			// if server doesn't support the authentication fast path, it will send a new challenge
			while (payload.HeaderByte == AuthenticationMethodSwitchRequestPayload.Signature)
			{
				payload = await SwitchAuthenticationAsync(cs, password, payload, ioBehavior, cancellationToken).ConfigureAwait(false);
			}

			// check if caching_sha2_password response was sent
			if (useCachingSha2 && payload.HeaderByte == CachingSha2ServerResponsePayload.Signature)
			{
				// process a successful response, or fall back to sending the password if the server doesn't have it
				var cachingSha2ServerResponsePayload = CachingSha2ServerResponsePayload.Create(payload.Span);
				if (cachingSha2ServerResponsePayload.Succeeded)
				{
					payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				else if (!m_isSecureConnection && password.Length != 0)
				{
					var publicKey = await GetRsaPublicKeyAsync(currentAuthenticationMethod, cs, ioBehavior, cancellationToken).ConfigureAwait(false);
					payload = await SendEncryptedPasswordAsync(AuthPluginData, publicKey, password, ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					payload = await SendClearPasswordAsync(password, ioBehavior, cancellationToken).ConfigureAwait(false);
				}
			}

			var ok = OkPayload.Create(payload.Span, this);

			if (m_sslPolicyErrors != SslPolicyErrors.None)
			{
				// SSL would normally have thrown error, but this was suppressed in ValidateRemoteCertificate; now we need to verify the server certificate
				// pass only if :
				// * connection method is MitM-proof (e.g. unix socket)
				// * auth plugin is MitM-proof and check SHA2(user's hashed password, scramble, certificate fingerprint)
				// see https://mariadb.org/mission-impossible-zero-configuration-ssl/
				var ignoreCertificateError = false;

				if (cs.ConnectionProtocol == MySqlConnectionProtocol.UnixSocket)
				{
					Log.CertificateErrorUnixSocket(m_logger, Id, m_sslPolicyErrors);
					ignoreCertificateError = true;
				}
				else if (string.IsNullOrEmpty(password))
				{
					// there is no shared secret that can be used to validate the certificate
					Log.CertificateErrorNoPassword(m_logger, Id, m_sslPolicyErrors);
				}
				else if (ValidateFingerprint(ok.StatusInfo, initialHandshake.AuthPluginData.AsSpan(0, 20)))
				{
					Log.CertificateErrorValidThumbprint(m_logger, Id, m_sslPolicyErrors);
					ignoreCertificateError = true;
				}

				if (!ignoreCertificateError)
				{
					ShutdownSocket();
					HostName = "";
					lock (m_lock)
						m_state = State.Failed;

					// throw a MySqlException with an AuthenticationException InnerException to mimic what would have happened if ValidateRemoteCertificate returned false
					var innerException = new AuthenticationException($"The remote certificate was rejected due to the following error: {m_sslPolicyErrors}");
					Log.CouldNotInitializeTlsConnection(m_logger, innerException, Id);
					throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "SSL Authentication Error", innerException);
				}
			}

			var redirectionUrl = ok.RedirectionUrl;

			if (m_compressionMethod == CompressionMethod.Zlib)
				m_payloadHandler = new CompressedPayloadHandler(m_payloadHandler.ByteHandler);
			else if (m_compressionMethod == CompressionMethod.Zstandard)
				m_payloadHandler = connection.ZstandardPlugin!.CreatePayloadHandler(m_payloadHandler.ByteHandler);

			// send 'SET NAMES' to set the character set and collation unless the server reports that it's already using the desired character set (e.g., MariaDB >= 11.5)
			if (ok.NewCharacterSet != (ServerVersion.Version >= ServerVersions.SupportsUtf8Mb4 ? CharacterSet.Utf8Mb4Binary : CharacterSet.Utf8Mb3Binary))
			{
				// set 'collation_connection' to the server default
				await SendAsync(m_setNamesPayload, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				OkPayload.Verify(payload.Span, this);
			}

			if (ShouldGetRealServerDetails(cs))
			{
				await GetRealServerDetailsAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
			}
			else if (ok.NewConnectionId is int newConnectionId && newConnectionId != ConnectionId)
			{
				Log.ChangingConnectionId(m_logger, Id, ConnectionId, newConnectionId, ServerVersion.OriginalString, ServerVersion.OriginalString);
				ConnectionId = newConnectionId;
			}

			m_payloadHandler.ByteHandler.RemainingTimeout = Constants.InfiniteTimeout;
			return redirectionUrl;
		}
		catch (ArgumentException ex)
		{
			Log.CouldNotConnectToServer(m_logger, ex, Id);
			throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Couldn't connect to server", ex);
		}
		catch (IOException ex)
		{
			Log.CouldNotConnectToServer(m_logger, ex, Id);
			throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Couldn't connect to server", ex);
		}
	}

	/// <summary>
	/// Validate SSL validation hash (from OK packet).
	/// </summary>
	/// <param name="validationHash">The validation hash received from the server.</param>
	/// <param name="challenge">The auth plugin data from the initial handshake.</param>
	/// <returns><c>true</c> if the validation hash matches the locally-computed value; otherwise, <c>false</c>.</returns>
	private bool ValidateFingerprint(byte[]? validationHash, ReadOnlySpan<byte> challenge)
	{
		// expect 0x01 followed by 64 hex characters giving a SHA2 hash
		if (validationHash?.Length != 65 || validationHash[0] != 1)
			return false;

		// the authentication plugin must have provided a password hash (via IAuthenticationPlugin3) that we saved for future use
		if (m_passwordHash is null)
			return false;

		// hash password hash || scramble || certificate thumbprint
		Span<byte> hashBytes = stackalloc byte[32];
		Span<byte> combined = [.. m_passwordHash, .. challenge, .. m_remoteCertificateSha2Thumbprint!];
#if NET5_0_OR_GREATER
		SHA256.TryHashData(combined, hashBytes, out _);
#else
		using var sha256 = SHA256.Create();
		sha256.TryComputeHash(combined, hashBytes, out _);
#endif

		Span<byte> serverHash = combined[0..32];
		return TryConvertFromHexString(validationHash.AsSpan(1), serverHash) && serverHash.SequenceEqual(hashBytes);

		static bool TryConvertFromHexString(ReadOnlySpan<byte> hexChars, Span<byte> data)
		{
#if NET10_0_OR_GREATER
			return Convert.FromHexString(hexChars, data, out _, out _) == OperationStatus.Done;
#else
			ReadOnlySpan<byte> hexDigits = "0123456789ABCDEFabcdef"u8;
			for (var i = 0; i < hexChars.Length; i += 2)
			{
				var high = hexDigits.IndexOf(hexChars[i]);
				var low = hexDigits.IndexOf(hexChars[i + 1]);
				if (high == -1 || low == -1)
					return false;
				if (high > 15)
					high -= 6;
				if (low > 15)
					low -= 6;
				data[i / 2] = (byte) ((high << 4) | low);
			}
			return true;
#endif
		}
	}

	public static async ValueTask<ServerSession> ConnectAndRedirectAsync(ILogger connectionLogger, ILogger poolLogger, IConnectionPoolMetadata pool, ConnectionSettings cs, ILoadBalancer? loadBalancer, MySqlConnection connection, Action<ILogger, int, string, Exception?>? logMessage, long startingTimestamp, Activity? activity, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var session = new ServerSession(connectionLogger, pool);
		if (logMessage is not null && poolLogger.IsEnabled(LogLevel.Debug))
			logMessage(poolLogger, pool.Id, session.Id, null);

		string? redirectionUrl;
		try
		{
			redirectionUrl = await session.ConnectAsync(cs, connection, startingTimestamp, loadBalancer, activity, ioBehavior, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception)
		{
			await session.DisposeAsync(ioBehavior, default).ConfigureAwait(false);
			throw;
		}

		Exception? redirectionException = null;
		if (redirectionUrl is not null)
		{
			Log.HasServerRedirectionHeader(connectionLogger, session.Id, redirectionUrl);
			if (cs.ServerRedirectionMode == MySqlServerRedirectionMode.Disabled)
			{
				Log.ServerRedirectionIsDisabled(connectionLogger, session.Id);
				return session;
			}

			if (Utility.TryParseRedirectionHeader(redirectionUrl, cs.UserID, out var host, out var port, out var user))
			{
				if (host != cs.HostNames![0] || port != cs.Port || user != cs.UserID)
				{
					var redirectedSettings = cs.CloneWith(host, port, user);
					Log.OpeningNewConnection(connectionLogger, session.Id, host, port, user);
					var redirectedSession = new ServerSession(connectionLogger, pool);
					try
					{
						await redirectedSession.ConnectAsync(redirectedSettings, connection, startingTimestamp, loadBalancer, activity, ioBehavior, cancellationToken).ConfigureAwait(false);
						Log.ClosingSessionToUseRedirectedSession(connectionLogger, session.Id, redirectedSession.Id);
						await session.DisposeAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
						return redirectedSession;
					}
					catch (Exception ex)
					{
						redirectionException = ex;
						Log.FailedToConnectRedirectedSession(connectionLogger, ex, session.Id, redirectedSession.Id);
						try
						{
							await redirectedSession.DisposeAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
						}
						catch (Exception)
						{
						}
					}
				}
				else
				{
					Log.SessionAlreadyConnectedToServer(connectionLogger, session.Id);
				}
			}
		}

		if (cs.ServerRedirectionMode == MySqlServerRedirectionMode.Required)
		{
			Log.RequiresServerRedirection(connectionLogger, session.Id);
			throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Server does not support redirection", redirectionException);
		}
		return session;
	}

	public async Task<bool> TryResetConnectionAsync(ConnectionSettings cs, MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		VerifyState(State.Connected);
		Conditions |= MySqlConnectionOpenedConditions.Reset;

		try
		{
			// clear all prepared statements; resetting the connection will clear them on the server
			ClearPreparedStatements();

			PayloadData payload;
			if (DatabaseOverride is null &&
				((!ServerVersion.IsMariaDb && ServerVersion.Version.CompareTo(ServerVersions.SupportsResetConnection) >= 0) ||
				(ServerVersion.IsMariaDb && ServerVersion.Version.CompareTo(ServerVersions.MariaDbSupportsResetConnection) >= 0)))
			{
				if (m_supportsPipelining)
				{
					Log.SendingPipelinedResetConnectionRequest(m_logger, Id, ServerVersion.OriginalString);

					// send both packets at once
					await SendRawAsync(m_pipelinedResetConnectionBytes, ioBehavior, cancellationToken).ConfigureAwait(false);

					// read two OK replies
					payload = await ReceiveReplyAsync(1, ioBehavior, cancellationToken).ConfigureAwait(false);
					OkPayload.Verify(payload.Span, this);

					payload = await ReceiveReplyAsync(1, ioBehavior, cancellationToken).ConfigureAwait(false);
					OkPayload.Verify(payload.Span, this);

					return true;
				}

				Log.SendingResetConnectionRequest(m_logger, Id, ServerVersion.OriginalString);
				await SendAsync(ResetConnectionPayload.Instance, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				OkPayload.Verify(payload.Span, this);
			}
			else
			{
				// optimistically hash the password with the challenge from the initial handshake (supported by MariaDB; doesn't appear to be supported by MySQL)
				if (DatabaseOverride is null)
				{
					Log.SendingChangeUserRequest(m_logger, Id, ServerVersion.OriginalString);
				}
				else
				{
					Log.SendingChangeUserRequestDueToChangedDatabase(m_logger, Id, DatabaseOverride);
					DatabaseOverride = null;
				}
				var password = GetPassword(cs, connection);
				AuthenticationUtility.CreateResponseAndPasswordHash(password, AuthPluginData, out var nativeResponse, out m_passwordHash);
				using (var changeUserPayload = ChangeUserPayload.Create(cs.UserID, nativeResponse, cs.Database, m_characterSet, m_supportsConnectionAttributes ? cs.ConnectionAttributes : null))
					await SendAsync(changeUserPayload, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				if (payload.HeaderByte == AuthenticationMethodSwitchRequestPayload.Signature)
				{
					Log.OptimisticReauthenticationFailed(m_logger, Id);
					payload = await SwitchAuthenticationAsync(cs, password, payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				OkPayload.Verify(payload.Span, this);
			}

			// set 'collation_connection' to the server default
			await SendAsync(m_setNamesPayload, ioBehavior, cancellationToken).ConfigureAwait(false);
			payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			OkPayload.Verify(payload.Span, this);

			return true;
		}
		catch (IOException ex)
		{
			Log.IgnoringFailureInTryResetConnectionAsync(m_logger, ex, Id, "IOException");
		}
		catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.ClientInteractionTimeout)
		{
			Log.IgnoringFailureInTryResetConnectionAsync(m_logger, ex, Id, "ClientInteractionTimeout MySqlException");
		}
		catch (ObjectDisposedException ex)
		{
			Log.IgnoringFailureInTryResetConnectionAsync(m_logger, ex, Id, "ObjectDisposedException");
		}
		catch (SocketException ex)
		{
			Log.IgnoringFailureInTryResetConnectionAsync(m_logger, ex, Id, "SocketException");
		}

		Conditions &= ~MySqlConnectionOpenedConditions.Reset;
		return false;
	}

	private async Task<PayloadData> SwitchAuthenticationAsync(ConnectionSettings cs, string password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// if the server didn't support the hashed password; rehash with the new challenge
		var switchRequest = AuthenticationMethodSwitchRequestPayload.Create(payload.Span);
		Log.SwitchingToAuthenticationMethod(m_logger, Id, switchRequest.Name);
		switch (switchRequest.Name)
		{
			case "mysql_native_password":
				AuthPluginData = switchRequest.Data;
				AuthenticationUtility.CreateResponseAndPasswordHash(password, AuthPluginData, out var nativeResponse, out m_passwordHash);
				payload = new(nativeResponse);
				await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			case "mysql_clear_password":
				if (!m_isSecureConnection && !m_isLoopbackConnection)
				{
					Log.NeedsSecureConnection(m_logger, Id, switchRequest.Name);
					throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, $"Authentication method '{switchRequest.Name}' requires a secure connection.");
				}

				// send the password as a NULL-terminated UTF-8 string
				var passwordBytes = AuthenticationUtility.GetNullTerminatedPasswordBytes(password);
				payload = new(passwordBytes);
				await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			case "caching_sha2_password":
				// see https://dev.mysql.com/doc/dev/mysql-server/latest/page_caching_sha2_authentication_exchanges.html
				var scrambleBytes = AuthenticationUtility.CreateScrambleResponse(Utility.TrimZeroByte(switchRequest.Data.AsSpan()), password);
				payload = new(scrambleBytes);
				await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

				// OK payload can be sent immediately (e.g., if password is empty) bypassing even the fast authentication path
				if (OkPayload.IsOk(payload.Span, this))
					return payload;

				var cachingSha2ServerResponsePayload = CachingSha2ServerResponsePayload.Create(payload.Span);
				if (cachingSha2ServerResponsePayload.Succeeded)
					return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

				goto case "sha256_password";

			case "sha256_password":
				if (!m_isSecureConnection && password.Length != 0)
				{
					var publicKey = await GetRsaPublicKeyAsync(switchRequest.Name, cs, ioBehavior, cancellationToken).ConfigureAwait(false);
					return await SendEncryptedPasswordAsync(switchRequest.Data, publicKey, password, ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					return await SendClearPasswordAsync(password, ioBehavior, cancellationToken).ConfigureAwait(false);
				}

			case "auth_gssapi_client":
				return await AuthGSSAPI.AuthenticateAsync(cs, switchRequest.Data, this, ioBehavior, cancellationToken).ConfigureAwait(false);

			case "mysql_old_password":
				Log.AuthenticationMethodNotSupported(m_logger, Id, switchRequest.Name);
				throw new NotSupportedException("'MySQL Server is requesting the insecure pre-4.1 auth mechanism (mysql_old_password). The user password must be upgraded; see https://dev.mysql.com/doc/refman/5.7/en/account-upgrades.html.");

			case "client_ed25519":
				if (!AuthenticationPlugins.TryGetPlugin(switchRequest.Name, out var ed25519Plugin) || ed25519Plugin is not IAuthenticationPlugin3 ed25519Plugin3)
					throw new NotSupportedException("You must install the MySqlConnector.Authentication.Ed25519 package and call Ed25519AuthenticationPlugin.Install to use client_ed25519 authentication.");
				ed25519Plugin3.CreateResponseAndPasswordHash(password, switchRequest.Data, out var ed25519Response, out m_passwordHash);
				payload = new(ed25519Response);
				await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			case "parsec":
				if (!AuthenticationPlugins.TryGetPlugin(switchRequest.Name, out var parsecPlugin) || parsecPlugin is not IAuthenticationPlugin3 parsecPlugin3)
					throw new NotSupportedException("You must install the MySqlConnector.Authentication.Ed25519 package and call ParsecAuthenticationPlugin.Install to use parsec authentication.");
				payload = new([]);
				await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				var extendedSalt = payload.Span;

				// MariaDB 11.8.4 sends an extra 0x01 byte at the beginning of ext-salt: https://github.com/mysql-net/MySqlConnector/issues/1606
				if (extendedSalt.Length > 2 && extendedSalt[0] == 1 && extendedSalt[1] == 'P')
					extendedSalt = extendedSalt[1..];

				Span<byte> combinedData = stackalloc byte[switchRequest.Data.Length + extendedSalt.Length];
				switchRequest.Data.CopyTo(combinedData);
				extendedSalt.CopyTo(combinedData.Slice(switchRequest.Data.Length));

				parsecPlugin3.CreateResponseAndPasswordHash(password, combinedData, out var parsecResponse, out m_passwordHash);
				payload = new(parsecResponse);
				await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			default:
				Log.AuthenticationMethodNotSupported(m_logger, Id, switchRequest.Name);
				throw new NotSupportedException($"Authentication method '{switchRequest.Name}' is not supported.");
		}
	}

	private async Task<PayloadData> SendClearPasswordAsync(string password, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// add NUL terminator to password
		var passwordBytes = AuthenticationUtility.GetNullTerminatedPasswordBytes(password);

		// send plaintext password
		var payload = new PayloadData(passwordBytes);
		await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
		return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
	}

	private async Task<PayloadData> SendEncryptedPasswordAsync(
		byte[] switchRequestData,
		byte[] rsaPublicKey,
		string password,
		IOBehavior ioBehavior,
		CancellationToken cancellationToken)
	{
		using var rsa = RSA.Create();
#if NET5_0_OR_GREATER
		try
		{
			Utility.LoadRsaParameters(rsaPublicKey, rsa);
		}
		catch (Exception ex)
		{
			Log.CouldNotLoadServerRsaPublicKey(m_logger, ex, Id);
			throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Couldn't load server's RSA public key; try using a secure connection instead.", ex);
		}
#else
		// load the RSA public key
		RSAParameters rsaParameters;
		try
		{
			rsaParameters = Utility.GetRsaParameters(Encoding.ASCII.GetString(rsaPublicKey));
		}
		catch (Exception ex)
		{
			Log.CouldNotLoadServerRsaPublicKey(m_logger, ex, Id);
			throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Couldn't load server's RSA public key; try using a secure connection instead.", ex);
		}

		rsa.ImportParameters(rsaParameters);
#endif

		// add NUL terminator to password
		var passwordBytes = AuthenticationUtility.GetNullTerminatedPasswordBytes(password);

		// XOR the password bytes with the challenge
		AuthPluginData = Utility.TrimZeroByte(switchRequestData);
		for (var i = 0; i < passwordBytes.Length; i++)
			passwordBytes[i] ^= AuthPluginData[i % AuthPluginData.Length];

		// encrypt with RSA public key
		var padding = RSAEncryptionPadding.OaepSHA1;
		var encryptedPassword = rsa.Encrypt(passwordBytes, padding);
		var payload = new PayloadData(encryptedPassword);
		await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
		return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
	}

	private async Task<byte[]> GetRsaPublicKeyAsync(string switchRequestName, ConnectionSettings cs, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (cs.ServerRsaPublicKeyFile.Length != 0)
		{
			try
			{
				return File.ReadAllBytes(cs.ServerRsaPublicKeyFile);
			}
			catch (IOException ex)
			{
				Log.CouldNotLoadServerRsaPublicKeyFromFile(m_logger, ex, Id, cs.ServerRsaPublicKeyFile);
				throw new MySqlException($"Couldn't load server's RSA public key from '{cs.ServerRsaPublicKeyFile}'", ex);
			}
		}

		if (cs.AllowPublicKeyRetrieval || m_isLoopbackConnection)
		{
			// request the RSA public key
			var payloadContent = switchRequestName == "caching_sha2_password" ? (byte) 0x02 : (byte) 0x01;
			await SendReplyAsync(new PayloadData([payloadContent]), ioBehavior, cancellationToken).ConfigureAwait(false);
			var payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			var publicKeyPayload = AuthenticationMoreDataPayload.Create(payload.Span);
			return publicKeyPayload.Data;
		}

		Log.CouldNotUseAuthenticationMethodForRsa(m_logger, Id, switchRequestName);
		throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, $"Authentication method '{switchRequestName}' failed. Either use a secure connection, specify the server's RSA public key with ServerRSAPublicKeyFile, or set AllowPublicKeyRetrieval=True.");
	}

	public async ValueTask<bool> TryPingAsync(bool logInfo, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		VerifyState(State.Connected);

		// send ping payload to verify client and server socket are still connected
		try
		{
			Log.PingingServer(m_logger, Id);
			await SendAsync(PingPayload.Instance, ioBehavior, cancellationToken).ConfigureAwait(false);
			var payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			OkPayload.Verify(payload.Span, this);
			Log.SuccessfullyPingedServer(m_logger, logInfo ? LogLevel.Information : LogLevel.Trace, Id);
			return true;
		}
		catch (IOException ex)
		{
			Log.PingFailed(m_logger, ex, Id, "IOException");
		}
		catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.ClientInteractionTimeout)
		{
			Log.PingFailed(m_logger, ex, Id, "ClientInteractionTimeout MySqlException");
		}
		catch (SocketException ex)
		{
			Log.PingFailed(m_logger, ex, Id, "SocketException");
		}

		VerifyState(State.Failed);
		return false;
	}

	// Starts a new conversation with the server by sending the first packet.
	public ValueTask SendAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		m_payloadHandler!.StartNewConversation();
		return SendReplyAsync(payload, ioBehavior, cancellationToken);
	}

	// Starts a new conversation with the server by receiving the first packet.
	public ValueTask<PayloadData> ReceiveAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		m_payloadHandler!.StartNewConversation();
		return ReceiveReplyAsync(ioBehavior, cancellationToken);
	}

	// Continues a conversation with the server by receiving a response to a packet sent with 'Send' or 'SendReply'.
	public async ValueTask<PayloadData> ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (CreateExceptionForInvalidState() is { } exception)
		{
			Log.FailedInReceiveReplyAsync(m_logger, exception, Id);
			throw exception;
		}

		ArraySegment<byte> bytes;
		try
		{
			bytes = await m_payloadHandler!.ReadPayloadAsync(m_payloadCache, ProtocolErrorBehavior.Throw, ioBehavior).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			if (ex is MySqlEndOfStreamException endOfStream)
				Log.ExpectedToReadMoreBytes(m_logger, Id, endOfStream.ExpectedByteCount, endOfStream.ReadByteCount);
			SetFailed(ex);
			throw;
		}

		var payload = new PayloadData(bytes);
		return payload.HeaderByte == ErrorPayload.Signature ? throw CreateExceptionForErrorPayload(payload.Span) : payload;
	}

	public ValueTask<PayloadData> ReceiveReplyAsync(int expectedSequenceNumber, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		m_payloadHandler!.SetNextSequenceNumber(expectedSequenceNumber);
		return ReceiveReplyAsync(ioBehavior, cancellationToken);
	}

	// Continues a conversation with the server by sending a reply to a packet received with 'Receive' or 'ReceiveReply'.
	public async ValueTask SendReplyAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (CreateExceptionForInvalidState() is { } exception)
		{
			Log.FailedInSendReplyAsync(m_logger, exception, Id);
			throw exception;
		}

		try
		{
			await m_payloadHandler!.WritePayloadAsync(payload.Memory, ioBehavior).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			SetFailed(ex);
			throw;
		}
	}

	// Sends raw bytes over the wire without formatting them into a payload. The caller is expected to set the packet header(s) correctly.
	public async ValueTask SendRawAsync(ReadOnlyMemory<byte> data, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (CreateExceptionForInvalidState() is { } exception)
		{
			Log.FailedInSendReplyAsync(m_logger, exception, Id);
			throw exception;
		}

		try
		{
			// send both packets at once
			await m_payloadHandler!.ByteHandler.WriteBytesAsync(data, ioBehavior).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			SetFailed(ex);
			throw;
		}
	}

	public static void ThrowIfStatementContainsDelimiter(MySqlException exception, IMySqlCommand command)
	{
		// check if the command used "DELIMITER"
		if (exception.ErrorCode == MySqlErrorCode.ParseError && command.CommandText?.IndexOf("delimiter", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			var parser = new DelimiterSqlParser(command);
			parser.Parse(command.CommandText);
			if (parser.HasDelimiter)
				throw new MySqlException(MySqlErrorCode.DelimiterNotSupported, "'DELIMITER' should not be used with MySqlConnector. See https://mysqlconnector.net/delimiter", exception);
		}
	}

	private InvalidOperationException? CreateExceptionForInvalidState()
	{
		lock (m_lock)
		{
			return m_state switch
			{
				State.Closed => new ObjectDisposedException(nameof(ServerSession)),
				State.Connected or State.Querying or State.CancelingQuery or State.ClearingPendingCancellation or State.Closing => null,
				_ => new InvalidOperationException("ServerSession is not connected."),
			};
		}
	}

	private async Task<bool> OpenTcpSocketAsync(ConnectionSettings cs, ILoadBalancer loadBalancer, Activity? activity, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// set activity tags for TCP/IP
		{
			m_activityTags.Add(ActivitySourceHelper.NetTransportTagName, ActivitySourceHelper.NetTransportTcpIpValue);
			string? port = cs.Port == 3306 ? default : cs.Port.ToString(CultureInfo.InvariantCulture);
			if (port is not null)
				m_activityTags.Add(ActivitySourceHelper.NetPeerPortTagName, port);
			if (activity is { IsAllDataRequested: true })
			{
				activity.SetTag(ActivitySourceHelper.NetTransportTagName, ActivitySourceHelper.NetTransportTcpIpValue);
				if (port is not null)
					activity.SetTag(ActivitySourceHelper.NetPeerPortTagName, port);
			}
		}

		var hostNames = loadBalancer.LoadBalance(cs.HostNames!);
		for (var hostNameIndex = 0; hostNameIndex < hostNames.Count; hostNameIndex++)
		{
			var hostName = hostNames[hostNameIndex];
			IPAddress[] ipAddresses;
			try
			{
				if (cs.IPAddress is { } ipAddress)
				{
					ipAddresses = [ipAddress];
				}
				else
				{
					ipAddresses = ioBehavior == IOBehavior.Asynchronous
#if NET6_0_OR_GREATER
						? await Dns.GetHostAddressesAsync(hostName, cancellationToken).ConfigureAwait(false)
#else
						? await Dns.GetHostAddressesAsync(hostName).ConfigureAwait(false)
#endif
						: Dns.GetHostAddresses(hostName);
				}
			}
			catch (SocketException ex)
			{
				// name couldn't be resolved
				Log.FailedToResolveHostName(m_logger, ex, Id, hostName, hostNameIndex + 1, hostNames.Count, ex.Message);
				continue;
			}

			// need to try IP Addresses one at a time: https://github.com/dotnet/corefx/issues/5829
			for (var ipAddressIndex = 0; ipAddressIndex < ipAddresses.Length; ipAddressIndex++)
			{
				var ipAddress = ipAddresses[ipAddressIndex];
				var ipAddressString = ipAddress.ToString();
				Log.ConnectingToIpAddress(m_logger, Id, ipAddressString, ipAddressIndex + 1, ipAddresses.Length, hostName, hostNameIndex + 1, hostNames.Count);

				// set activity tags for the current IP address/hostname
				{
					m_activityTags[ActivitySourceHelper.NetPeerIpTagName] = ipAddressString;
					if (ipAddressString != hostName)
						m_activityTags[ActivitySourceHelper.NetPeerNameTagName] = hostName;
					else
						m_activityTags.Remove(ActivitySourceHelper.NetPeerNameTagName);

					if (activity is { IsAllDataRequested: true })
					{
						activity.SetTag(ActivitySourceHelper.NetPeerIpTagName, ipAddressString);
						if (ipAddressString != hostName)
							activity.SetTag(ActivitySourceHelper.NetPeerNameTagName, hostName);
						else
							activity.SetTag(ActivitySourceHelper.NetPeerNameTagName, null);
					}
				}

				TcpClient? tcpClient = null;
				try
				{
					tcpClient = new(ipAddress.AddressFamily);

					using (cancellationToken.Register(() => tcpClient?.Client?.Dispose()))
					{
						try
						{
							if (ioBehavior == IOBehavior.Asynchronous)
							{
#if NET5_0_OR_GREATER
								await tcpClient.ConnectAsync(ipAddress, cs.Port, cancellationToken).ConfigureAwait(false);
#else
								await tcpClient.ConnectAsync(ipAddress, cs.Port).ConfigureAwait(false);
#endif
							}
							else
							{
								if (Utility.IsWindows())
								{
									tcpClient.Connect(ipAddress, cs.Port);
								}
								else
								{
									// non-windows platforms block on synchronous connect, use send/receive timeouts: https://github.com/dotnet/corefx/issues/20954
									var originalSendTimeout = tcpClient.Client.SendTimeout;
									var originalReceiveTimeout = tcpClient.Client.ReceiveTimeout;
									tcpClient.Client.SendTimeout = cs.ConnectionTimeoutMilliseconds;
									tcpClient.Client.ReceiveTimeout = cs.ConnectionTimeoutMilliseconds;
									tcpClient.Connect(ipAddress, cs.Port);
									tcpClient.Client.SendTimeout = originalSendTimeout;
									tcpClient.Client.ReceiveTimeout = originalReceiveTimeout;
								}
							}
						}
						catch (Exception ex) when (cancellationToken.IsCancellationRequested && ex is ObjectDisposedException or SocketException)
						{
							SafeDispose(ref tcpClient);
							Log.ConnectTimeoutExpired(m_logger, ex, Id, ipAddressString, hostName);
							throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Connect Timeout expired.");
						}
					}
				}
				catch (SocketException ex)
				{
					SafeDispose(ref tcpClient);

					// if this is the final IP address in the list, throw a fatal exception; otherwise try the next IP address
					if (hostNameIndex == hostNames.Count - 1 && ipAddressIndex == ipAddresses.Length - 1)
					{
						lock (m_lock)
							m_state = State.Failed;
						if (hostNames.Count == 1 && ipAddresses.Length == 1)
							Log.FailedToConnectToSingleIpAddress(m_logger, ex, Id, ipAddressString, hostName, ex.Message);
						else
							Log.FailedToConnectToIpAddress(m_logger, ex, LogLevel.Information, Id, ipAddressString, ipAddressIndex + 1, ipAddresses.Length, hostName, hostNameIndex + 1, hostNames.Count, ex.Message);
						throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Unable to connect to any of the specified MySQL hosts.");
					}

					Log.FailedToConnectToIpAddress(m_logger, ex, LogLevel.Trace, Id, ipAddressString, ipAddressIndex + 1, ipAddresses.Length, hostName, hostNameIndex + 1, hostNames.Count, ex.Message);
					continue;
				}

				if (!tcpClient.Connected && cancellationToken.IsCancellationRequested)
				{
					SafeDispose(ref tcpClient);
					Log.ConnectTimeoutExpired(m_logger, null, Id, ipAddressString, hostName);
					throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Connect Timeout expired.");
				}

				try
				{
					HostName = hostName;
					m_tcpClient = tcpClient;
					m_socket = m_tcpClient.Client;
					m_socket.NoDelay = true;
					m_stream = m_tcpClient.GetStream();
					m_socket.SetKeepAlive(cs.Keepalive);
					m_isLoopbackConnection = IPAddress.IsLoopback(ipAddress);
				}
				catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
				{
					Utility.Dispose(ref m_stream);
					SafeDispose(ref m_tcpClient);
					SafeDispose(ref m_socket);
					Log.ConnectTimeoutExpired(m_logger, null, Id, ipAddressString, hostName);
					throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Connect Timeout expired.");
				}

				lock (m_lock)
					m_state = State.Connected;
				Log.ConnectedToIpAddress(m_logger, Id, ipAddressString, hostName, (m_socket.LocalEndPoint as IPEndPoint)?.Port);
				return true;
			}
		}
		return false;
	}

	private async Task<bool> OpenUnixSocketAsync(ConnectionSettings cs, Activity? activity, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Log.ConnectingToUnixSocket(m_logger, Id, cs.UnixSocket!);

		// set activity tags
		{
			m_activityTags.Add(ActivitySourceHelper.NetTransportTagName, ActivitySourceHelper.NetTransportUnixValue);
			m_activityTags.Add(ActivitySourceHelper.NetPeerNameTagName, cs.UnixSocket);
			if (activity is { IsAllDataRequested: true })
			{
				activity.SetTag(ActivitySourceHelper.NetTransportTagName, ActivitySourceHelper.NetTransportUnixValue)
					.SetTag(ActivitySourceHelper.NetPeerNameTagName, cs.UnixSocket);
			}
		}

		var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
		var unixEp = new UnixDomainSocketEndPoint(cs.UnixSocket!);
		try
		{
			using (cancellationToken.Register(socket.Dispose))
			{
				try
				{
					if (ioBehavior == IOBehavior.Asynchronous)
					{
						await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, unixEp, null).ConfigureAwait(false);
					}
					else
					{
						socket.Connect(unixEp);
					}
				}
				catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
				{
					Log.ConnectTimeoutExpiredForUnixSocket(m_logger, Id, cs.UnixSocket!);
					throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Connect Timeout expired.");
				}
			}
		}
		catch (SocketException)
		{
			socket.Dispose();
		}

		if (socket.Connected)
		{
			m_socket = socket;
			m_stream = new NetworkStream(socket);

			lock (m_lock)
				m_state = State.Connected;
			return true;
		}

		return false;
	}

	private async Task<bool> OpenNamedPipeAsync(ConnectionSettings cs, long startingTimestamp, Activity? activity, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Log.ConnectingToNamedPipe(m_logger, Id, cs.PipeName, cs.HostNames![0]);

		// set activity tags
		{
			// see https://docs.microsoft.com/en-us/windows/win32/ipc/pipe-names for pipe name format
			var pipeName = $@"\\{cs.HostNames![0]}\pipe\{cs.PipeName}";
			m_activityTags.Add(ActivitySourceHelper.NetTransportTagName, ActivitySourceHelper.NetTransportNamedPipeValue);
			m_activityTags.Add(ActivitySourceHelper.NetPeerNameTagName, pipeName);
			if (activity is { IsAllDataRequested: true })
			{
				activity.SetTag(ActivitySourceHelper.NetTransportTagName, ActivitySourceHelper.NetTransportNamedPipeValue);
				activity.SetTag(ActivitySourceHelper.NetPeerNameTagName, pipeName);
			}
		}

		var namedPipeStream = new NamedPipeClientStream(cs.HostNames![0], cs.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
		var timeout = Math.Max(1, cs.ConnectionTimeoutMilliseconds - Utility.GetElapsedMilliseconds(startingTimestamp));
		try
		{
			using (cancellationToken.Register(namedPipeStream.Dispose))
			{
				try
				{
					if (ioBehavior == IOBehavior.Asynchronous)
						await namedPipeStream.ConnectAsync(timeout, cancellationToken).ConfigureAwait(false);
					else
						namedPipeStream.Connect(timeout);
				}
				catch (Exception ex) when ((ex is ObjectDisposedException && cancellationToken.IsCancellationRequested) || ex is TimeoutException)
				{
					Log.ConnectTimeoutExpiredForNamedPipe(m_logger, ex, Id, cs.PipeName, cs.HostNames![0]);
					throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Connect Timeout expired.");
				}
			}
		}
		catch (IOException)
		{
			namedPipeStream.Dispose();
		}

		if (namedPipeStream.IsConnected)
		{
			m_stream = namedPipeStream;

			lock (m_lock)
				m_state = State.Connected;
			return true;
		}

		return false;
	}

	private async Task InitSslAsync(ProtocolCapabilities serverCapabilities, ConnectionSettings cs, MySqlConnection connection, SslProtocols sslProtocols, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Log.InitializingTlsConnection(m_logger, Id);
		X509CertificateCollection? clientCertificates = null;

		if (cs.CertificateStoreLocation != MySqlCertificateStoreLocation.None)
		{
			try
			{
				var storeLocation = (cs.CertificateStoreLocation == MySqlCertificateStoreLocation.CurrentUser) ? StoreLocation.CurrentUser : StoreLocation.LocalMachine;
				using var store = new X509Store(StoreName.My, storeLocation);
				store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

				if (cs.CertificateThumbprint.Length == 0)
				{
					if (store.Certificates.Count == 0)
					{
						Log.NoCertificatesFound(m_logger, Id);
						throw new MySqlException("No certificates were found in the certificate store");
					}

					clientCertificates = new(store.Certificates);
				}
				else
				{
					var requireValid = cs.SslMode is MySqlSslMode.VerifyCA or MySqlSslMode.VerifyFull;
					var foundCertificates = store.Certificates.Find(X509FindType.FindByThumbprint, cs.CertificateThumbprint, requireValid);
					if (foundCertificates.Count == 0)
					{
						Log.CertificateNotFoundInStore(m_logger, Id, cs.CertificateThumbprint);
						throw new MySqlException($"Certificate with Thumbprint {cs.CertificateThumbprint} not found");
					}

					clientCertificates = new(foundCertificates);
				}
			}
			catch (CryptographicException ex)
			{
				Log.CouldNotLoadCertificate(m_logger, ex, Id, cs.CertificateStoreLocation);
				throw new MySqlException("Certificate couldn't be loaded from the CertificateStoreLocation", ex);
			}
		}

		if (cs.SslKeyFile.Length != 0 && cs.SslCertificateFile.Length != 0)
		{
			clientCertificates = LoadCertificate(cs.SslKeyFile, cs.SslCertificateFile);
		}
		else if (cs.CertificateFile.Length != 0)
		{
			try
			{
#if NET9_0_OR_GREATER
				var certificate = X509CertificateLoader.LoadPkcs12FromFile(cs.CertificateFile, cs.CertificatePassword, X509KeyStorageFlags.MachineKeySet);
#else
				var certificate = new X509Certificate2(cs.CertificateFile, cs.CertificatePassword, X509KeyStorageFlags.MachineKeySet);
#endif
				if (!certificate.HasPrivateKey)
				{
					certificate.Dispose();

					Log.NoPrivateKeyIncludedWithCertificateFile(m_logger, Id, cs.CertificateFile);
					throw new MySqlException("CertificateFile does not contain a private key. " +
						"CertificateFile should be in PKCS #12 (.pfx) format and contain both a Certificate and Private Key");
				}
				m_clientCertificate = certificate;
				clientCertificates = [certificate];
			}
			catch (CryptographicException ex)
			{
				Log.CouldNotLoadCertificateFromFile(m_logger, ex, Id, cs.CertificateFile);
				if (!File.Exists(cs.CertificateFile))
					throw new MySqlException("Cannot find Certificate File", ex);
				throw new MySqlException("Either the Certificate Password is incorrect or the Certificate File is invalid", ex);
			}
		}

		if (clientCertificates is null && connection.ProvideClientCertificatesCallback is { } clientCertificatesProvider)
		{
			clientCertificates = [];
			try
			{
				await clientCertificatesProvider(clientCertificates).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.FailedToObtainClientCertificates(m_logger, ex, Id, ex.Message);
				throw new MySqlException("Failed to obtain client certificates via ProvideClientCertificatesCallback", ex);
			}
		}

		X509Chain? caCertificateChain = null;
		if (cs.CACertificateFile.Length != 0)
		{
			X509Chain? certificateChain = new()
			{
				ChainPolicy =
				{
					RevocationMode = X509RevocationMode.NoCheck,
					VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority,
				},
			};

			try
			{
				// read the CA Certificate File
				Log.LoadingCaCertificatesFromFile(m_logger, Id, cs.CACertificateFile);
				byte[] certificateBytes;
				try
				{
					certificateBytes = File.ReadAllBytes(cs.CACertificateFile);
				}
				catch (Exception ex)
				{
					Log.CouldNotLoadCaCertificateFromFile(m_logger, ex, LogLevel.Error, Id, cs.CACertificateFile);
					if (!File.Exists(cs.CACertificateFile))
						throw new MySqlException("Cannot find CA Certificate File: " + cs.CACertificateFile, ex);
					throw new MySqlException("Could not load CA Certificate File: " + cs.CACertificateFile, ex);
				}

				// find the index of each individual certificate in the file (assuming there may be multiple certificates concatenated together)
				for (var index = 0; index != -1;)
				{
					var nextIndex = Utility.FindNextIndex(certificateBytes, index + 1, "-----BEGIN CERTIFICATE-----"u8);
					try
					{
						// load the certificate at this index; note that 'new X509Certificate' stops at the end of the first certificate it loads
						Log.LoadingCaCertificate(m_logger, Id, index);
#if NET9_0_OR_GREATER
						var caCertificate = X509CertificateLoader.LoadCertificate(certificateBytes.AsSpan(index, (nextIndex == -1 ? certificateBytes.Length : nextIndex) - index));
#elif NET5_0_OR_GREATER
						var caCertificate = new X509Certificate2(certificateBytes.AsSpan(index, (nextIndex == -1 ? certificateBytes.Length : nextIndex) - index), default(ReadOnlySpan<char>), X509KeyStorageFlags.MachineKeySet);
#else
						var caCertificate = new X509Certificate2(Utility.ArraySlice(certificateBytes, index, (nextIndex == -1 ? certificateBytes.Length : nextIndex) - index), default(string), X509KeyStorageFlags.MachineKeySet);
#endif
						certificateChain.ChainPolicy.ExtraStore.Add(caCertificate);
					}
					catch (CryptographicException ex)
					{
						Log.CouldNotLoadCaCertificateFromFile(m_logger, ex, LogLevel.Warning, Id, cs.CACertificateFile);
					}
					index = nextIndex;
				}

				// success
				Log.LoadedCaCertificatesFromFile(m_logger, Id, certificateChain.ChainPolicy.ExtraStore.Count, cs.CACertificateFile);
				caCertificateChain = certificateChain;
				certificateChain = null;
			}
			finally
			{
				certificateChain?.Dispose();
			}
		}

		X509Certificate ValidateLocalCertificate(object lcbSender, string lcbTargetHost, X509CertificateCollection lcbLocalCertificates, X509Certificate? lcbRemoteCertificate, string[] lcbAcceptableIssuers) => lcbLocalCertificates[0];

		bool ValidateRemoteCertificate(object rcbSender, X509Certificate? rcbCertificate, X509Chain? rcbChain, SslPolicyErrors rcbPolicyErrors)
		{
			// if no CA verification is required, then we trust any remote certificate
			if (cs.SslMode is MySqlSslMode.Preferred or MySqlSslMode.Required)
				return true;

			// if there are errors, then try to build a path to a root certificate from the certificates presented by the remote host
			if ((rcbPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 &&
				rcbChain is not null &&
				caCertificateChain is not null &&
				caCertificateChain.Build(rcbChain.ChainElements[^1].Certificate) &&
				caCertificateChain.ChainStatus.Length > 0)
			{
				// if the only error is an Untrusted Root Certificate, then check all provided SSL CA certificates to see if one is the root
				if (caCertificateChain.ChainStatus[0].Status == X509ChainStatusFlags.UntrustedRoot)
				{
					var rootCertificate = caCertificateChain.ChainElements[^1].Certificate;
					foreach (var sslCaCertificate in caCertificateChain.ChainPolicy.ExtraStore)
					{
#if NET7_0_OR_GREATER
						if (rootCertificate.RawDataMemory.Span.SequenceEqual(sslCaCertificate.RawDataMemory.Span))
#else
						if (rootCertificate.RawData.AsSpan().SequenceEqual(sslCaCertificate.RawData))
#endif
						{
							rcbPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
							break;
						}
					}
				}
			}

			if (cs.SslMode == MySqlSslMode.VerifyCA)
				rcbPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;

			if (rcbCertificate is X509Certificate2 cert2)
			{
				// saving sha256 thumbprint and SSL errors until thumbprint validation
#if NET7_0_OR_GREATER
				m_remoteCertificateSha2Thumbprint = SHA256.HashData(cert2.RawDataMemory.Span);
#elif NET5_0_OR_GREATER
				m_remoteCertificateSha2Thumbprint = SHA256.HashData(cert2.RawData);
#else
				using var sha256 = SHA256.Create();
				m_remoteCertificateSha2Thumbprint = sha256.ComputeHash(cert2.RawData);
#endif
				m_sslPolicyErrors = rcbPolicyErrors;
				return true;
			}

			return rcbPolicyErrors == SslPolicyErrors.None;
		}

		// use the client's callback (if any) for Preferred or Required mode
		RemoteCertificateValidationCallback validateRemoteCertificate = ValidateRemoteCertificate;
		if (connection.RemoteCertificateValidationCallback is not null)
		{
			if (caCertificateChain is not null)
			{
				Log.NotUsingRemoteCertificateValidationCallbackDueToSslCa(m_logger, Id);
			}
			else if (cs.SslMode is not MySqlSslMode.Preferred and not MySqlSslMode.Required)
			{
				Log.NotUsingRemoteCertificateValidationCallbackDueToSslMode(m_logger, Id, cs.SslMode);
			}
			else
			{
				Log.UsingRemoteCertificateValidationCallback(m_logger, Id);
				validateRemoteCertificate = connection.RemoteCertificateValidationCallback;
			}
		}

		var sslStream = clientCertificates is null ? new SslStream(m_stream!, false, validateRemoteCertificate) :
			new SslStream(m_stream!, false, validateRemoteCertificate, ValidateLocalCertificate);

		var checkCertificateRevocation = cs.SslMode == MySqlSslMode.VerifyFull;

		using (var initSsl = HandshakeResponse41Payload.CreateWithSsl(serverCapabilities, cs, m_compressionMethod, m_characterSet))
			await SendReplyAsync(initSsl, ioBehavior, cancellationToken).ConfigureAwait(false);

		var clientAuthenticationOptions = new SslClientAuthenticationOptions
		{
			EnabledSslProtocols = sslProtocols,
			ClientCertificates = clientCertificates,
			TargetHost = HostName,
			CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
		};

#if NETCOREAPP3_0_OR_GREATER
#pragma warning disable CA1416 // Validate platform compatibility
		if (cs.TlsCipherSuites is { Count: > 0 })
			clientAuthenticationOptions.CipherSuitesPolicy = new CipherSuitesPolicy(cs.TlsCipherSuites);
#pragma warning restore CA1416 // Validate platform compatibility
#endif

		try
		{
			if (ioBehavior == IOBehavior.Asynchronous)
			{
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
				await sslStream.AuthenticateAsClientAsync(clientAuthenticationOptions, cancellationToken).ConfigureAwait(false);
#else
				await sslStream.AuthenticateAsClientAsync(clientAuthenticationOptions.TargetHost,
					clientAuthenticationOptions.ClientCertificates,
					clientAuthenticationOptions.EnabledSslProtocols,
					checkCertificateRevocation).ConfigureAwait(false);
#endif
			}
			else
			{
#if NET5_0_OR_GREATER
				sslStream.AuthenticateAsClient(clientAuthenticationOptions);
#else
				sslStream.AuthenticateAsClient(clientAuthenticationOptions.TargetHost,
					clientAuthenticationOptions.ClientCertificates,
					clientAuthenticationOptions.EnabledSslProtocols,
					checkCertificateRevocation);
#endif
			}
			var sslByteHandler = new StreamByteHandler(sslStream);
			m_payloadHandler!.ByteHandler = sslByteHandler;
			m_isSecureConnection = true;
			m_sslStream = sslStream;
			if (m_sslPolicyErrors != SslPolicyErrors.None)
			{
#if NETCOREAPP3_0_OR_GREATER
				Log.ConnectedTlsBasicPreliminary(m_logger, Id, m_sslPolicyErrors, sslStream.SslProtocol, sslStream.NegotiatedCipherSuite);
#else
				Log.ConnectedTlsDetailedPreliminary(m_logger, Id, m_sslPolicyErrors, sslStream.SslProtocol, sslStream.CipherAlgorithm, sslStream.HashAlgorithm, sslStream.KeyExchangeAlgorithm, sslStream.KeyExchangeStrength);
#endif
			}
			else
			{
#if NETCOREAPP3_0_OR_GREATER
				Log.ConnectedTlsBasic(m_logger, Id, sslStream.SslProtocol, sslStream.NegotiatedCipherSuite);
#else
				Log.ConnectedTlsDetailed(m_logger, Id, sslStream.SslProtocol, sslStream.CipherAlgorithm, sslStream.HashAlgorithm, sslStream.KeyExchangeAlgorithm, sslStream.KeyExchangeStrength);
#endif
			}
		}
		catch (Exception ex)
		{
			if (ex is not OperationCanceledException)
				Log.CouldNotInitializeTlsConnection(m_logger, ex, Id);
			sslStream.Dispose();
			ShutdownSocket();
			HostName = "";
			lock (m_lock)
				m_state = State.Failed;
			if (ex is AuthenticationException)
				throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "SSL Authentication Error", ex);
			if (ex is IOException && clientCertificates is not null)
				throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "MySQL Server rejected client certificate", ex);
			if (ex is Win32Exception { NativeErrorCode: -2146893007 }) // SEC_E_ALGORITHM_MISMATCH (0x80090331)
				throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "The server doesn't support the client's specified TLS versions.", ex);
			throw;
		}
		finally
		{
			caCertificateChain?.Dispose();
		}

		// Returns a X509CertificateCollection containing the single certificate contained in 'sslKeyFile' (PEM private key) and 'sslCertificateFile' (PEM certificate).
		X509CertificateCollection LoadCertificate(string sslKeyFile, string sslCertificateFile)
		{
#if NETSTANDARD2_0
			throw new NotSupportedException("SslCert and SslKey connection string options are not supported in netstandard2.0.");
#elif NET5_0_OR_GREATER
			m_clientCertificate = X509Certificate2.CreateFromPemFile(sslCertificateFile, sslKeyFile);
			if (Utility.IsWindows())
			{
				// Schannel has a bug where ephemeral keys can't be loaded: https://github.com/dotnet/runtime/issues/23749#issuecomment-485947319
				// The workaround is to export the key (which may make it "Perphemeral"): https://github.com/dotnet/runtime/issues/23749#issuecomment-739895373
				var oldCertificate = m_clientCertificate;
#if NET9_0_OR_GREATER
				m_clientCertificate = X509CertificateLoader.LoadPkcs12(m_clientCertificate.Export(X509ContentType.Pkcs12, default(string?)), null);
#else
				m_clientCertificate = new X509Certificate2(m_clientCertificate.Export(X509ContentType.Pkcs12));
#endif
				oldCertificate.Dispose();
			}
			return [m_clientCertificate];
#else
			Log.LoadingClientKeyFromKeyFile(m_logger, Id, sslKeyFile);
			string keyPem;
			try
			{
				keyPem = File.ReadAllText(sslKeyFile);
			}
			catch (Exception ex)
			{
				Log.CouldNotLoadClientKeyFromKeyFile(m_logger, ex, Id, sslKeyFile);
				throw new MySqlException($"Could not load the client key from '{sslKeyFile}'", ex);
			}

			RSAParameters rsaParameters;
			try
			{
				rsaParameters = Utility.GetRsaParameters(keyPem);
			}
			catch (FormatException ex)
			{
				Log.CouldNotLoadClientKeyFromKeyFile(m_logger, ex, Id, sslKeyFile);
				throw new MySqlException($"Could not load the client key from '{sslKeyFile}'", ex);
			}

			try
			{
				RSA rsa;
				try
				{
					// SslStream on Windows needs a KeyContainerName to be set
					var csp = new CspParameters
					{
						KeyContainerName = Guid.NewGuid().ToString(),
					};
					rsa = new RSACryptoServiceProvider(csp)
					{
						PersistKeyInCsp = true,
					};
				}
				catch (PlatformNotSupportedException)
				{
					rsa = RSA.Create();
				}
				rsa.ImportParameters(rsaParameters);

#if NET462 || NET471
				var certificate = new X509Certificate2(sslCertificateFile, "", X509KeyStorageFlags.MachineKeySet)
				{
					PrivateKey = rsa,
				};
#else
				X509Certificate2 certificate;
				using (var publicCertificate = new X509Certificate2(sslCertificateFile))
					certificate = publicCertificate.CopyWithPrivateKey(rsa);
#endif

				m_clientCertificate = certificate;
				return [certificate];
			}
			catch (CryptographicException ex)
			{
				Log.CouldNotLoadClientKeyFromKeyFile(m_logger, ex, Id, sslCertificateFile);
				if (!File.Exists(sslCertificateFile))
					throw new MySqlException("Cannot find client certificate file: " + sslCertificateFile, ex);
				throw new MySqlException("Could not load the client key from " + sslCertificateFile, ex);
			}
#endif
			}
	}

#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
	// a stripped-down version of this POCO options class for TFMs that don't have it built in
	internal sealed class SslClientAuthenticationOptions
	{
		public X509RevocationMode CertificateRevocationCheckMode { get; set; }
		public X509CertificateCollection? ClientCertificates { get; set; }
		public SslProtocols EnabledSslProtocols { get; set; }
		public string? TargetHost { get; set; }
	}
#endif

	// Some servers are exposed through a proxy, which handles the initial handshake and gives the proxy's
	// server version and thread ID. Detect this situation and return `true` if the real server's details should
	// be requested after connecting (which takes an extra roundtrip).
	private bool ShouldGetRealServerDetails(ConnectionSettings cs)
	{
		// currently hardcoded to the version(s) returned by the Azure Database for MySQL proxy
		if (ServerVersion.OriginalString is "5.6.47.0" or "5.6.42.0" or "5.6.39.0")
			return true;

		// detect Azure Database for MySQL DNS suffixes, if a "user@host" user ID is being used
		if (cs.ConnectionProtocol == MySqlConnectionProtocol.Sockets && cs.UserID.Contains('@'))
		{
			return HostName.EndsWith(".mysql.database.azure.com", StringComparison.OrdinalIgnoreCase) ||
				HostName.EndsWith(".database.windows.net", StringComparison.OrdinalIgnoreCase) ||
				HostName.EndsWith(".mysql.database.chinacloudapi.cn", StringComparison.OrdinalIgnoreCase);
		}

		// detect AWS RDS Proxy, if hostname like <name>.proxy-<random-chars>.<region>.rds.amazonaws.com
		if (HostName.EndsWith(".rds.amazonaws.com", StringComparison.OrdinalIgnoreCase) &&
			HostName.AsSpan().Contains(".proxy-".AsSpan(), StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return false;
	}

	private async Task GetRealServerDetailsAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Log.DetectedProxy(m_logger, Id);
		try
		{
			var payload = SupportsQueryAttributes ? s_selectConnectionIdVersionWithAttributesPayload : s_selectConnectionIdVersionNoAttributesPayload;
			await SendAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);

			// column count: 2
			_ = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			// CONNECTION_ID() and VERSION() columns
			_ = await ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
			_ = await ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);

			if (!SupportsDeprecateEof)
			{
				payload = await ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
				_ = EofPayload.Create(payload.Span);
			}

			// first (and only) row
			payload = await ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);

			var reader = new ByteArrayReader(payload.Span);
			var length = reader.ReadLengthEncodedIntegerOrNull();
			var connectionId = (length != -1 && Utf8Parser.TryParse(reader.ReadByteString(length), out int id, out _)) ? id : default(int?);
			length = reader.ReadLengthEncodedIntegerOrNull();
			var serverVersion = length != -1 ? new ServerVersion(reader.ReadByteString(length)) : default;

			// OK/EOF payload
			payload = await ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
			if (OkPayload.IsOk(payload.Span, this))
				OkPayload.Verify(payload.Span, this);
			else
				EofPayload.Create(payload.Span);

			if (connectionId is int newConnectionId && serverVersion is not null)
			{
				Log.ChangingConnectionId(m_logger, Id, ConnectionId, newConnectionId, ServerVersion.OriginalString, serverVersion.OriginalString);
				ConnectionId = newConnectionId;
				ServerVersion = serverVersion;
			}
		}
		catch (MySqlException ex)
		{
			Log.FailedToGetConnectionId(m_logger, ex, Id);
		}
	}

	private void ShutdownSocket()
	{
		Log.ClosingStreamSocket(m_logger, Id);
		Utility.Dispose(ref m_payloadHandler);
		Utility.Dispose(ref m_stream);
		SafeDispose(ref m_tcpClient);
		SafeDispose(ref m_socket);
		Utility.Dispose(ref m_clientCertificate);
		m_activityTags.Clear();
	}

	/// <summary>
	/// Disposes and sets <paramref name="disposable"/> to <c>null</c>, ignoring any
	/// <see cref="IOException"/> or <see cref="SocketException"/> that is thrown.
	/// </summary>
	/// <typeparam name="T">An <see cref="IDisposable"/> type.</typeparam>
	/// <param name="disposable">The object to dispose.</param>
	private static void SafeDispose<T>(ref T? disposable)
		where T : class, IDisposable
	{
		if (disposable is not null)
		{
			try
			{
				disposable.Dispose();
			}
			catch (IOException)
			{
			}
			catch (SocketException)
			{
			}
			disposable = null;
		}
	}

	internal void SetFailed(Exception exception)
	{
		Log.SettingStateToFailed(m_logger, exception, Id);
		lock (m_lock)
			m_state = State.Failed;
		if (OwningConnection is not null && OwningConnection.TryGetTarget(out var connection))
			connection.SetState(ConnectionState.Broken);
	}

	private void VerifyState(State state)
	{
		if (m_state != state)
		{
			ExpectedSessionState1(m_logger, Id, state, m_state);
			throw new InvalidOperationException($"Expected state to be {state} but was {m_state}.");
		}
	}

	private void VerifyState(State state1, State state2, State state3, State state4, State state5, State state6)
	{
		if (m_state != state1 && m_state != state2 && m_state != state3 && m_state != state4 && m_state != state5 && m_state != state6)
		{
			ExpectedSessionState6(m_logger, Id, state1, state2, state3, state4, state5, state6, m_state);
			throw new InvalidOperationException($"Expected state to be ({state1}|{state2}|{state3}|{state4}|{state5}|{state6}) but was {m_state}.");
		}
	}

	internal bool SslIsEncrypted => m_sslStream?.IsEncrypted is true;

	internal bool SslIsSigned => m_sslStream?.IsSigned is true;

	internal bool SslIsAuthenticated => m_sslStream?.IsAuthenticated is true;

	internal bool SslIsMutuallyAuthenticated => m_sslStream?.IsMutuallyAuthenticated is true;

	internal SslProtocols SslProtocol => m_sslStream?.SslProtocol ?? SslProtocols.None;

	private byte[] CreateConnectionAttributes(string? programName)
	{
		Log.CreatingConnectionAttributes(m_logger, Id);
		var attributesWriter = new ByteBufferWriter();
		attributesWriter.WriteLengthEncodedString("_client_name");
		attributesWriter.WriteLengthEncodedString("MySqlConnector");
		attributesWriter.WriteLengthEncodedString("_client_version");

		var version = typeof(ServerSession).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
		var plusIndex = version.IndexOf('+');
		if (plusIndex != -1)
			version = version[..plusIndex];
		attributesWriter.WriteLengthEncodedString(version);

		try
		{
			Utility.GetOSDetails(out var os, out var osDescription, out var architecture);
			if (os is not null)
			{
				attributesWriter.WriteLengthEncodedString("_os");
				attributesWriter.WriteLengthEncodedString(os);
			}
			attributesWriter.WriteLengthEncodedString("_os_details");
			attributesWriter.WriteLengthEncodedString(osDescription);
			attributesWriter.WriteLengthEncodedString("_platform");
			attributesWriter.WriteLengthEncodedString(architecture);
		}
		catch (PlatformNotSupportedException)
		{
		}
#if NET5_0_OR_GREATER
		var processId = Environment.ProcessId;
#else
		using var process = Process.GetCurrentProcess();
		var processId = process.Id;
#endif
		attributesWriter.WriteLengthEncodedString("_pid");
		attributesWriter.WriteLengthEncodedString(processId.ToString(CultureInfo.InvariantCulture));
		if (!string.IsNullOrEmpty(programName))
		{
			attributesWriter.WriteLengthEncodedString("program_name");
			attributesWriter.WriteLengthEncodedString(programName!);
		}
		using var connectionAttributesPayload = attributesWriter.ToPayloadData();
		var connectionAttributes = connectionAttributesPayload.Span;
		var writer = new ByteBufferWriter(connectionAttributes.Length + 9);
		writer.WriteLengthEncodedInteger((ulong) connectionAttributes.Length);
		writer.Write(connectionAttributes);
		using var payload = writer.ToPayloadData();
		return payload.Memory.ToArray();
	}

	private MySqlException CreateExceptionForErrorPayload(ReadOnlySpan<byte> span)
	{
		var errorPayload = ErrorPayload.Create(span);
		Log.ErrorPayload(m_logger, Id, errorPayload.ErrorCode, errorPayload.State, errorPayload.Message);
		var exception = errorPayload.ToException();
		if (exception.ErrorCode is MySqlErrorCode.ClientInteractionTimeout)
			SetFailed(exception);
		return exception;
	}

	private void ClearPreparedStatements()
	{
		if (m_preparedStatements is not null)
		{
			foreach (var pair in m_preparedStatements)
				pair.Value.Dispose();
			m_preparedStatements.Clear();
		}
	}

	private string GetPassword(ConnectionSettings cs, MySqlConnection connection)
	{
		if (cs.Password.Length != 0)
			return cs.Password;

		if (connection.ProvidePasswordCallback is { } passwordProvider)
		{
			try
			{
				Log.ObtainingPasswordViaProvidePasswordCallback(m_logger, Id);
				return passwordProvider(new(HostName, cs.Port, cs.UserID, cs.Database));
			}
			catch (Exception ex)
			{
				Log.FailedToObtainPassword(m_logger, ex, Id, ex.Message);
				throw new MySqlException(MySqlErrorCode.ProvidePasswordCallbackFailed, "Failed to obtain password via ProvidePasswordCallback", ex);
			}
		}

		return "";
	}

	private enum State
	{
		// The session has been created; no connection has been made.
		Created,

		// The session is attempting to connect to a server.
		Connecting,

		// The session is connected to a server; there is no active query.
		Connected,

		// The session is connected to a server and a query is being made.
		Querying,

		// The session is connected to a server and the active query is being cancelled.
		CancelingQuery,

		// A cancellation is pending on the server and needs to be cleared.
		ClearingPendingCancellation,

		// The session is closing.
		Closing,

		// The session is closed.
		Closed,

		// An unexpected error occurred; the session is in an unusable state.
		Failed,
	}

	private sealed class DelimiterSqlParser(IMySqlCommand command)
		: SqlParser(new StatementPreparer(command.CommandText!, null, command.CreateStatementPreparerOptions()))
	{
		public bool HasDelimiter { get; private set; }

		protected override void OnStatementBegin(int index)
		{
			if (index + 10 < Sql.Length && Sql.AsSpan(index, 10).Equals("delimiter ".AsSpan(), StringComparison.OrdinalIgnoreCase))
				HasDelimiter = true;
		}

		private string Sql { get; } = command.CommandText!;
	}

	[LoggerMessage(EventIds.CannotExecuteNewCommandInState, LogLevel.Error, "Session {SessionId} can't execute new command when in state {SessionState}")]
	private static partial void CannotExecuteNewCommandInState(ILogger logger, string sessionId, State sessionState);

	[LoggerMessage(EventIds.EnteringFinishQuerying, LogLevel.Trace, "Session {SessionId} entering FinishQuerying; state is {SessionState}")]
	private static partial void EnteringFinishQuerying(ILogger logger, string sessionId, State sessionState);

	[LoggerMessage(EventIds.ExpectedSessionState1, LogLevel.Error, "Session {SessionId} should have state {ExpectedState1} but was {SessionState}")]
	private static partial void ExpectedSessionState1(ILogger logger, string sessionId, State expectedState1, State sessionState);

	[LoggerMessage(EventIds.ExpectedSessionState6, LogLevel.Error, "Session {SessionId} should have state {ExpectedState1} or {ExpectedState2} or {ExpectedState3} or {ExpectedState4} or {ExpectedState5} or {ExpectedState6} but was {SessionState}")]
	private static partial void ExpectedSessionState6(ILogger logger, string sessionId, State expectedState1, State expectedState2, State expectedState3, State expectedState4, State expectedState5, State expectedState6, State sessionState);

	private static readonly PayloadData s_setNamesUtf8NoAttributesPayload = QueryPayload.Create(false, "SET NAMES utf8;"u8);
	private static readonly PayloadData s_setNamesUtf8mb4NoAttributesPayload = QueryPayload.Create(false, "SET NAMES utf8mb4;"u8);
	private static readonly PayloadData s_setNamesUtf8WithAttributesPayload = QueryPayload.Create(true, "SET NAMES utf8;"u8);
	private static readonly PayloadData s_setNamesUtf8mb4WithAttributesPayload = QueryPayload.Create(true, "SET NAMES utf8mb4;"u8);
	private static readonly PayloadData s_sleepNoAttributesPayload = QueryPayload.Create(false, "SELECT SLEEP(0) INTO @__MySqlConnector__Sleep;"u8);
	private static readonly PayloadData s_sleepWithAttributesPayload = QueryPayload.Create(true, "SELECT SLEEP(0) INTO @__MySqlConnector__Sleep;"u8);
	private static readonly PayloadData s_selectConnectionIdVersionNoAttributesPayload = QueryPayload.Create(false, "SELECT CONNECTION_ID(), VERSION();"u8);
	private static readonly PayloadData s_selectConnectionIdVersionWithAttributesPayload = QueryPayload.Create(true, "SELECT CONNECTION_ID(), VERSION();"u8);

	private readonly ILogger m_logger;
#if NET9_0_OR_GREATER
	private readonly Lock m_lock;
#else
	private readonly object m_lock;
#endif
	private readonly ArraySegmentHolder<byte> m_payloadCache;
	private readonly ActivityTagsCollection m_activityTags;
	private State m_state;
	private TcpClient? m_tcpClient;
	private Socket? m_socket;
	private Stream? m_stream;
	private SslStream? m_sslStream;
	private X509Certificate2? m_clientCertificate;
	private IPayloadHandler? m_payloadHandler;
	private CompressionMethod m_compressionMethod;
	private bool m_isSecureConnection;
	private bool m_isLoopbackConnection;
	private bool m_supportsConnectionAttributes;
	private bool m_supportsPipelining;
	private CharacterSet m_characterSet;
	private PayloadData m_setNamesPayload;
	private byte[]? m_pipelinedResetConnectionBytes;
	private Dictionary<string, PreparedStatements>? m_preparedStatements;
	private byte[]? m_passwordHash;
	private byte[]? m_remoteCertificateSha2Thumbprint;
	private SslPolicyErrors m_sslPolicyErrors;
}
