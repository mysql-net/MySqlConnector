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
using MySqlConnector.Authentication;
using MySqlConnector.Logging;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable

internal sealed class ServerSession
{
	public ServerSession()
		: this(null, 0, Interlocked.Increment(ref s_lastId))
	{
	}

	public ServerSession(ConnectionPool? pool, int poolGeneration, int id)
	{
		m_lock = new();
		m_payloadCache = new();
		Id = (pool?.Id ?? 0) + "." + id;
		ServerVersion = ServerVersion.Empty;
		CreatedTicks = unchecked((uint) Environment.TickCount);
		Pool = pool;
		PoolGeneration = poolGeneration;
		HostName = "";
		m_logArguments = new object?[] { "{0}".FormatInvariant(Id), null };
		m_activityTags = new ActivityTagsCollection
		{
			{ ActivitySourceHelper.DatabaseSystemTagName, ActivitySourceHelper.DatabaseSystemValue },
		};
		Log.Trace("Session{0} created new session", m_logArguments);
	}

	public string Id { get; }
	public ServerVersion ServerVersion { get; set; }
	public int ActiveCommandId { get; private set; }
	public int CancellationTimeout { get; private set; }
	public int ConnectionId { get; set; }
	public byte[]? AuthPluginData { get; set; }
	public uint CreatedTicks { get; }
	public ConnectionPool? Pool { get; }
	public int PoolGeneration { get; }
	public uint LastReturnedTicks { get; private set; }
	public string? DatabaseOverride { get; set; }
	public string HostName { get; private set; }
	public IPAddress? IPAddress => (m_tcpClient?.Client.RemoteEndPoint as IPEndPoint)?.Address;
	public WeakReference<MySqlConnection>? OwningConnection { get; set; }
	public bool SupportsComMulti => m_supportsComMulti;
	public bool SupportsDeprecateEof => m_supportsDeprecateEof;
	public bool SupportsQueryAttributes { get; private set; }
	public bool SupportsSessionTrack => m_supportsSessionTrack;
	public bool ProcAccessDenied { get; set; }
	public ICollection<KeyValuePair<string, object?>> ActivityTags => m_activityTags;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public ValueTask ReturnToPoolAsync(IOBehavior ioBehavior, MySqlConnection? owningConnection)
#else
	public ValueTask<int> ReturnToPoolAsync(IOBehavior ioBehavior, MySqlConnection? owningConnection)
#endif
	{
		if (Log.IsTraceEnabled())
		{
			m_logArguments[1] = Pool?.Id;
			Log.Trace("Session{0} returning to Pool{1}", m_logArguments);
		}
		LastReturnedTicks = unchecked((uint) Environment.TickCount);
		return Pool is null ? default : Pool.ReturnAsync(ioBehavior, this);
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

		Log.Debug("Session{0} will cancel CommandId: {1} (CancelledAttempts={2}) CommandText: {3}", m_logArguments[0], command.CommandId, command.CancelAttemptCount, (command as MySqlCommand)?.CommandText);
		return true;
	}

	public void DoCancel(ICancellableCommand commandToCancel, MySqlCommand killCommand)
	{
		Log.Info("Session{0} canceling CommandId {1} from Session{2}; CommandText: {3}", m_logArguments[0], commandToCancel.CommandId, killCommand.Connection!.Session.Id, (commandToCancel as MySqlCommand)?.CommandText);
		lock (m_lock)
		{
			if (ActiveCommandId != commandToCancel.CommandId)
			{
				Log.Debug("Session{0} ActiveCommandId {1} is not the CommandId {2} being canceled; ignoring cancellation.", m_logArguments[0], ActiveCommandId, commandToCancel.CommandId);
				return;
			}

			// NOTE: This command is executed while holding the lock to prevent race conditions during asynchronous cancellation.
			// For example, if the lock weren't held, the current command could finish and the other thread could set ActiveCommandId
			// to zero, then start executing a new command. By the time this "KILL QUERY" command reached the server, the wrong
			// command would be killed (because "KILL QUERY" specifies the connection whose command should be killed, not
			// a unique identifier of the command itself). As a mitigation, we set the CommandTimeout to a low value to avoid
			// blocking the other thread for an extended duration.
			Log.Debug("Session{0} canceling CommandId {1} with CommandText {2}", killCommand.Connection!.Session.Id, commandToCancel.CommandId, killCommand.CommandText);
			killCommand.ExecuteNonQuery();
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
				throw new MySqlException("Procedure or function '{0}' cannot be found in database '{1}'.".FormatInvariant(name.Component, name.Schema));
			}

			var parameterCount = cachedProcedure.Parameters.Count;
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
			commandToPrepare = string.Create(commandText.Length + 7 + parameterCount * 2 + (parameterCount == 0 ? 1 : 0), (commandText, parameterCount), static (buffer, state) =>
			{
				buffer[0] = 'C';
				buffer[1] = 'A';
				buffer[2] = 'L';
				buffer[3] = 'L';
				buffer[4] = ' ';
				buffer = buffer.Slice(5);
				state.commandText.AsSpan().CopyTo(buffer);
				buffer = buffer.Slice(state.commandText.Length);
				buffer[0] = '(';
				buffer = buffer.Slice(1);
				if (state.parameterCount > 0)
				{
					buffer[0] = '?';
					buffer = buffer.Slice(1);
					for (var i = 1; i < state.parameterCount; i++)
					{
						buffer[0] = ',';
						buffer[1] = '?';
						buffer = buffer.Slice(2);
					}
				}
				buffer[0] = ')';
				buffer[1] = ';';
			});
#else
			var callStatement = new StringBuilder("CALL ", commandText.Length + 8 + parameterCount * 2);
			callStatement.Append(commandText);
			callStatement.Append('(');
			for (int i = 0; i < parameterCount; i++)
				callStatement.Append("?,");
			if (parameterCount == 0)
				callStatement.Append(')');
			else
				callStatement[callStatement.Length - 1] = ')';
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
					payload.Span.CopyTo(columnsAndParameters.Array.AsSpan().Slice(columnsAndParametersSize));
					parameters[i] = ColumnDefinitionPayload.Create(new(columnsAndParameters, columnsAndParametersSize, payloadLength));
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
					payload.Span.CopyTo(columnsAndParameters.Array.AsSpan().Slice(columnsAndParametersSize));
					columns[i] = ColumnDefinitionPayload.Create(new(columnsAndParameters, columnsAndParametersSize, payloadLength));
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

		m_preparedStatements ??= new();
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
				m_logArguments[1] = m_state;
				Log.Error("Session{0} can't execute new command when in SessionState: {1}", m_logArguments[0], m_state);
				throw new InvalidOperationException("This MySqlConnection is already in use. See https://fl.vu/mysql-conn-reuse");
			}

			VerifyState(State.Connected);
			m_state = State.Querying;

			command.CancelAttemptCount = 0;
			ActiveCommandId = command.CommandId;
		}
	}

	public void FinishQuerying()
	{
		m_logArguments[1] = m_state;
		Log.Trace("Session{0} entering FinishQuerying; SessionState={1}", m_logArguments);
		bool clearConnection = false;
		lock (m_lock)
		{
			if (m_state == State.CancelingQuery)
			{
				m_state = State.ClearingPendingCancellation;
				clearConnection = true;
			}
		}

		if (clearConnection)
		{
			// KILL QUERY will kill a subsequent query if the command it was intended to cancel has already completed.
			// In order to handle this case, we issue a dummy query that will consume the pending cancellation.
			// See https://bugs.mysql.com/bug.php?id=45679
			Log.Debug("Session{0} sending 'SLEEP(0)' command to clear pending cancellation", m_logArguments);
			var payload = SupportsQueryAttributes ? s_sleepWithAttributesPayload : s_sleepNoAttributesPayload;
#pragma warning disable CA2012 // Safe because method completes synchronously
			SendAsync(payload, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
			payload = ReceiveReplyAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
			OkPayload.Create(payload.Span, SupportsDeprecateEof, SupportsSessionTrack);
		}

		lock (m_lock)
		{
			if (m_state is State.Querying or State.ClearingPendingCancellation)
				m_state = State.Connected;
			else
				VerifyState(State.Failed);
			ActiveCommandId = 0;
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
					Log.Trace("Session{0} sending QUIT command", m_logArguments);
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

	public async Task<string?> ConnectAsync(ConnectionSettings cs, MySqlConnection connection, int startTickCount, ILoadBalancer? loadBalancer, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		string? statusInfo = null;

		try
		{
			lock (m_lock)
			{
				VerifyState(State.Created);
				m_state = State.Connecting;
			}

			// TLS negotiation should automatically fall back to the best version supported by client and server. However,
			// Windows Schannel clients will fail to connect to a yaSSL-based MySQL Server if TLS 1.2 is requested and
			// have to use only TLS 1.1: https://github.com/mysql-net/MySqlConnector/pull/101
			// In order to use the best protocol possible (i.e., not always default to TLS 1.1), we try the OS-default protocol
			// (which is SslProtocols.None; see https://docs.microsoft.com/en-us/dotnet/framework/network-programming/tls),
			// then fall back to SslProtocols.Tls11 if that fails and it's possible that the cause is a yaSSL server.
			bool shouldRetrySsl;
			var sslProtocols = Pool?.SslProtocols ?? cs.TlsVersions;
			PayloadData payload;
			InitialHandshakePayload initialHandshake;
			do
			{
				bool tls11or10Supported = (sslProtocols & (SslProtocols.Tls | SslProtocols.Tls11)) != SslProtocols.None;
				bool tls12Supported = (sslProtocols & SslProtocols.Tls12) == SslProtocols.Tls12;
				shouldRetrySsl = (sslProtocols == SslProtocols.None || (tls12Supported && tls11or10Supported)) && Utility.IsWindows();

				var connected = false;
				if (cs.ConnectionProtocol == MySqlConnectionProtocol.Sockets)
					connected = await OpenTcpSocketAsync(cs, loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer)), ioBehavior, cancellationToken).ConfigureAwait(false);
				else if (cs.ConnectionProtocol == MySqlConnectionProtocol.UnixSocket)
					connected = await OpenUnixSocketAsync(cs, ioBehavior, cancellationToken).ConfigureAwait(false);
				else if (cs.ConnectionProtocol == MySqlConnectionProtocol.NamedPipe)
					connected = await OpenNamedPipeAsync(cs, startTickCount, ioBehavior, cancellationToken).ConfigureAwait(false);
				if (!connected)
				{
					lock (m_lock)
						m_state = State.Failed;
					Log.Error("Session{0} connecting failed", m_logArguments);
					throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Unable to connect to any of the specified MySQL hosts.");
				}

				var byteHandler = m_socket is null ? new StreamByteHandler(m_stream!) : (IByteHandler) new SocketByteHandler(m_socket);
				if (cs.ConnectionTimeout != 0)
					byteHandler.RemainingTimeout = Math.Max(1, cs.ConnectionTimeoutMilliseconds - unchecked(Environment.TickCount - startTickCount));
				m_payloadHandler = new StandardPayloadHandler(byteHandler);

				payload = await ReceiveAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				initialHandshake = InitialHandshakePayload.Create(payload.Span);

				// if PluginAuth is supported, then use the specified auth plugin; else, fall back to protocol capabilities to determine the auth type to use
				string authPluginName;
				if ((initialHandshake.ProtocolCapabilities & ProtocolCapabilities.PluginAuth) != 0)
					authPluginName = initialHandshake.AuthPluginName!;
				else
					authPluginName = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.SecureConnection) == 0 ? "mysql_old_password" : "mysql_native_password";
				m_logArguments[1] = authPluginName;
				Log.Trace("Session{0} server sent AuthPluginName={1}", m_logArguments);
				if (authPluginName != "mysql_native_password" && authPluginName != "sha256_password" && authPluginName != "caching_sha2_password")
				{
					Log.Error("Session{0} unsupported authentication method AuthPluginName={1}", m_logArguments);
					throw new NotSupportedException("Authentication method '{0}' is not supported.".FormatInvariant(initialHandshake.AuthPluginName));
				}

				ServerVersion = new(initialHandshake.ServerVersion);
				ConnectionId = initialHandshake.ConnectionId;
				AuthPluginData = initialHandshake.AuthPluginData;
				m_useCompression = cs.UseCompression && (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.Compress) != 0;
				CancellationTimeout = cs.CancellationTimeout;

				m_supportsComMulti = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.MariaDbComMulti) != 0;
				m_supportsConnectionAttributes = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.ConnectionAttributes) != 0;
				m_supportsDeprecateEof = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.DeprecateEof) != 0;
				SupportsQueryAttributes = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.QueryAttributes) != 0;
				m_supportsSessionTrack = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.SessionTrack) != 0;
				var serverSupportsSsl = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.Ssl) != 0;
				m_characterSet = ServerVersion.Version >= ServerVersions.SupportsUtf8Mb4 ? CharacterSet.Utf8Mb4GeneralCaseInsensitive : CharacterSet.Utf8GeneralCaseInsensitive;
				m_setNamesPayload = ServerVersion.Version >= ServerVersions.SupportsUtf8Mb4 ?
					(SupportsQueryAttributes ? s_setNamesUtf8mb4WithAttributesPayload : s_setNamesUtf8mb4NoAttributesPayload) :
					(SupportsQueryAttributes ? s_setNamesUtf8WithAttributesPayload : s_setNamesUtf8NoAttributesPayload);

				// disable pipelining for RDS MySQL 5.7 (assuming Aurora); otherwise take it from the connection string or default to true
				if (!cs.Pipelining.HasValue && ServerVersion.Version.Major == 5 && ServerVersion.Version.Minor == 7 && HostName.EndsWith(".rds.amazonaws.com", StringComparison.OrdinalIgnoreCase))
				{
					m_logArguments[1] = HostName;
					Log.Debug("Session{0} auto-detected Aurora 5.7 at '{1}'; disabling pipelining", m_logArguments);
					m_supportsPipelining = false;
				}
				else
				{
					// pipelining is not currently compatible with compression
					m_supportsPipelining = !cs.UseCompression && (cs.Pipelining ?? true);

					// for pipelining, concatenate reset connection and SET NAMES query into one buffer
					if (m_supportsPipelining)
					{
						m_pipelinedResetConnectionBytes = new byte[m_setNamesPayload.Span.Length + 9];

						// first packet: reset connection
						m_pipelinedResetConnectionBytes[0] = 1;
						m_pipelinedResetConnectionBytes[4] = (byte) CommandKind.ResetConnection;

						// second packet: SET NAMES query
						m_pipelinedResetConnectionBytes[5] = (byte) m_setNamesPayload.Span.Length;
						m_setNamesPayload.Span.CopyTo(m_pipelinedResetConnectionBytes.AsSpan().Slice(9));
					}
				}

				Log.Debug("Session{0} made connection; ServerVersion={1}; ConnectionId={2}; Compression={3}; Attributes={4}; DeprecateEof={5}; Ssl={6}; SessionTrack={7}; Pipelining={8}; QueryAttributes={9}",
					m_logArguments[0], ServerVersion.OriginalString, ConnectionId,
					m_useCompression, m_supportsConnectionAttributes, m_supportsDeprecateEof, serverSupportsSsl, m_supportsSessionTrack, m_supportsPipelining, SupportsQueryAttributes);

				if (cs.SslMode != MySqlSslMode.None && (cs.SslMode != MySqlSslMode.Preferred || serverSupportsSsl))
				{
					if (!serverSupportsSsl)
					{
						Log.Error("Session{0} requires SSL but server doesn't support it", m_logArguments);
						throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Server does not support SSL");
					}

					try
					{
						await InitSslAsync(initialHandshake.ProtocolCapabilities, cs, connection, sslProtocols, ioBehavior, cancellationToken).ConfigureAwait(false);
						shouldRetrySsl = false;
					}
					catch (ArgumentException ex) when (ex.ParamName == "sslProtocolType" && sslProtocols == SslProtocols.None)
					{
						Log.Debug(ex, "Session{0} doesn't support SslProtocols.None; falling back to explicitly specifying SslProtocols", m_logArguments);
						sslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
					}
					catch (Exception ex) when (shouldRetrySsl && ((ex is MySqlException && ex.InnerException is IOException) || ex is IOException))
					{
						// negotiating TLS 1.2 with a yaSSL-based server throws an exception on Windows, see comment at top of method
						Log.Debug(ex, "Session{0} failed negotiating TLS; falling back to TLS 1.1", m_logArguments);
						sslProtocols = sslProtocols == SslProtocols.None ? SslProtocols.Tls | SslProtocols.Tls11 : (SslProtocols.Tls | SslProtocols.Tls11) & sslProtocols;
						if (Pool is not null)
							Pool.SslProtocols = sslProtocols;
					}
				}
				else
				{
					shouldRetrySsl = false;
				}
			} while (shouldRetrySsl);

			if (m_supportsConnectionAttributes && cs.ConnectionAttributes is null)
				cs.ConnectionAttributes = CreateConnectionAttributes(cs.ApplicationName);

			var password = GetPassword(cs, connection);
			using (var handshakeResponsePayload = HandshakeResponse41Payload.Create(initialHandshake, cs, password, m_useCompression, m_characterSet, m_supportsConnectionAttributes ? cs.ConnectionAttributes : null))
				await SendReplyAsync(handshakeResponsePayload, ioBehavior, cancellationToken).ConfigureAwait(false);
			payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			// if server doesn't support the authentication fast path, it will send a new challenge
			if (payload.HeaderByte == AuthenticationMethodSwitchRequestPayload.Signature)
			{
				payload = await SwitchAuthenticationAsync(cs, password, payload, ioBehavior, cancellationToken).ConfigureAwait(false);
			}

			var ok = OkPayload.Create(payload.Span, SupportsDeprecateEof, SupportsSessionTrack);
			statusInfo = ok.StatusInfo;

			if (m_useCompression)
				m_payloadHandler = new CompressedPayloadHandler(m_payloadHandler.ByteHandler);

			// set 'collation_connection' to the server default
			await SendAsync(m_setNamesPayload, ioBehavior, cancellationToken).ConfigureAwait(false);
			payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			OkPayload.Create(payload.Span, SupportsDeprecateEof, SupportsSessionTrack);

			if (ShouldGetRealServerDetails(cs))
				await GetRealServerDetailsAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);

			m_payloadHandler.ByteHandler.RemainingTimeout = Constants.InfiniteTimeout;

			m_activityTags.Add(ActivitySourceHelper.DatabaseConnectionIdTagName, ConnectionId.ToString(CultureInfo.InvariantCulture));
			m_activityTags.Add(ActivitySourceHelper.DatabaseConnectionStringTagName, cs.ConnectionStringBuilder.GetConnectionString(cs.ConnectionStringBuilder.PersistSecurityInfo));
			m_activityTags.Add(ActivitySourceHelper.DatabaseUserTagName, cs.UserID);
			if (cs.Database.Length != 0)
				m_activityTags.Add(ActivitySourceHelper.DatabaseNameTagName, cs.Database);
		}
		catch (ArgumentException ex)
		{
			Log.Error(ex, "Session{0} couldn't connect to server", m_logArguments);
			throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Couldn't connect to server", ex);
		}
		catch (IOException ex)
		{
			Log.Error(ex, "Session{0} couldn't connect to server", m_logArguments);
			throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Couldn't connect to server", ex);
		}

		return statusInfo;
	}

	public async Task<bool> TryResetConnectionAsync(ConnectionSettings cs, MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		VerifyState(State.Connected);

		try
		{
			// clear all prepared statements; resetting the connection will clear them on the server
			ClearPreparedStatements();

			PayloadData payload;
			if (DatabaseOverride is null && (ServerVersion.Version.CompareTo(ServerVersions.SupportsResetConnection) >= 0 || ServerVersion.MariaDbVersion?.CompareTo(ServerVersions.MariaDbSupportsResetConnection) >= 0))
			{
				m_logArguments[1] = ServerVersion.OriginalString;

				if (m_supportsPipelining)
				{
					Log.Trace("Session{0} ServerVersion={1} supports reset connection and pipelining; sending pipelined reset connection request", m_logArguments);

					// send both packets at once
					await m_payloadHandler!.ByteHandler.WriteBytesAsync(m_pipelinedResetConnectionBytes!, ioBehavior).ConfigureAwait(false);

					// read two OK replies
					m_payloadHandler.SetNextSequenceNumber(1);
					payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					OkPayload.Create(payload.Span, SupportsDeprecateEof, SupportsSessionTrack);

					m_payloadHandler.SetNextSequenceNumber(1);
					payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					OkPayload.Create(payload.Span, SupportsDeprecateEof, SupportsSessionTrack);

					return true;
				}

				Log.Trace("Session{0} ServerVersion={1} supports reset connection; sending reset connection request", m_logArguments);
				await SendAsync(ResetConnectionPayload.Instance, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				OkPayload.Create(payload.Span, SupportsDeprecateEof, SupportsSessionTrack);
			}
			else
			{
				// optimistically hash the password with the challenge from the initial handshake (supported by MariaDB; doesn't appear to be supported by MySQL)
				if (DatabaseOverride is null)
				{
					m_logArguments[1] = ServerVersion.OriginalString;
					Log.Trace("Session{0} ServerVersion={1} doesn't support reset connection; sending change user request", m_logArguments);
				}
				else
				{
					m_logArguments[1] = DatabaseOverride;
					Log.Debug("Session{0} sending change user request due to changed Database={1}", m_logArguments);
					DatabaseOverride = null;
				}
				var password = GetPassword(cs, connection);
				var hashedPassword = AuthenticationUtility.CreateAuthenticationResponse(AuthPluginData!, password);
				using (var changeUserPayload = ChangeUserPayload.Create(cs.UserID, hashedPassword, cs.Database, m_characterSet, m_supportsConnectionAttributes ? cs.ConnectionAttributes : null))
					await SendAsync(changeUserPayload, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				if (payload.HeaderByte == AuthenticationMethodSwitchRequestPayload.Signature)
				{
					Log.Trace("Session{0} optimistic reauthentication failed; logging in again", m_logArguments);
					payload = await SwitchAuthenticationAsync(cs, password, payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				OkPayload.Create(payload.Span, SupportsDeprecateEof, SupportsSessionTrack);
			}

			// set 'collation_connection' to the server default
			await SendAsync(m_setNamesPayload, ioBehavior, cancellationToken).ConfigureAwait(false);
			payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			OkPayload.Create(payload.Span, SupportsDeprecateEof, SupportsSessionTrack);

			return true;
		}
		catch (IOException ex)
		{
			Log.Trace(ex, "Session{0} ignoring IOException in TryResetConnectionAsync", m_logArguments);
		}
		catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.ClientInteractionTimeout)
		{
			Log.Trace(ex, "Session{0} ignoring ClientInteractionTimeout MySqlException in TryResetConnectionAsync", m_logArguments);
		}
		catch (ObjectDisposedException ex)
		{
			Log.Trace(ex, "Session{0} ignoring ObjectDisposedException in TryResetConnectionAsync", m_logArguments);
		}
		catch (SocketException ex)
		{
			Log.Trace(ex, "Session{0} ignoring SocketException in TryResetConnectionAsync", m_logArguments);
		}

		return false;
	}

	private async Task<PayloadData> SwitchAuthenticationAsync(ConnectionSettings cs, string password, PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// if the server didn't support the hashed password; rehash with the new challenge
		var switchRequest = AuthenticationMethodSwitchRequestPayload.Create(payload.Span);
		m_logArguments[1] = switchRequest.Name;
		Log.Trace("Session{0} switching to AuthenticationMethod '{1}'", m_logArguments);
		switch (switchRequest.Name)
		{
		case "mysql_native_password":
			AuthPluginData = switchRequest.Data;
			var hashedPassword = AuthenticationUtility.CreateAuthenticationResponse(AuthPluginData, password);
			payload = new(hashedPassword);
			await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
			return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

		case "mysql_clear_password":
			if (!m_isSecureConnection)
			{
				Log.Error("Session{0} needs a secure connection to use AuthenticationMethod '{1}'", m_logArguments);
				throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Authentication method '{0}' requires a secure connection.".FormatInvariant(switchRequest.Name));
			}

			// send the password as a NULL-terminated UTF-8 string
			var passwordBytes = Encoding.UTF8.GetBytes(password);
			Array.Resize(ref passwordBytes, passwordBytes.Length + 1);
			payload = new(passwordBytes);
			await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
			return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

		case "caching_sha2_password":
			var scrambleBytes = AuthenticationUtility.CreateScrambleResponse(Utility.TrimZeroByte(switchRequest.Data.AsSpan()), password);
			payload = new(scrambleBytes);
			await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
			payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			// OK payload can be sent immediately (e.g., if password is empty( (short-circuiting the )
			if (OkPayload.IsOk(payload.Span, SupportsDeprecateEof))
				return payload;

			var cachingSha2ServerResponsePayload = CachingSha2ServerResponsePayload.Create(payload.Span);
			if (cachingSha2ServerResponsePayload.Succeeded)
				return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			goto case "sha256_password";

		case "sha256_password":
			if (!m_isSecureConnection && password.Length != 0)
			{
#if NET45
				Log.Error("Session{0} can't use AuthenticationMethod '{1}' without secure connection on .NET 4.5", m_logArguments);
				throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Authentication method '{0}' requires a secure connection (prior to .NET 4.6).".FormatInvariant(switchRequest.Name));
#else
				var publicKey = await GetRsaPublicKeyAsync(switchRequest.Name, cs, ioBehavior, cancellationToken).ConfigureAwait(false);
				return await SendEncryptedPasswordAsync(switchRequest, publicKey, password, ioBehavior, cancellationToken).ConfigureAwait(false);
#endif
			}
			else
			{
				return await SendClearPasswordAsync(password, ioBehavior, cancellationToken).ConfigureAwait(false);
			}

		case "auth_gssapi_client":
			return await AuthGSSAPI.AuthenticateAsync(cs, switchRequest.Data, this, ioBehavior, cancellationToken).ConfigureAwait(false);

		case "mysql_old_password":
			Log.Error("Session{0} is requesting AuthenticationMethod '{1}' which is not supported", m_logArguments);
			throw new NotSupportedException("'MySQL Server is requesting the insecure pre-4.1 auth mechanism (mysql_old_password). The user password must be upgraded; see https://dev.mysql.com/doc/refman/5.7/en/account-upgrades.html.");

		case "client_ed25519":
			if (!AuthenticationPlugins.TryGetPlugin(switchRequest.Name, out var ed25519Plugin))
				throw new NotSupportedException("You must install the MySqlConnector.Authentication.Ed25519 package and call Ed25519AuthenticationPlugin.Install to use client_ed25519 authentication.");
			payload = new(ed25519Plugin.CreateResponse(password, switchRequest.Data));
			await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
			return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

		default:
			Log.Error("Session{0} is requesting AuthenticationMethod '{1}' which is not supported", m_logArguments);
			throw new NotSupportedException("Authentication method '{0}' is not supported.".FormatInvariant(switchRequest.Name));
		}
	}

	private async Task<PayloadData> SendClearPasswordAsync(string password, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// add NUL terminator to password
		var passwordBytes = Encoding.UTF8.GetBytes(password);
		Array.Resize(ref passwordBytes, passwordBytes.Length + 1);

		// send plaintext password
		var payload = new PayloadData(passwordBytes);
		await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
		return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
	}

#if !NET45
	private async Task<PayloadData> SendEncryptedPasswordAsync(
		AuthenticationMethodSwitchRequestPayload switchRequest,
		string rsaPublicKey,
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
			Log.Error(ex, "Session{0} couldn't load server's RSA public key", m_logArguments);
			throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Couldn't load server's RSA public key; try using a secure connection instead.", ex);
		}
#else
		// load the RSA public key
		RSAParameters rsaParameters;
		try
		{
			rsaParameters = Utility.GetRsaParameters(rsaPublicKey);
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Session{0} couldn't load server's RSA public key", m_logArguments);
			throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Couldn't load server's RSA public key; try using a secure connection instead.", ex);
		}

		rsa.ImportParameters(rsaParameters);
#endif

		// add NUL terminator to password
		var passwordBytes = Encoding.UTF8.GetBytes(password);
		Array.Resize(ref passwordBytes, passwordBytes.Length + 1);

		// XOR the password bytes with the challenge
		AuthPluginData = Utility.TrimZeroByte(switchRequest.Data);
		for (var i = 0; i < passwordBytes.Length; i++)
			passwordBytes[i] ^= AuthPluginData[i % AuthPluginData.Length];

		// encrypt with RSA public key
		var padding = RSAEncryptionPadding.OaepSHA1;
		var encryptedPassword = rsa.Encrypt(passwordBytes, padding);
		var payload = new PayloadData(encryptedPassword);
		await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
		return await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
	}
#endif

#if !NET45
	private async Task<string> GetRsaPublicKeyAsync(string switchRequestName, ConnectionSettings cs, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (cs.ServerRsaPublicKeyFile.Length != 0)
		{
			try
			{
				return File.ReadAllText(cs.ServerRsaPublicKeyFile);
			}
			catch (IOException ex)
			{
				m_logArguments[1] = cs.ServerRsaPublicKeyFile;
				Log.Error(ex, "Session{0} couldn't load server's RSA public key from PublicKeyFile '{1}'", m_logArguments);
				throw new MySqlException("Couldn't load server's RSA public key from '{0}'".FormatInvariant(cs.ServerRsaPublicKeyFile), ex);
			}
		}

		if (cs.AllowPublicKeyRetrieval)
		{
			// request the RSA public key
			var payloadContent = switchRequestName == "caching_sha2_password" ? (byte) 0x02 : (byte) 0x01;
			await SendReplyAsync(new PayloadData(new[] { payloadContent }), ioBehavior, cancellationToken).ConfigureAwait(false);
			var payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			var publicKeyPayload = AuthenticationMoreDataPayload.Create(payload.Span);
			return Encoding.ASCII.GetString(publicKeyPayload.Data);
		}

		m_logArguments[1] = switchRequestName;
		Log.Error("Session{0} couldn't use AuthenticationMethod '{1}' because RSA key wasn't specified or couldn't be retrieved", m_logArguments);
		throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Authentication method '{0}' failed. Either use a secure connection, specify the server's RSA public key with ServerRSAPublicKeyFile, or set AllowPublicKeyRetrieval=True.".FormatInvariant(switchRequestName));
	}
#endif

	public async ValueTask<bool> TryPingAsync(bool logInfo, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		VerifyState(State.Connected);

		// send ping payload to verify client and server socket are still connected
		try
		{
			Log.Trace("Session{0} pinging server", m_logArguments);
			await SendAsync(PingPayload.Instance, ioBehavior, cancellationToken).ConfigureAwait(false);
			var payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			OkPayload.Create(payload.Span, SupportsDeprecateEof, SupportsSessionTrack);
			if (logInfo)
				Log.Info("Session{0} successfully pinged server", m_logArguments);
			else
				Log.Trace("Session{0} successfully pinged server", m_logArguments);
			return true;
		}
		catch (IOException ex)
		{
			Log.Trace(ex, "Session{0} ping failed due to IOException", m_logArguments);
		}
		catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.ClientInteractionTimeout)
		{
			Log.Trace(ex, "Session{0} ping failed due to ClientInteractionTimeout MySqlException", m_logArguments);
		}
		catch (SocketException ex)
		{
			Log.Trace(ex, "Session{0} ping failed due to SocketException", m_logArguments);
		}

		VerifyState(State.Failed);
		return false;
	}

	// Starts a new conversation with the server by sending the first packet.
	public ValueTask<int> SendAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
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
	public ValueTask<PayloadData> ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		ValueTask<ArraySegment<byte>> task;
		try
		{
			VerifyConnected();
			task = m_payloadHandler!.ReadPayloadAsync(m_payloadCache, ProtocolErrorBehavior.Throw, ioBehavior);
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Session{0} failed in ReceiveReplyAsync", m_logArguments);
			if ((ex as MySqlException)?.ErrorCode == MySqlErrorCode.CommandTimeoutExpired)
				HandleTimeout();
			task = ValueTaskExtensions.FromException<ArraySegment<byte>>(ex);
		}

		if (task.IsCompletedSuccessfully)
		{
			var payload = new PayloadData(task.Result);
			if (payload.HeaderByte != ErrorPayload.Signature)
				return new ValueTask<PayloadData>(payload);

			var exception = CreateExceptionForErrorPayload(payload.Span);
			return ValueTaskExtensions.FromException<PayloadData>(exception);
		}

		return ReceiveReplyAsyncAwaited(task);
	}

	private async ValueTask<PayloadData> ReceiveReplyAsyncAwaited(ValueTask<ArraySegment<byte>> task)
	{
		ArraySegment<byte> bytes;
		try
		{
			bytes = await task.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			SetFailed(ex);
			if (ex is MySqlException { ErrorCode: MySqlErrorCode.CommandTimeoutExpired })
				HandleTimeout();
			throw;
		}
		var payload = new PayloadData(bytes);
		if (payload.HeaderByte == ErrorPayload.Signature)
			throw CreateExceptionForErrorPayload(payload.Span);
		return payload;
	}

	// Continues a conversation with the server by sending a reply to a packet received with 'Receive' or 'ReceiveReply'.
	public ValueTask<int> SendReplyAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		ValueTask<int> task;
		try
		{
			VerifyConnected();
			task = m_payloadHandler!.WritePayloadAsync(payload.Memory, ioBehavior);
		}
		catch (Exception ex)
		{
			Log.Debug(ex, "Session{0} failed in SendReplyAsync", m_logArguments);
			task = ValueTaskExtensions.FromException<int>(ex);
		}

		if (task.IsCompletedSuccessfully)
			return task;

		return SendReplyAsyncAwaited(task);
	}

	private async ValueTask<int> SendReplyAsyncAwaited(ValueTask<int> task)
	{
		try
		{
			return await task.ConfigureAwait(false);
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
				throw new MySqlException(MySqlErrorCode.DelimiterNotSupported, "'DELIMITER' should not be used with MySqlConnector. See https://fl.vu/mysql-delimiter", exception);
		}
	}

	internal void HandleTimeout()
	{
		if (OwningConnection is not null && OwningConnection.TryGetTarget(out var connection))
			connection.SetState(ConnectionState.Closed);
	}

	private void VerifyConnected()
	{
		lock (m_lock)
		{
			if (m_state == State.Closed)
				throw new ObjectDisposedException(nameof(ServerSession));
			if (m_state != State.Connected && m_state != State.Querying && m_state != State.CancelingQuery && m_state != State.ClearingPendingCancellation && m_state != State.Closing)
				throw new InvalidOperationException("ServerSession is not connected.");
		}
	}

	private async Task<bool> OpenTcpSocketAsync(ConnectionSettings cs, ILoadBalancer loadBalancer, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		var hostNames = loadBalancer.LoadBalance(cs.HostNames!);
		foreach (var hostName in hostNames)
		{
			IPAddress[] ipAddresses;
			try
			{
				ipAddresses = ioBehavior == IOBehavior.Asynchronous
#if NET6_0_OR_GREATER
					? await Dns.GetHostAddressesAsync(hostName, cancellationToken).ConfigureAwait(false)
#else
					? await Dns.GetHostAddressesAsync(hostName).ConfigureAwait(false)
#endif
					: Dns.GetHostAddresses(hostName);
			}
			catch (SocketException)
			{
				// name couldn't be resolved
				continue;
			}

			// need to try IP Addresses one at a time: https://github.com/dotnet/corefx/issues/5829
			foreach (var ipAddress in ipAddresses)
			{
				Log.Trace("Session{0} connecting to IpAddress {1} for HostName '{2}'", m_logArguments[0], ipAddress, hostName);
				TcpClient? tcpClient = null;
				try
				{
					tcpClient = new(ipAddress.AddressFamily);

					using (cancellationToken.Register(() => tcpClient.Client?.Dispose()))
					{
						try
						{
							if (ioBehavior == IOBehavior.Asynchronous)
							{
								await tcpClient.ConnectAsync(ipAddress, cs.Port).ConfigureAwait(false);
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
						catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
						{
							SafeDispose(ref tcpClient);
							Log.Info("Session{0} connect timeout expired connecting to IpAddress {1} for HostName '{2}'", m_logArguments[0], ipAddress, hostName);
							throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Connect Timeout expired.");
						}
					}
				}
				catch (SocketException)
				{
					SafeDispose(ref tcpClient);
					continue;
				}

				if (!tcpClient.Connected && cancellationToken.IsCancellationRequested)
				{
					SafeDispose(ref tcpClient);
					Log.Info("Session{0} connect timeout expired connecting to IpAddress {1} for HostName '{2}'", m_logArguments[0], ipAddress, hostName);
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

					m_activityTags.Add(ActivitySourceHelper.NetTransportTagName, ActivitySourceHelper.NetTransportTcpIpValue);
					var ipAddressString = ipAddress.ToString();
					m_activityTags.Add(ActivitySourceHelper.NetPeerIpTagName, ipAddressString);
					if (ipAddressString != hostName)
						m_activityTags.Add(ActivitySourceHelper.NetPeerNameTagName, hostName);
					if (cs.Port != 3306)
						m_activityTags.Add(ActivitySourceHelper.NetPeerPortTagName, cs.Port.ToString(CultureInfo.InvariantCulture));
				}
				catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
				{
					Utility.Dispose(ref m_stream);
					SafeDispose(ref m_tcpClient);
					SafeDispose(ref m_socket);
					Log.Info("Session{0} connect timeout expired connecting to IpAddress {1} for HostName '{2}'", m_logArguments[0], ipAddress, hostName);
					throw new MySqlException(MySqlErrorCode.UnableToConnectToHost, "Connect Timeout expired.");
				}

				lock (m_lock)
					m_state = State.Connected;
				Log.Trace("Session{0} connected to IpAddress {1} for HostName '{2}' with local Port {3}", m_logArguments[0], ipAddress, hostName, (m_socket.LocalEndPoint as IPEndPoint)?.Port);
				return true;
			}
		}
		return false;
	}

	private async Task<bool> OpenUnixSocketAsync(ConnectionSettings cs, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		m_logArguments[1] = cs.UnixSocket;
		Log.Trace("Session{0} connecting to UNIX Socket '{1}'", m_logArguments);
		var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
		var unixEp = new UnixDomainSocketEndPoint(cs.UnixSocket!);
		try
		{
			using (cancellationToken.Register(() => socket.Dispose()))
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
					Log.Info("Session{0} connect timeout expired connecting to UNIX Socket '{1}'", m_logArguments);
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

			m_activityTags.Add(ActivitySourceHelper.NetTransportTagName, ActivitySourceHelper.NetTransportUnixValue);
			m_activityTags.Add(ActivitySourceHelper.NetPeerNameTagName, cs.UnixSocket);

			lock (m_lock)
				m_state = State.Connected;
			return true;
		}

		return false;
	}

#if NET45
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
#endif
	private async Task<bool> OpenNamedPipeAsync(ConnectionSettings cs, int startTickCount, IOBehavior ioBehavior, CancellationToken cancellationToken)
#if NET45
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
#endif
	{
		if (Log.IsTraceEnabled())
			Log.Trace("Session{0} connecting to NamedPipe '{1}' on Server '{2}'", m_logArguments[0], cs.PipeName, cs.HostNames![0]);
		var namedPipeStream = new NamedPipeClientStream(cs.HostNames![0], cs.PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
		var timeout = Math.Max(1, cs.ConnectionTimeoutMilliseconds - unchecked(Environment.TickCount - startTickCount));
		try
		{
			using (cancellationToken.Register(() => namedPipeStream.Dispose()))
			{
				try
				{
#if !NET45
					if (ioBehavior == IOBehavior.Asynchronous)
						await namedPipeStream.ConnectAsync(timeout, cancellationToken).ConfigureAwait(false);
					else
#endif
					namedPipeStream.Connect(timeout);
				}
				catch (Exception ex) when ((ex is ObjectDisposedException && cancellationToken.IsCancellationRequested) || ex is TimeoutException)
				{
					m_logArguments[1] = cs.PipeName;
					Log.Info("Session{0} connect timeout expired connecting to named pipe '{1}'", m_logArguments);
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

			// see https://docs.microsoft.com/en-us/windows/win32/ipc/pipe-names for pipe name format
			m_activityTags.Add(ActivitySourceHelper.NetTransportTagName, ActivitySourceHelper.NetTransportNamedPipeValue);
			m_activityTags.Add(ActivitySourceHelper.NetPeerNameTagName, @"\\" + cs.HostNames![0] + @"\pipe\" + cs.PipeName);

			lock (m_lock)
				m_state = State.Connected;
			return true;
		}

		return false;
	}

	private async Task InitSslAsync(ProtocolCapabilities serverCapabilities, ConnectionSettings cs, MySqlConnection connection, SslProtocols sslProtocols, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Log.Trace("Session{0} initializing TLS connection", m_logArguments);
		X509CertificateCollection? clientCertificates = null;

		if (cs.CertificateStoreLocation != MySqlCertificateStoreLocation.None)
		{
			try
			{
				var storeLocation = (cs.CertificateStoreLocation == MySqlCertificateStoreLocation.CurrentUser) ? StoreLocation.CurrentUser : StoreLocation.LocalMachine;
				var store = new X509Store(StoreName.My, storeLocation);
				store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

				if (cs.CertificateThumbprint.Length == 0)
				{
					if (store.Certificates.Count == 0)
					{
						Log.Error("Session{0} no certificates were found in the certificate store", m_logArguments);
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
						m_logArguments[1] = cs.CertificateThumbprint;
						Log.Error("Session{0} certificate with Thumbprint={1} not found in store", m_logArguments);
						throw new MySqlException("Certificate with Thumbprint {0} not found".FormatInvariant(cs.CertificateThumbprint));
					}

					clientCertificates = new(foundCertificates);
				}
			}
			catch (CryptographicException ex)
			{
				m_logArguments[1] = cs.CertificateStoreLocation;
				Log.Error(ex, "Session{0} couldn't load certificate from CertificateStoreLocation={1}", m_logArguments);
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
				var certificate = new X509Certificate2(cs.CertificateFile, cs.CertificatePassword, X509KeyStorageFlags.MachineKeySet);
				if (!certificate.HasPrivateKey)
				{
#if NET45
					certificate.Reset();
#else
					certificate.Dispose();
#endif
					m_logArguments[1] = cs.CertificateFile;
					Log.Error("Session{0} no private key included with CertificateFile '{1}'", m_logArguments);
					throw new MySqlException("CertificateFile does not contain a private key. " +
						"CertificateFile should be in PKCS #12 (.pfx) format and contain both a Certificate and Private Key");
				}
				m_clientCertificate = certificate;
				clientCertificates = new() { certificate };
			}
			catch (CryptographicException ex)
			{
				m_logArguments[1] = cs.CertificateFile;
				Log.Error(ex, "Session{0} couldn't load certificate from CertificateFile '{1}'", m_logArguments);
				if (!File.Exists(cs.CertificateFile))
					throw new MySqlException("Cannot find Certificate File", ex);
				throw new MySqlException("Either the Certificate Password is incorrect or the Certificate File is invalid", ex);
			}
		}

		if (clientCertificates is null && connection.ProvideClientCertificatesCallback is { } clientCertificatesProvider)
		{
			clientCertificates = new();
			try
			{
				await clientCertificatesProvider(clientCertificates).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				m_logArguments[1] = ex.Message;
				Log.Error(ex, "Session{0} failed to obtain client certificates via ProvideClientCertificatesCallback: {1}", m_logArguments);
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
				m_logArguments[1] = cs.CACertificateFile;
				Log.Trace("Session{0} loading CA certificate(s) from CertificateFile '{1}'", m_logArguments);
				byte[] certificateBytes;
				try
				{
					certificateBytes = File.ReadAllBytes(cs.CACertificateFile);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Session{0} couldn't load CA certificate from CertificateFile '{1}'", m_logArguments);
					if (!File.Exists(cs.CACertificateFile))
						throw new MySqlException("Cannot find CA Certificate File: " + cs.CACertificateFile, ex);
					throw new MySqlException("Could not load CA Certificate File: " + cs.CACertificateFile, ex);
				}

				// find the index of each individual certificate in the file (assuming there may be multiple certificates concatenated together)
				for (var index = 0; index != -1;)
				{
					var nextIndex = Utility.FindNextIndex(certificateBytes, index + 1, BeginCertificateBytes);
					try
					{
						// load the certificate at this index; note that 'new X509Certificate' stops at the end of the first certificate it loads
						m_logArguments[1] = index;
						Log.Trace("Session{0} loading certificate at Index {1} in the CA certificate file.", m_logArguments);
						var caCertificate = new X509Certificate2(Utility.ArraySlice(certificateBytes, index, (nextIndex == -1 ? certificateBytes.Length : nextIndex) - index), default(string), X509KeyStorageFlags.MachineKeySet);
						certificateChain.ChainPolicy.ExtraStore.Add(caCertificate);
					}
					catch (CryptographicException ex)
					{
						m_logArguments[1] = cs.CACertificateFile;
						Log.Warn(ex, "Session{0} couldn't load CA certificate from CertificateFile '{1}'", m_logArguments);
					}
					index = nextIndex;
				}

				// success
				if (Log.IsTraceEnabled())
					Log.Trace("Session{0} loaded certificates from CertificateFile '{1}'; CertificateCount: {2}", m_logArguments[0], cs.CACertificateFile, certificateChain.ChainPolicy.ExtraStore.Count);
				caCertificateChain = certificateChain;
				certificateChain = null;
			}
			finally
			{
#if NET45
				certificateChain?.Reset();
#else
				certificateChain?.Dispose();
#endif
			}
		}

		X509Certificate ValidateLocalCertificate(object lcbSender, string lcbTargetHost, X509CertificateCollection lcbLocalCertificates, X509Certificate? lcbRemoteCertificate, string[] lcbAcceptableIssuers) => lcbLocalCertificates[0];

		bool ValidateRemoteCertificate(object rcbSender, X509Certificate? rcbCertificate, X509Chain? rcbChain, SslPolicyErrors rcbPolicyErrors)
		{
			if (cs.SslMode is MySqlSslMode.Preferred or MySqlSslMode.Required)
				return true;

			if ((rcbPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 &&
				rcbCertificate is not null &&
				caCertificateChain is not null &&
				caCertificateChain.Build((X509Certificate2) rcbCertificate) &&
				caCertificateChain.ChainStatus.Length > 0)
			{
				var chainStatus = caCertificateChain.ChainStatus[0].Status & ~X509ChainStatusFlags.UntrustedRoot;
				if (chainStatus == X509ChainStatusFlags.NoError)
					rcbPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
			}

			if (cs.SslMode == MySqlSslMode.VerifyCA)
				rcbPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;

			return rcbPolicyErrors == SslPolicyErrors.None;
		}

		// use the client's callback (if any) for Preferred or Required mode
		RemoteCertificateValidationCallback validateRemoteCertificate = ValidateRemoteCertificate;
		if (connection.RemoteCertificateValidationCallback is not null)
		{
			if (caCertificateChain is not null)
			{
				Log.Warn("Session{0} not using client-provided RemoteCertificateValidationCallback because SslCA is specified", m_logArguments);
			}
			else if (cs.SslMode is not MySqlSslMode.Preferred and not MySqlSslMode.Required)
			{
				m_logArguments[1] = cs.SslMode;
				Log.Warn("Session{0} not using client-provided RemoteCertificateValidationCallback because SslMode is {1}", m_logArguments);
			}
			else
			{
				Log.Debug("Session{0} using client-provided RemoteCertificateValidationCallback", m_logArguments);
				validateRemoteCertificate = connection.RemoteCertificateValidationCallback;
			}
		}

		var sslStream = clientCertificates is null ? new SslStream(m_stream!, false, validateRemoteCertificate) :
			new SslStream(m_stream!, false, validateRemoteCertificate, ValidateLocalCertificate);

		var checkCertificateRevocation = cs.SslMode == MySqlSslMode.VerifyFull;

		using (var initSsl = HandshakeResponse41Payload.CreateWithSsl(serverCapabilities, cs, m_useCompression, m_characterSet))
			await SendReplyAsync(initSsl, ioBehavior, cancellationToken).ConfigureAwait(false);

		var clientAuthenticationOptions = new SslClientAuthenticationOptions
		{
			EnabledSslProtocols = sslProtocols,
			ClientCertificates = clientCertificates,
			TargetHost = HostName,
			CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
		};

#if NETCOREAPP3_0_OR_GREATER
		if (cs.TlsCipherSuites is { Count: > 0 })
			clientAuthenticationOptions.CipherSuitesPolicy = new CipherSuitesPolicy(cs.TlsCipherSuites);
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
#elif NET45_OR_GREATER || NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_0_OR_GREATER
				sslStream.AuthenticateAsClient(clientAuthenticationOptions.TargetHost,
					clientAuthenticationOptions.ClientCertificates,
					clientAuthenticationOptions.EnabledSslProtocols,
					checkCertificateRevocation);
#else
				await sslStream.AuthenticateAsClientAsync(HostName, clientCertificates, sslProtocols, checkCertificateRevocation).ConfigureAwait(false);
#endif
			}
			var sslByteHandler = new StreamByteHandler(sslStream);
			m_payloadHandler!.ByteHandler = sslByteHandler;
			m_isSecureConnection = true;
			m_sslStream = sslStream;
			if (Log.IsDebugEnabled())
			{
#if NETCOREAPP3_0_OR_GREATER
				Log.Debug("Session{0} connected TLS with SslProtocol={1}, NegotiatedCipherSuite={2}", m_logArguments[0], sslStream.SslProtocol, sslStream.NegotiatedCipherSuite);
#else
				Log.Debug("Session{0} connected TLS with SslProtocol={1}, CipherAlgorithm={2}, HashAlgorithm={3}, KeyExchangeAlgorithm={4}, KeyExchangeStrength={5}", m_logArguments[0], sslStream.SslProtocol, sslStream.CipherAlgorithm, sslStream.HashAlgorithm, sslStream.KeyExchangeAlgorithm, sslStream.KeyExchangeStrength);
#endif
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Session{0} couldn't initialize TLS connection", m_logArguments);
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
#if NET45
			caCertificateChain?.Reset();
#else
			caCertificateChain?.Dispose();
#endif
		}

		// Returns a X509CertificateCollection containing the single certificate contained in 'sslKeyFile' (PEM private key) and 'sslCertificateFile' (PEM certificate).
		X509CertificateCollection LoadCertificate(string sslKeyFile, string sslCertificateFile)
		{
#if NETSTANDARD2_0
			throw new NotSupportedException("SslCert and SslKey connection string options are not supported in netstandard2.0.");
#elif NET5_0_OR_GREATER
			m_clientCertificate = X509Certificate2.CreateFromPemFile(sslCertificateFile, sslKeyFile);
			return new() { m_clientCertificate };
#else
			m_logArguments[1] = sslKeyFile;
			Log.Trace("Session{0} loading client key from KeyFile '{1}'", m_logArguments);
			string keyPem;
			try
			{
				keyPem = File.ReadAllText(sslKeyFile);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Session{0} couldn't load client key from KeyFile '{1}'", m_logArguments);
				throw new MySqlException("Could not load client key file: " + sslKeyFile, ex);
			}

			RSAParameters rsaParameters;
			try
			{
				rsaParameters = Utility.GetRsaParameters(keyPem);
			}
			catch (FormatException ex)
			{
				Log.Error(ex, "Session{0} couldn't load client key from KeyFile '{1}'", m_logArguments);
				throw new MySqlException("Could not load the client key from " + sslKeyFile, ex);
			}

			try
			{
				RSA rsa;
				try
				{
#pragma warning disable CA1416
					// SslStream on Windows needs a KeyContainerName to be set
					var csp = new CspParameters
					{
						KeyContainerName = Guid.NewGuid().ToString(),
					};
					rsa = new RSACryptoServiceProvider(csp)
					{
						PersistKeyInCsp = true,
					};
#pragma warning restore
				}
				catch (PlatformNotSupportedException)
				{
					rsa = RSA.Create();
				}
				rsa.ImportParameters(rsaParameters);

#if NET45 || NET461 || NET471
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
				return new() { certificate };
			}
			catch (CryptographicException ex)
			{
				Log.Error(ex, "Session{0} couldn't load client key from KeyFile '{1}'", m_logArguments);
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
		if (cs.ConnectionProtocol == MySqlConnectionProtocol.Sockets && cs.UserID.IndexOf('@') != -1)
		{
			return HostName.EndsWith(".mysql.database.azure.com", StringComparison.OrdinalIgnoreCase) ||
				HostName.EndsWith(".database.windows.net", StringComparison.OrdinalIgnoreCase) ||
				HostName.EndsWith(".mysql.database.chinacloudapi.cn", StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}

	private async Task GetRealServerDetailsAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Log.Debug("Session{0} detected proxy; getting CONNECTION_ID(), VERSION() from server", m_logArguments);
		try
		{
			var payload = SupportsQueryAttributes ? s_selectConnectionIdVersionWithAttributesPayload : s_selectConnectionIdVersionNoAttributesPayload;
			await SendAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);

			// column count: 2
			await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			// CONNECTION_ID() and VERSION() columns
			await ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
			await ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);

			if (!SupportsDeprecateEof)
			{
				payload = await ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
				EofPayload.Create(payload.Span);
			}

			// first (and only) row
			payload = await ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
			static void ReadRow(ReadOnlySpan<byte> span, out int? connectionId, out byte[]? serverVersion)
			{
				var reader = new ByteArrayReader(span);
				var length = reader.ReadLengthEncodedIntegerOrNull();
				connectionId = (length != -1 && Utf8Parser.TryParse(reader.ReadByteString(length), out int id, out _)) ? id : default(int?);
				length = reader.ReadLengthEncodedIntegerOrNull();
				serverVersion = length != -1 ? reader.ReadByteString(length).ToArray() : null;
			}
			ReadRow(payload.Span, out var connectionId, out var serverVersion);

			// OK/EOF payload
			payload = await ReceiveReplyAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false);
			if (OkPayload.IsOk(payload.Span, SupportsDeprecateEof))
				OkPayload.Create(payload.Span, SupportsDeprecateEof, SupportsSessionTrack);
			else
				EofPayload.Create(payload.Span);

			if (connectionId.HasValue && serverVersion is not null)
			{
				var newServerVersion = new ServerVersion(serverVersion);
				Log.Debug("Session{0} changing ConnectionIdOld {1} to ConnectionId {2} and ServerVersionOld {3} to ServerVersion {4}", m_logArguments[0], ConnectionId, connectionId.Value, ServerVersion.OriginalString, newServerVersion.OriginalString);
				ConnectionId = connectionId.Value;
				ServerVersion = newServerVersion;
			}
		}
		catch (MySqlException ex)
		{
			Log.Info(ex, "Session{0} failed to get CONNECTION_ID(), VERSION()", m_logArguments);
		}
	}

	private void ShutdownSocket()
	{
		Log.Debug("Session{0} closing stream/socket", m_logArguments);
		Utility.Dispose(ref m_payloadHandler);
		Utility.Dispose(ref m_stream);
		SafeDispose(ref m_tcpClient);
		SafeDispose(ref m_socket);
#if NET45
		m_clientCertificate?.Reset();
		m_clientCertificate = null;
#else
		Utility.Dispose(ref m_clientCertificate);
#endif
		m_activityTags.Clear();
		m_activityTags.Add(ActivitySourceHelper.DatabaseSystemTagName, ActivitySourceHelper.DatabaseSystemValue);
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
		Log.Debug(exception, "Session{0} setting state to Failed", m_logArguments);
		lock (m_lock)
			m_state = State.Failed;
		if (OwningConnection is not null && OwningConnection.TryGetTarget(out var connection))
			connection.SetState(ConnectionState.Closed);
	}

	private void VerifyState(State state)
	{
		if (m_state != state)
		{
			Log.Error("Session{0} should have SessionStateExpected {1} but was SessionState {2}", m_logArguments[0], state, m_state);
			throw new InvalidOperationException("Expected state to be {0} but was {1}.".FormatInvariant(state, m_state));
		}
	}

	private void VerifyState(State state1, State state2, State state3, State state4, State state5, State state6)
	{
		if (m_state != state1 && m_state != state2 && m_state != state3 && m_state != state4 && m_state != state5 && m_state != state6)
		{
			Log.Error("Session{0} should have SessionStateExpected {1} or SessionStateExpected2 {2} or SessionStateExpected3 {3} or SessionStateExpected4 {4} or SessionStateExpected5 {5} or SessionStateExpected6 {6} but was SessionState {7}",
				m_logArguments[0], state1, state2, state3, state4, state5, state6, m_state);
			throw new InvalidOperationException("Expected state to be ({0}|{1}|{2}|{3}|{4}|{5}) but was {6}.".FormatInvariant(state1, state2, state3, state4, state5, state6, m_state));
		}
	}

	internal bool SslIsEncrypted => m_sslStream?.IsEncrypted ?? false;

	internal bool SslIsSigned => m_sslStream?.IsSigned ?? false;

	internal bool SslIsAuthenticated => m_sslStream?.IsAuthenticated ?? false;

	internal bool SslIsMutuallyAuthenticated => m_sslStream?.IsMutuallyAuthenticated ?? false;

	internal SslProtocols SslProtocol => m_sslStream?.SslProtocol ?? SslProtocols.None;

	private byte[] CreateConnectionAttributes(string programName)
	{
		Log.Trace("Session{0} creating connection attributes", m_logArguments);
		var attributesWriter = new ByteBufferWriter();
		attributesWriter.WriteLengthEncodedString("_client_name");
		attributesWriter.WriteLengthEncodedString("MySqlConnector");
		attributesWriter.WriteLengthEncodedString("_client_version");

		var version = typeof(ServerSession).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
		var plusIndex = version.IndexOf('+');
		if (plusIndex != -1)
			version = version.Substring(0, plusIndex);
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
		if (programName.Length != 0)
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

	private Exception CreateExceptionForErrorPayload(ReadOnlySpan<byte> span)
	{
		var errorPayload = ErrorPayload.Create(span);
		if (Log.IsDebugEnabled())
			Log.Debug("Session{0} got error payload: Code={1}, State={2}, Message={3}", m_logArguments[0], errorPayload.ErrorCode, errorPayload.State, errorPayload.Message);
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
				Log.Trace("Session{0} obtaining password via ProvidePasswordCallback", m_logArguments);
				return passwordProvider(new(HostName, cs.Port, cs.UserID, cs.Database));
			}
			catch (Exception ex)
			{
				m_logArguments[1] = ex.Message;
				Log.Error(ex, "Session{0} failed to obtain password via ProvidePasswordCallback: {1}", m_logArguments);
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

	private sealed class DelimiterSqlParser : SqlParser
	{
		public DelimiterSqlParser(IMySqlCommand command)
			: base(new StatementPreparer(command.CommandText!, null, command.CreateStatementPreparerOptions()))
		{
			m_sql = command.CommandText!;
		}

		public bool HasDelimiter { get; private set; }

		protected override void OnStatementBegin(int index)
		{
			if (index + 10 < m_sql.Length && string.Equals("delimiter ", m_sql.Substring(index, 10), StringComparison.OrdinalIgnoreCase))
				HasDelimiter = true;
		}

		private readonly string m_sql;
	}

	private static ReadOnlySpan<byte> BeginCertificateBytes => new byte[] { 45, 45, 45, 45, 45, 66, 69, 71, 73, 78, 32, 67, 69, 82, 84, 73, 70, 73, 67, 65, 84, 69, 45, 45, 45, 45, 45 }; // -----BEGIN CERTIFICATE-----
	private static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(ServerSession));
	private static readonly PayloadData s_setNamesUtf8NoAttributesPayload = QueryPayload.Create(false, "SET NAMES utf8;");
	private static readonly PayloadData s_setNamesUtf8mb4NoAttributesPayload = QueryPayload.Create(false, "SET NAMES utf8mb4;");
	private static readonly PayloadData s_setNamesUtf8WithAttributesPayload = QueryPayload.Create(true, "SET NAMES utf8;");
	private static readonly PayloadData s_setNamesUtf8mb4WithAttributesPayload = QueryPayload.Create(true, "SET NAMES utf8mb4;");
	private static readonly PayloadData s_sleepNoAttributesPayload = QueryPayload.Create(false, "SELECT SLEEP(0) INTO @\uE001MySqlConnector\uE001Sleep;");
	private static readonly PayloadData s_sleepWithAttributesPayload = QueryPayload.Create(true, "SELECT SLEEP(0) INTO @\uE001MySqlConnector\uE001Sleep;");
	private static readonly PayloadData s_selectConnectionIdVersionNoAttributesPayload = QueryPayload.Create(false, "SELECT CONNECTION_ID(), VERSION();");
	private static readonly PayloadData s_selectConnectionIdVersionWithAttributesPayload = QueryPayload.Create(true, "SELECT CONNECTION_ID(), VERSION();");
	private static int s_lastId;

	private readonly object m_lock;
	private readonly object?[] m_logArguments;
	private readonly ArraySegmentHolder<byte> m_payloadCache;
	private readonly ActivityTagsCollection m_activityTags;
	private State m_state;
	private TcpClient? m_tcpClient;
	private Socket? m_socket;
	private Stream? m_stream;
	private SslStream? m_sslStream;
	private X509Certificate2? m_clientCertificate;
	private IPayloadHandler? m_payloadHandler;
	private bool m_useCompression;
	private bool m_isSecureConnection;
	private bool m_supportsComMulti;
	private bool m_supportsConnectionAttributes;
	private bool m_supportsDeprecateEof;
	private bool m_supportsSessionTrack;
	private bool m_supportsPipelining;
	private CharacterSet m_characterSet;
	private PayloadData m_setNamesPayload;
	private byte[]? m_pipelinedResetConnectionBytes;
	private Dictionary<string, PreparedStatements>? m_preparedStatements;
}
