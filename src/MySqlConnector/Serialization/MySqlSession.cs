using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MySql.Data.Serialization
{
	internal sealed class MySqlSession
	{
		public MySqlSession()
			: this(null, 0)
		{
		}

		public MySqlSession(ConnectionPool pool, int poolGeneration)
		{
			Pool = pool;
			PoolGeneration = poolGeneration;
		}

		public ServerVersion ServerVersion { get; set; }
		public int ConnectionId { get; set; }
		public byte[] AuthPluginData { get; set; }
		public ConnectionPool Pool { get; }
		public int PoolGeneration { get; }

		public void ReturnToPool() => Pool?.Return(this);

		public async Task DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (m_transmitter != null)
			{
				try
				{
					await m_transmitter.SendAsync(QuitPayload.Create(), ioBehavior, cancellationToken).ConfigureAwait(false);
					await m_transmitter.TryReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				catch (SocketException)
				{
					// socket may have been closed during shutdown; ignore
				}
				m_transmitter = null;
			}
			if (m_socket != null)
			{
				try
				{
					if (m_socket.Connected)
						m_socket.Shutdown(SocketShutdown.Both);
					m_socket.Dispose();
				}
				catch (SocketException)
				{
				}
				m_socket = null;
			}
			m_state = State.Closed;
		}

		public async Task ConnectAsync(IEnumerable<string> hosts, int port, string userId, string password, string database, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			var connected = await OpenSocketAsync(hosts, port, ioBehavior, cancellationToken).ConfigureAwait(false);
			if (!connected)
				throw new MySqlException("Unable to connect to any of the specified MySQL hosts.");

			var payload = await ReceiveAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			var reader = new ByteArrayReader(payload.ArraySegment.Array, payload.ArraySegment.Offset, payload.ArraySegment.Count);
			var initialHandshake = new InitialHandshakePacket(reader);
			if (initialHandshake.AuthPluginName != "mysql_native_password")
				throw new NotSupportedException("Only 'mysql_native_password' authentication method is supported.");
			ServerVersion = new ServerVersion(Encoding.ASCII.GetString(initialHandshake.ServerVersion));
			ConnectionId = initialHandshake.ConnectionId;
			AuthPluginData = initialHandshake.AuthPluginData;

			var response = HandshakeResponse41Packet.Create(initialHandshake, userId, password, database);
			payload = new PayloadData(new ArraySegment<byte>(response));
			await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
			await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}

		public async Task ResetConnectionAsync(string userId, string password, string database, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (ServerVersion.Version.CompareTo(ServerVersions.SupportsResetConnection) >= 0)
			{
				await SendAsync(ResetConnectionPayload.Create(), ioBehavior, cancellationToken).ConfigureAwait(false);
				var payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				OkPayload.Create(payload);

				// the "reset connection" packet also resets the connection charset, so we need to change that back to our default
				payload = new PayloadData(new ArraySegment<byte>(PayloadUtilities.CreateEofStringPayload(CommandKind.Query, "SET NAMES utf8mb4;")));
				await SendAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				OkPayload.Create(payload);
			}
			else
			{
				// optimistically hash the password with the challenge from the initial handshake (supported by MariaDB; doesn't appear to be supported by MySQL)
				var hashedPassword = AuthenticationUtility.CreateAuthenticationResponse(AuthPluginData, 0, password);
				var payload = ChangeUserPayload.Create(userId, hashedPassword, database);
				await SendAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
				payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				if (payload.HeaderByte == AuthenticationMethodSwitchRequestPayload.Signature)
				{
					// if the server didn't support the hashed password; rehash with the new challenge
					var switchRequest = AuthenticationMethodSwitchRequestPayload.Create(payload);
					if (switchRequest.Name != "mysql_native_password")
						throw new NotSupportedException("Only 'mysql_native_password' authentication method is supported.");
					hashedPassword = AuthenticationUtility.CreateAuthenticationResponse(switchRequest.Data, 0, password);
					payload = new PayloadData(new ArraySegment<byte>(hashedPassword));
					await SendReplyAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
					payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				OkPayload.Create(payload);
			}
		}

		public async Task<bool> TryPingAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			await SendAsync(PingPayload.Create(), ioBehavior, cancellationToken).ConfigureAwait(false);
			try
			{
				var payload = await ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				OkPayload.Create(payload);
				return true;
			}
			catch (EndOfStreamException)
			{
			}
			catch (SocketException)
			{
			}

			return false;
		}

		// Starts a new conversation with the server by sending the first packet.
		public ValueTask<int> SendAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
			=> TryAsync(m_transmitter.SendAsync, payload, ioBehavior, cancellationToken);

		// Starts a new conversation with the server by receiving the first packet.
		public ValueTask<PayloadData> ReceiveAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
			=> TryAsync(m_transmitter.ReceiveAsync, ioBehavior, cancellationToken);

		// Continues a conversation with the server by receiving a response to a packet sent with 'Send' or 'SendReply'.
		public ValueTask<PayloadData> ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
			=> TryAsync(m_transmitter.ReceiveReplyAsync, ioBehavior, cancellationToken);

		// Continues a conversation with the server by sending a reply to a packet received with 'Receive' or 'ReceiveReply'.
		public ValueTask<int> SendReplyAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
			=> TryAsync(m_transmitter.SendReplyAsync, payload, ioBehavior, cancellationToken);

		private void VerifyConnected()
		{
			if (m_state == State.Closed)
				throw new ObjectDisposedException(nameof(MySqlSession));
			if (m_state != State.Connected)
				throw new InvalidOperationException("MySqlSession is not connected.");
		}

		private async Task<bool> OpenSocketAsync(IEnumerable<string> hostnames, int port, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			foreach (var hostname in hostnames)
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
					Socket socket = null;
					try
					{
						socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
						using (cancellationToken.Register(() => socket.Dispose()))
						{
							try
							{
								if (ioBehavior == IOBehavior.Asynchronous)
								{
#if NETSTANDARD1_3
									await socket.ConnectAsync(ipAddress, port).ConfigureAwait(false);
#else
									await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, ipAddress, port, null).ConfigureAwait(false);
#endif
								}
								else
								{
#if NETSTANDARD1_3
									await socket.ConnectAsync(ipAddress, port).ConfigureAwait(false);
#else
									socket.Connect(ipAddress, port);
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
						Utility.Dispose(ref socket);
						continue;
					}

					m_socket = socket;
					m_transmitter = new PacketTransmitter(m_socket);
					m_state = State.Connected;
					return true;
				}
			}
			return false;
		}

		private ValueTask<int> TryAsync<TArg>(Func<TArg, IOBehavior, CancellationToken, ValueTask<int>> func, TArg arg, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyConnected();
			var task = func(arg, ioBehavior, cancellationToken);
			if (task.IsCompletedSuccessfully)
				return task;

			return new ValueTask<int>(task.AsTask()
				.ContinueWith(TryAsyncContinuation, cancellationToken, TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
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

		private ValueTask<PayloadData> TryAsync(Func<IOBehavior, CancellationToken, ValueTask<PayloadData>> func, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyConnected();
			var task = func(ioBehavior, cancellationToken);
			if (task.IsCompletedSuccessfully)
			{
				if (task.Result.HeaderByte != ErrorPayload.Signature)
					return task;

				var exception = ErrorPayload.Create(task.Result).ToException();
#if NETSTANDARD1_3
				return new ValueTask<PayloadData>(Task.FromException<PayloadData>(exception));
#else
				var tcs = new TaskCompletionSource<PayloadData>();
				tcs.SetException(exception);
				return new ValueTask<PayloadData>(tcs.Task);
#endif
			}

			return new ValueTask<PayloadData>(task.AsTask()
				.ContinueWith(TryAsyncContinuation, cancellationToken, TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default));
		}

		private PayloadData TryAsyncContinuation(Task<PayloadData> task)
		{
			if (task.IsFaulted)
				SetFailed();
			var payload = task.GetAwaiter().GetResult();
			payload.ThrowIfError();
			return payload;
		}

		private void SetFailed()
		{
			m_state = State.Failed;
		}

		private enum State
		{
			Created,
			Connected,
			Closed,
			Failed,
		}

		State m_state;
		Socket m_socket;
		PacketTransmitter m_transmitter;
	}
}
