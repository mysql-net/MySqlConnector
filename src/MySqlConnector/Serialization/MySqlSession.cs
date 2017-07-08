using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySql.Data.Protocol.Serialization;

namespace MySql.Data.Serialization
{
	internal sealed class MySqlSession
	{
		public MySqlSession()
			: this(null, 0, 0)
		{
		}

		public MySqlSession(ConnectionPool pool, int poolGeneration, int id)
		{
			m_lock = new object();
			m_payloadCache = new ArraySegmentHolder<byte>();
			Id = id;
			CreatedUtc = DateTime.UtcNow;
			Pool = pool;
			PoolGeneration = poolGeneration;
		}

		public int Id { get; }
		public ServerVersion ServerVersion { get; set; }
		public int ConnectionId { get; set; }
		public byte[] AuthPluginData { get; set; }
		public DateTime CreatedUtc { get; }
		public ConnectionPool Pool { get; }
		public int PoolGeneration { get; }
		public DateTime LastReturnedUtc { get; private set; }
		public string DatabaseOverride { get; set; }
		public IPAddress IPAddress => (m_tcpClient?.Client.RemoteEndPoint as IPEndPoint)?.Address;
		public WeakReference<MySqlConnection> OwningConnection { get; set; }

		public void ReturnToPool()
		{
			LastReturnedUtc = DateTime.UtcNow;
			Pool?.Return(this);
		}

		public bool IsConnected
		{
			get
			{
				lock (m_lock)
					return m_state == State.Connected;
			}
		}

		public bool TryStartCancel(MySqlCommand command)
		{
			lock (m_lock)
			{
				if (m_activeCommand != command)
					return false;
				VerifyState(State.Querying, State.CancelingQuery);
				if (m_state != State.Querying)
					return false;
				m_state = State.CancelingQuery;
			}

			return true;
		}

		public void DoCancel(MySqlCommand commandToCancel, MySqlCommand killCommand)
		{
			lock (m_lock)
			{
				if (m_activeCommand != commandToCancel)
					return;

				// NOTE: This command is executed while holding the lock to prevent race conditions during asynchronous cancellation.
				// For example, if the lock weren't held, the current command could finish and the other thread could set m_activeCommand
				// to null, then start executing a new command. By the time this "KILL QUERY" command reached the server, the wrong
				// command would be killed (because "KILL QUERY" specifies the connection whose command should be killed, not
				// a unique identifier of the command itself). As a mitigation, we set the CommandTimeout to a low value to avoid
				// blocking the other thread for an extended duration.
				killCommand.CommandTimeout = 3;
				killCommand.ExecuteNonQuery();
			}
		}

		public void AbortCancel(MySqlCommand command)
		{
			lock (m_lock)
			{
				if (m_activeCommand == command && m_state == State.CancelingQuery)
					m_state = State.Querying;
			}
		}

		public void StartQuerying(MySqlCommand command)
		{
			lock (m_lock)
			{
				if (m_state == State.Querying || m_state == State.CancelingQuery)
					throw new MySqlException("There is already an open DataReader associated with this Connection which must be closed first.");

				VerifyState(State.Connected);
				m_state = State.Querying;
				m_activeCommand = command;
			}
		}

		public MySqlDataReader ActiveReader => m_activeReader;

		public void SetActiveReader(MySqlDataReader dataReader)
		{
			VerifyState(State.Querying, State.CancelingQuery);
			if (dataReader == null)
				throw new ArgumentNullException(nameof(dataReader));
			if (m_activeReader != null)
				throw new InvalidOperationException("Can't replace active reader.");
			m_activeReader = dataReader;
		}

		public void FinishQuerying()
		{
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
				var payload = new PayloadData(new ArraySegment<byte>(PayloadUtilities.CreateEofStringPayload(CommandKind.Query, "DO SLEEP(0);")));
				SendAsync(payload, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
				payload = ReceiveReplyAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
				OkPayload.Create(payload);
			}

			lock (m_lock)
			{
				VerifyState(State.Querying, State.ClearingPendingCancellation);
				m_state = State.Connected;
				m_activeReader = null;
				m_activeCommand = null;
			}
		}

		public async Task DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (m_payloadHandler != null)
			{
				// attempt to gracefully close the connection, ignoring any errors (it may have been closed already by the server, etc.)
				lock (m_lock)
				{
					VerifyState(State.Connected, State.Failed);
					m_state = State.Closing;
				}
				try
				{
					m_payloadHandler.StartNewConversation();
					await m_payloadHandler.WritePayloadAsync(QuitPayload.Create(), ioBehavior).ConfigureAwait(false);
					await m_payloadHandler.ReadPayloadAsync(m_payloadCache, ProtocolErrorBehavior.Ignore, ioBehavior).ConfigureAwait(false);
				}
				catch (IOException)
				{
				}
				catch (SocketException)
				{
				}
			}
			ShutdownSocket();
			lock (m_lock)
				m_state = State.Closed;
		}

		public async Task ConnectAsync(ConnectionSettings cs, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			lock (m_lock)
			{
				VerifyState(State.Created);
				m_state = State.Connecting;
			}
			var connected = false;
			if (cs.ConnectionType == ConnectionType.Tcp)
				connected = await OpenTcpSocketAsync(cs, ioBehavior, cancellationToken).ConfigureAwait(false);
			else if (cs.ConnectionType == ConnectionType.Unix)
				connected = await OpenUnixSocketAsync(cs, ioBehavior, cancellationToken).ConfigureAwait(false);
			if (!connected)
			{
				lock (m_lock)
					m_state = State.Failed;
				throw new MySqlException("Unable to connect to any of the specified MySQL hosts.");
			}

			var socketByteHandler = new SocketByteHandler(m_socket);
			m_payloadHandler = new StandardPayloadHandler(socketByteHandler);

			var payload = await ReceiveAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			var reader = new ByteArrayReader(payload.ArraySegment.Array, payload.ArraySegment.Offset, payload.ArraySegment.Count);
			var initialHandshake = new InitialHandshakePacket(reader);

			// if PluginAuth is supported, then use the specified auth plugin; else, fall back to protocol capabilities to determine the auth type to use
			string authPluginName;
			if ((initialHandshake.ProtocolCapabilities & ProtocolCapabilities.PluginAuth) != 0)
				authPluginName = initialHandshake.AuthPluginName;
			else
				authPluginName = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.SecureConnection) == 0 ? "mysql_old_password" : "mysql_native_password";
			if (authPluginName != "mysql_native_password" && authPluginName != "sha256_password")
				throw new NotSupportedException("Authentication method '{0}' is not supported.".FormatInvariant(initialHandshake.AuthPluginName));

			ServerVersion = new ServerVersion(Encoding.ASCII.GetString(initialHandshake.ServerVersion));
			ConnectionId = initialHandshake.ConnectionId;
			AuthPluginData = initialHandshake.AuthPluginData;
			m_useCompression = cs.UseCompression && (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.Compress) != 0;

			var serverSupportsSsl = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.Ssl) != 0;
			if (cs.SslMode != MySqlSslMode.None && (cs.SslMode != MySqlSslMode.Preferred || serverSupportsSsl))
			{
				if (!serverSupportsSsl)
					throw new MySqlException("Server does not support SSL");
				await InitSslAsync(initialHandshake.ProtocolCapabilities, cs, ioBehavior, cancellationToken).ConfigureAwait(false);
			}

			m_supportsConnectionAttributes = (initialHandshake.ProtocolCapabilities & ProtocolCapabilities.ConnectionAttributes) != 0;
			if (m_supportsConnectionAttributes && s_connectionAttributes == null)
				s_connectionAttributes = CreateConnectionAttributes();

			var response = HandshakeResponse41Packet.Create(initialHandshake, cs, m_useCompression, m_supportsConnectionAttributes ? s_connectionAttributes : null);
			payload = new PayloadData(new ArraySegment<byte>(response));
			await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
			payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			// if server doesn't support the authentication fast path, it will send a new challenge
			if (payload.HeaderByte == AuthenticationMethodSwitchRequestPayload.Signature)
			{
				await SwitchAuthenticationAsync(payload, cs.Password, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			}

			OkPayload.Create(payload);

			if (m_useCompression)
				m_payloadHandler = new CompressedPayloadHandler(m_payloadHandler.ByteHandler);
		}

		public async Task ResetConnectionAsync(ConnectionSettings cs, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyState(State.Connected);
			if (ServerVersion.Version.CompareTo(ServerVersions.SupportsResetConnection) >= 0)
			{
				await SendAsync(ResetConnectionPayload.Create(), ioBehavior, cancellationToken).ConfigureAwait(false);
				var payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				OkPayload.Create(payload);

				// the "reset connection" packet also resets the connection charset, so we need to change that back to our default
				payload = new PayloadData(new ArraySegment<byte>(PayloadUtilities.CreateEofStringPayload(CommandKind.Query, "SET NAMES utf8mb4 COLLATE utf8mb4_bin;")));
				await SendAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				OkPayload.Create(payload);
			}
			else
			{
				// optimistically hash the password with the challenge from the initial handshake (supported by MariaDB; doesn't appear to be supported by MySQL)
				var hashedPassword = AuthenticationUtility.CreateAuthenticationResponse(AuthPluginData, 0, cs.Password);
				var payload = ChangeUserPayload.Create(cs.UserID, hashedPassword, cs.Database, m_supportsConnectionAttributes ? s_connectionAttributes : null);
				await SendAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				if (payload.HeaderByte == AuthenticationMethodSwitchRequestPayload.Signature)
				{
					await SwitchAuthenticationAsync(payload, cs.Password, ioBehavior, cancellationToken);
					payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				OkPayload.Create(payload);
			}
		}

		private async Task SwitchAuthenticationAsync(PayloadData payload, string password, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			// if the server didn't support the hashed password; rehash with the new challenge
			var switchRequest = AuthenticationMethodSwitchRequestPayload.Create(payload);
			switch (switchRequest.Name)
			{
			case "mysql_native_password":
				AuthPluginData = switchRequest.Data;
				var hashedPassword = AuthenticationUtility.CreateAuthenticationResponse(AuthPluginData, 0, password);
				payload = new PayloadData(new ArraySegment<byte>(hashedPassword));
				await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				break;

			case "mysql_clear_password":
				if (!m_isSecureConnection)
					throw new MySqlException("Authentication method '{0}' requires a secure connection.".FormatInvariant(switchRequest.Name));
				payload = new PayloadData(new ArraySegment<byte>(Encoding.UTF8.GetBytes(password)));
				await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				break;

			case "sha256_password":
				// add NUL terminator to password
				var passwordBytes = Encoding.UTF8.GetBytes(password);
				Array.Resize(ref passwordBytes, passwordBytes.Length + 1);

				if (!m_isSecureConnection && passwordBytes.Length > 1)
				{
#if NET451
					throw new MySqlException("Authentication method '{0}' requires a secure connection (prior to .NET 4.6).".FormatInvariant(switchRequest.Name));
#else
					// request the RSA public key
					await SendReplyAsync(new PayloadData(new ArraySegment<byte>(new byte[] { 0x01 }, 0, 1)), ioBehavior, cancellationToken).ConfigureAwait(false);
					payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					var publicKeyPayload = AuthenticationMoreDataPayload.Create(payload);
					var publicKey = Encoding.ASCII.GetString(publicKeyPayload.Data);

					// load the RSA public key
					RSA rsa;
					try
					{
						rsa = Utility.DecodeX509PublicKey(publicKey);
					}
					catch (Exception ex)
					{
						throw new MySqlException("Couldn't load server's RSA public key; try using a secure connection instead.", ex);
					}

					using (rsa)
					{
						// XOR the password bytes with the challenge
						AuthPluginData = Utility.TrimZeroByte(switchRequest.Data);
						for (int i = 0; i < passwordBytes.Length; i++)
							passwordBytes[i] ^= AuthPluginData[i % AuthPluginData.Length];

						// encrypt with RSA public key
						var encryptedPassword = rsa.Encrypt(passwordBytes, RSAEncryptionPadding.OaepSHA1);
						payload = new PayloadData(new ArraySegment<byte>(encryptedPassword));
						await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
					}
#endif
				}
				else
				{
					// send plaintext password
					payload = new PayloadData(new ArraySegment<byte>(passwordBytes));
					await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				break;

			default:
				throw new NotSupportedException("Authentication method '{0}' is not supported.".FormatInvariant(switchRequest.Name));
			}
		}

		public async Task<bool> TryPingAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyState(State.Connected);

			// send ping payload to verify client and server socket are still connected
			try
			{
				await SendAsync(PingPayload.Create(), ioBehavior, cancellationToken).ConfigureAwait(false);
				var payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				OkPayload.Create(payload);
				return true;
			}
			catch (EndOfStreamException)
			{
			}
			catch (IOException)
			{
			}
			catch (SocketException)
			{
			}

			VerifyState(State.Failed);
			return false;
		}

		// Starts a new conversation with the server by sending the first packet.
		public ValueTask<int> SendAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			m_payloadHandler.StartNewConversation();
			return SendReplyAsync(payload, ioBehavior, cancellationToken);
		}

		// Starts a new conversation with the server by receiving the first packet.
		public ValueTask<PayloadData> ReceiveAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			m_payloadHandler.StartNewConversation();
			return ReceiveReplyAsync(ioBehavior, cancellationToken);
		}

		// Continues a conversation with the server by receiving a response to a packet sent with 'Send' or 'SendReply'.
		public ValueTask<PayloadData> ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			ValueTask<ArraySegment<byte>> task;
			try
			{
				VerifyConnected();
				task = m_payloadHandler.ReadPayloadAsync(m_payloadCache, ProtocolErrorBehavior.Throw, ioBehavior);
			}
			catch (Exception ex)
			{
				task = ValueTaskExtensions.FromException<ArraySegment<byte>>(ex);
			}

			if (task.IsCompletedSuccessfully)
			{
				var payload = new PayloadData(task.Result);
				if (payload.HeaderByte != ErrorPayload.Signature)
					return new ValueTask<PayloadData>(payload);

				var exception = ErrorPayload.Create(payload).ToException();
				return ValueTaskExtensions.FromException<PayloadData>(exception);
			}

			return new ValueTask<PayloadData>(task.AsTask().ContinueWith(TryAsyncContinuation, cancellationToken, TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
		}

		// Continues a conversation with the server by sending a reply to a packet received with 'Receive' or 'ReceiveReply'.
		public ValueTask<int> SendReplyAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			ValueTask<int> task;
			try
			{
				VerifyConnected();
				task = m_payloadHandler.WritePayloadAsync(payload.ArraySegment, ioBehavior);
			}
			catch (Exception ex)
			{
				task = ValueTaskExtensions.FromException<int>(ex);
			}

			if (task.IsCompletedSuccessfully)
				return task;

			return new ValueTask<int>(task.AsTask().ContinueWith(TryAsyncContinuation, cancellationToken, TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
		}

		private void VerifyConnected()
		{
			lock (m_lock)
			{
				if (m_state == State.Closed)
					throw new ObjectDisposedException(nameof(MySqlSession));
				if (m_state != State.Connected && m_state != State.Querying && m_state != State.CancelingQuery && m_state != State.ClearingPendingCancellation && m_state != State.Closing)
					throw new InvalidOperationException("MySqlSession is not connected.");
			}
		}

		private async Task<bool> OpenTcpSocketAsync(ConnectionSettings cs, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			foreach (var hostname in cs.Hostnames)
			{
				IPAddress[] ipAddresses;
				try
				{
#if NETSTANDARD1_3
// Dns.GetHostAddresses isn't available until netstandard 2.0: https://github.com/dotnet/corefx/pull/11950
					ipAddresses = await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false);
#else
					if (ioBehavior == IOBehavior.Asynchronous)
					{
						ipAddresses = await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false);
					}
					else
					{
						ipAddresses = Dns.GetHostAddresses(hostname);
					}
#endif
				}
				catch (SocketException)
				{
					// name couldn't be resolved
					continue;
				}

				// need to try IP Addresses one at a time: https://github.com/dotnet/corefx/issues/5829
				foreach (var ipAddress in ipAddresses)
				{
					TcpClient tcpClient = null;
					try
					{
						tcpClient = new TcpClient(ipAddress.AddressFamily);
						using (cancellationToken.Register(() => tcpClient?.Client?.Dispose()))
						{
							try
							{
								if (ioBehavior == IOBehavior.Asynchronous)
								{
									await tcpClient.ConnectAsync(ipAddress, cs.Port).ConfigureAwait(false);
								}
								else
								{
#if NETSTANDARD1_3
									await tcpClient.ConnectAsync(ipAddress, cs.Port).ConfigureAwait(false);
#else
									tcpClient.Connect(ipAddress, cs.Port);
#endif
								}
							}
							catch (ObjectDisposedException ex) when (cancellationToken.IsCancellationRequested)
							{
								throw new MySqlException("Connect Timeout expired.", ex);
							}
						}
					}
					catch (SocketException)
					{
						tcpClient?.Client?.Dispose();
						continue;
					}

					m_hostname = hostname;
					m_tcpClient = tcpClient;
					m_socket = m_tcpClient.Client;
					m_networkStream = m_tcpClient.GetStream();
					SerializationUtility.SetKeepalive(m_socket, cs.Keepalive);
					lock (m_lock)
						m_state = State.Connected;
					return true;
				}
			}
			return false;
		}

		private async Task<bool> OpenUnixSocketAsync(ConnectionSettings cs, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
			var unixEp = new UnixEndPoint(cs.UnixSocket);
			try
			{
				using (cancellationToken.Register(() => socket.Dispose()))
				{
					try
					{
						if (ioBehavior == IOBehavior.Asynchronous)
						{
#if NETSTANDARD1_3
							await socket.ConnectAsync(unixEp).ConfigureAwait(false);
#else
							await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, unixEp, null).ConfigureAwait(false);
#endif
						}
						else
						{
							socket.Connect(unixEp);
						}
					}
					catch (ObjectDisposedException ex) when (cancellationToken.IsCancellationRequested)
					{
						throw new MySqlException("Connect Timeout expired.", ex);
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
				m_networkStream = new NetworkStream(socket);

				lock (m_lock)
					m_state = State.Connected;
				return true;
			}

			return false;
		}

		private async Task InitSslAsync(ProtocolCapabilities serverCapabilities, ConnectionSettings cs, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			X509CertificateCollection clientCertificates = null;
			if (cs.CertificateFile != null)
			{
				try
				{
					var certificate = new X509Certificate2(cs.CertificateFile, cs.CertificatePassword);
#if !NET451
					m_clientCertificate = certificate;
#endif
					clientCertificates = new X509CertificateCollection {certificate};
				}
				catch (CryptographicException ex)
				{
					if (!File.Exists(cs.CertificateFile))
						throw new MySqlException("Cannot find Certificate File", ex);
					throw new MySqlException("Either the Certificate Password is incorrect or the Certificate File is invalid", ex);
				}
			}

			X509Chain caCertificateChain = null;
			if (cs.CACertificateFile != null)
			{
				try
				{
					var caCertificate = new X509Certificate2(cs.CACertificateFile);
#if !NET451
					m_serverCertificate = caCertificate;
#endif
					caCertificateChain = new X509Chain
					{
						ChainPolicy =
						{
							RevocationMode = X509RevocationMode.NoCheck,
							VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority
						}
					};
					caCertificateChain.ChainPolicy.ExtraStore.Add(caCertificate);
				}
				catch (CryptographicException ex)
				{
					if (!File.Exists(cs.CACertificateFile))
						throw new MySqlException("Cannot find CA Certificate File", ex);
					throw new MySqlException("The CA Certificate File is invalid", ex);
				}
			}

			X509Certificate ValidateLocalCertificate(object lcbSender, string lcbTargetHost, X509CertificateCollection lcbLocalCertificates, X509Certificate lcbRemoteCertificate, string[] lcbAcceptableIssuers) => lcbLocalCertificates[0];

			bool ValidateRemoteCertificate(object rcbSender, X509Certificate rcbCertificate, X509Chain rcbChain, SslPolicyErrors rcbPolicyErrors)
			{
				if (cs.SslMode == MySqlSslMode.Preferred || cs.SslMode == MySqlSslMode.Required)
					return true;

				if ((rcbPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 && caCertificateChain != null)
				{
					if (caCertificateChain.Build((X509Certificate2) rcbCertificate))
					{
						var chainStatus = caCertificateChain.ChainStatus[0].Status & ~X509ChainStatusFlags.UntrustedRoot;
						if (chainStatus == X509ChainStatusFlags.NoError)
							rcbPolicyErrors &= ~SslPolicyErrors.RemoteCertificateChainErrors;
					}
				}

				if (cs.SslMode == MySqlSslMode.VerifyCA)
					rcbPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNameMismatch;

				return rcbPolicyErrors == SslPolicyErrors.None;
			}

			SslStream sslStream;
			if (clientCertificates == null)
				sslStream = new SslStream(m_networkStream, false, ValidateRemoteCertificate);
			else
				sslStream = new SslStream(m_networkStream, false, ValidateRemoteCertificate, ValidateLocalCertificate);

			// SslProtocols.Tls1.2 throws an exception in Windows, see https://github.com/mysql-net/MySqlConnector/pull/101
			var sslProtocols = SslProtocols.Tls | SslProtocols.Tls11;
			if (!Utility.IsWindows())
				sslProtocols |= SslProtocols.Tls12;

			var checkCertificateRevocation = cs.SslMode == MySqlSslMode.VerifyFull;

			var initSsl = new PayloadData(new ArraySegment<byte>(HandshakeResponse41Packet.InitSsl(serverCapabilities, cs, m_useCompression)));
			await SendReplyAsync(initSsl, ioBehavior, cancellationToken).ConfigureAwait(false);

			try
			{
				if (ioBehavior == IOBehavior.Asynchronous)
				{
					await sslStream.AuthenticateAsClientAsync(m_hostname, clientCertificates, sslProtocols, checkCertificateRevocation).ConfigureAwait(false);
				}
				else
				{
#if NETSTANDARD1_3
					await sslStream.AuthenticateAsClientAsync(m_hostname, clientCertificates, sslProtocols, checkCertificateRevocation).ConfigureAwait(false);
#else
					sslStream.AuthenticateAsClient(m_hostname, clientCertificates, sslProtocols, checkCertificateRevocation);
#endif
				}
				var sslByteHandler = new StreamByteHandler(sslStream);
				m_payloadHandler.ByteHandler = sslByteHandler;
				m_isSecureConnection = true;
			}
			catch (Exception ex)
			{
				sslStream.Dispose();
				ShutdownSocket();
				m_hostname = "";
				lock (m_lock)
					m_state = State.Failed;
				if (ex is AuthenticationException)
					throw new MySqlException("SSL Authentication Error", ex);
				if (ex is IOException && clientCertificates != null)
					throw new MySqlException("MySQL Server rejected client certificate", ex);
				throw;
			}
		}

		private void ShutdownSocket()
		{
			Utility.Dispose(ref m_payloadHandler);
			Utility.Dispose(ref m_networkStream);
			SafeDispose(ref m_tcpClient);
			SafeDispose(ref m_socket);
#if !NET451
			Utility.Dispose(ref m_clientCertificate);
			Utility.Dispose(ref m_serverCertificate);
#endif
		}

		/// <summary>
		/// Disposes and sets <paramref name="disposable"/> to <c>null</c>, ignoring any
		/// <see cref="SocketException"/> that is thrown.
		/// </summary>
		/// <typeparam name="T">An <see cref="IDisposable"/> type.</typeparam>
		/// <param name="disposable">The object to dispose.</param>
		private static void SafeDispose<T>(ref T disposable)
			where T : class, IDisposable
		{
			if (disposable != null)
			{
				try
				{
					disposable.Dispose();
				}
				catch (SocketException)
				{
				}
				disposable = null;
			}
		}

		private int TryAsyncContinuation(Task<int> task)
		{
			if (task.IsFaulted)
			{
				SetFailed();
				task.GetAwaiter().GetResult();
			}
			return 0;
		}

		private PayloadData TryAsyncContinuation(Task<ArraySegment<byte>> task)
		{
			if (task.IsFaulted)
				SetFailed();
			var payload = new PayloadData(task.GetAwaiter().GetResult());
			payload.ThrowIfError();
			return payload;
		}

		private void SetFailed()
		{
			lock (m_lock)
				m_state = State.Failed;
		}

		private void VerifyState(State state)
		{
			if (m_state != state)
				throw new InvalidOperationException("Expected state to be {0} but was {1}.".FormatInvariant(state, m_state));
		}

		private void VerifyState(State state1, State state2)
		{
			if (m_state != state1 && m_state != state2)
				throw new InvalidOperationException("Expected state to be ({0}|{1}) but was {2}.".FormatInvariant(state1, state2, m_state));
		}

		private static byte[] CreateConnectionAttributes()
		{
			var attributesWriter = new PayloadWriter();
			attributesWriter.WriteLengthEncodedString("_client_name");
			attributesWriter.WriteLengthEncodedString("MySqlConnector");
			attributesWriter.WriteLengthEncodedString("_client_version");
			attributesWriter.WriteLengthEncodedString(typeof(MySqlSession).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
			try
			{
				var os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
					RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
						RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "macOS" : null;
				var osDetails = RuntimeInformation.OSDescription;
				var platform = RuntimeInformation.ProcessArchitecture.ToString();
				if (os != null)
				{
					attributesWriter.WriteLengthEncodedString("_os");
					attributesWriter.WriteLengthEncodedString(os);
				}
				attributesWriter.WriteLengthEncodedString("_os_details");
				attributesWriter.WriteLengthEncodedString(osDetails);
				attributesWriter.WriteLengthEncodedString("_platform");
				attributesWriter.WriteLengthEncodedString(platform);
			}
			catch (PlatformNotSupportedException)
			{
			}
			using (var process = Process.GetCurrentProcess())
			{
				attributesWriter.WriteLengthEncodedString("_pid");
				attributesWriter.WriteLengthEncodedString(process.Id.ToString(CultureInfo.InvariantCulture));
			}
			var connectionAttributes = attributesWriter.ToBytes();

			var writer = new PayloadWriter();
			writer.WriteLengthEncodedInteger((ulong) connectionAttributes.Length);
			writer.Write(connectionAttributes);
			return writer.ToBytes();
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

		static byte[] s_connectionAttributes;

		readonly object m_lock;
		readonly ArraySegmentHolder<byte> m_payloadCache;
		State m_state;
		string m_hostname = "";
		TcpClient m_tcpClient;
		Socket m_socket;
		NetworkStream m_networkStream;
#if !NET451
		IDisposable m_clientCertificate;
		IDisposable m_serverCertificate;
#endif
		IPayloadHandler m_payloadHandler;
		MySqlCommand m_activeCommand;
		MySqlDataReader m_activeReader;
		bool m_useCompression;
		bool m_isSecureConnection;
		bool m_supportsConnectionAttributes;
	}
}
