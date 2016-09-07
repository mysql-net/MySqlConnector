using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MySql.Data.Serialization
{
	internal sealed class MySqlSession : IDisposable
	{
		public MySqlSession(ConnectionPool pool)
		{
			Pool = pool;
		}

		public ServerVersion ServerVersion { get; set; }
		public byte[] AuthPluginData { get; set; }
		public ConnectionPool Pool { get; }
		public bool ReturnToPool() => Pool != null && Pool.Return(this);

		public void Dispose()
		{
			DisposeAsync(CancellationToken.None).GetAwaiter().GetResult();
		}

		public async Task DisposeAsync(CancellationToken cancellationToken)
		{
			if (m_transmitter != null)
			{
				try
				{
					await m_transmitter.SendAsync(QuitPayload.Create(), cancellationToken).ConfigureAwait(false);
					await m_transmitter.TryReceiveReplyAsync(cancellationToken).ConfigureAwait(false);
				}
				catch (SocketException)
				{
					// socket may have been closed during shutdown; ignore
				}
				m_transmitter = null;
			}
			if (m_socket != null)
			{
				if (m_socket.Connected)
					m_socket.Shutdown(SocketShutdown.Both);
				Utility.Dispose(ref m_socket);
			}
			m_state = State.Closed;
		}

		public async Task<bool> ConnectAsync(IEnumerable<string> hostnames, int port)
		{
			foreach (var hostname in hostnames)
			{
				IPAddress[] ipAddresses;
				try
				{
					ipAddresses = await Dns.GetHostAddressesAsync(hostname).ConfigureAwait(false);
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
#if NETSTANDARD1_3
						await socket.ConnectAsync(ipAddress, port).ConfigureAwait(false);
#else
						await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, hostname, port, null).ConfigureAwait(false);
#endif
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

		// Starts a new conversation with the server by sending the first packet.
		public Task SendAsync(PayloadData payload, CancellationToken cancellationToken)
			=> TryAsync(m_transmitter.SendAsync, payload, cancellationToken);

		// Starts a new conversation with the server by receiving the first packet.
		public ValueTask<PayloadData> ReceiveAsync(CancellationToken cancellationToken)
			=> TryAsync(m_transmitter.ReceiveAsync, cancellationToken);

		// Continues a conversation with the server by receiving a response to a packet sent with 'Send' or 'SendReply'.
		public ValueTask<PayloadData> ReceiveReplyAsync(CancellationToken cancellationToken)
			=> TryAsync(m_transmitter.ReceiveReplyAsync, cancellationToken);

		// Continues a conversation with the server by sending a reply to a packet received with 'Receive' or 'ReceiveReply'.
		public Task SendReplyAsync(PayloadData payload, CancellationToken cancellationToken)
			=> TryAsync(m_transmitter.SendReplyAsync, payload, cancellationToken);


		private void VerifyConnected()
		{
			if (m_state == State.Closed)
				throw new ObjectDisposedException(nameof(MySqlSession));
			if (m_state != State.Connected)
				throw new InvalidOperationException("MySqlSession is not connected.");
		}

		private Task TryAsync<TArg>(Func<TArg, CancellationToken, Task> func, TArg arg, CancellationToken cancellationToken)
		{
			VerifyConnected();
			var task = func(arg, cancellationToken);
			if (task.Status == TaskStatus.RanToCompletion)
				return task;

			return task.ContinueWith(TryAsyncContinuation, cancellationToken, TaskContinuationOptions.LazyCancellation | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		private void TryAsyncContinuation(Task task)
		{
			if (task.IsFaulted)
			{
				SetFailed();
				task.GetAwaiter().GetResult();
			}
		}

		private ValueTask<PayloadData> TryAsync(Func<CancellationToken, ValueTask<PayloadData>> func, CancellationToken cancellationToken)
		{
			VerifyConnected();
			var task = func(cancellationToken);
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
