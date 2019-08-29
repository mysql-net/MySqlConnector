#if NETSTANDARD2_1 || NETCOREAPP3_0
#define VALUETASKSOURCE
#endif

using System;
using System.Net.Sockets;
using System.Threading.Tasks;
#if VALUETASKSOURCE
using System.Threading.Tasks.Sources;
#endif
using MySql.Data.MySqlClient;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Serialization
{
	internal sealed class SocketByteHandler : IByteHandler
#if VALUETASKSOURCE
		, IValueTaskSource<int>
#endif
	{
		public SocketByteHandler(Socket socket)
		{
			m_socket = socket;
#if VALUETASKSOURCE
			m_valueTaskSource = new ManualResetValueTaskSourceCore<int> { RunContinuationsAsynchronously = true };
			m_socketEventArgs = new SocketAsyncEventArgs();
			m_socketEventArgs.Completed += (s, e) => PropagateSocketAsyncEventArgsStatus();
#else
			m_socketAwaitable = new SocketAwaitable(new SocketAsyncEventArgs());
#endif
			m_closeSocket = socket.Dispose;
			RemainingTimeout = Constants.InfiniteTimeout;
		}

#if VALUETASKSOURCE
		public void Dispose() => m_socketEventArgs.Dispose();
#else
		public void Dispose() => m_socketAwaitable.EventArgs.Dispose();
#endif

		public int RemainingTimeout { get; set; }

		public ValueTask<int> ReadBytesAsync(ArraySegment<byte> buffer, IOBehavior ioBehavior) =>
			ioBehavior == IOBehavior.Asynchronous ? DoReadBytesAsync(buffer) : DoReadBytesSync(buffer);

		private ValueTask<int> DoReadBytesSync(ArraySegment<byte> buffer)
		{
			try
			{
				if (RemainingTimeout == Constants.InfiniteTimeout)
					return new ValueTask<int>(m_socket.Receive(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None));

				while (RemainingTimeout > 0)
				{
					var startTime = Environment.TickCount;
					if (m_socket.Poll(Math.Min(int.MaxValue / 1000, RemainingTimeout) * 1000, SelectMode.SelectRead))
					{
						var bytesRead = m_socket.Receive(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None);
						RemainingTimeout -= unchecked(Environment.TickCount - startTime);
						return new ValueTask<int>(bytesRead);
					}
					RemainingTimeout -= unchecked(Environment.TickCount - startTime);
				}
				return ValueTaskExtensions.FromException<int>(MySqlException.CreateForTimeout());
			}
			catch (Exception ex)
			{
				return ValueTaskExtensions.FromException<int>(ex);
			}
		}

		private async ValueTask<int> DoReadBytesAsync(ArraySegment<byte> buffer)
		{
			var startTime = RemainingTimeout == Constants.InfiniteTimeout ? 0 : Environment.TickCount;
			var timerId = RemainingTimeout == Constants.InfiniteTimeout ? 0 :
				RemainingTimeout <= 0 ? throw MySqlException.CreateForTimeout() :
				TimerQueue.Instance.Add(RemainingTimeout, m_closeSocket);
#if VALUETASKSOURCE
			m_socketEventArgs.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
#else
			m_socketAwaitable.EventArgs.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
#endif
			int bytesRead;
			try
			{
#if VALUETASKSOURCE
				m_valueTaskSource.Reset();
				if (!m_socket.ReceiveAsync(m_socketEventArgs))
				{
					if (m_socketEventArgs.SocketError != SocketError.Success)
						throw new SocketException((int) m_socketEventArgs.SocketError);
					else
						bytesRead = m_socketEventArgs.BytesTransferred;
				}
				else
				{
					bytesRead = await new ValueTask<int>(this, m_valueTaskSource.Version).ConfigureAwait(false);
				}
#else
				await m_socket.ReceiveAsync(m_socketAwaitable);
				bytesRead = m_socketAwaitable.EventArgs.BytesTransferred;
#endif
			}
			catch (SocketException ex)
			{
				if (RemainingTimeout != Constants.InfiniteTimeout)
				{
					RemainingTimeout -= unchecked(Environment.TickCount - startTime);
					if (!TimerQueue.Instance.Remove(timerId))
						throw MySqlException.CreateForTimeout(ex);
				}
				throw;
			}
			if (RemainingTimeout != Constants.InfiniteTimeout)
			{
				RemainingTimeout -= unchecked(Environment.TickCount - startTime);
				if (!TimerQueue.Instance.Remove(timerId))
					throw MySqlException.CreateForTimeout();
			}
			return bytesRead;
		}

		public ValueTask<int> WriteBytesAsync(ArraySegment<byte> data, IOBehavior ioBehavior)
		{
			if (ioBehavior == IOBehavior.Asynchronous)
				return DoWriteBytesAsync(data);

			try
			{
				m_socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
				return default;
			}
			catch (Exception ex)
			{
				return ValueTaskExtensions.FromException<int>(ex);
			}
		}

#if VALUETASKSOURCE
		private ValueTask<int> DoWriteBytesAsync(ArraySegment<byte> data)
		{
			m_socketEventArgs.SetBuffer(data.Array, data.Offset, data.Count);
			m_valueTaskSource.Reset();
			if (!m_socket.SendAsync(m_socketEventArgs))
				PropagateSocketAsyncEventArgsStatus();
			return new ValueTask<int>(this, m_valueTaskSource.Version);
		}
#else
		private async ValueTask<int> DoWriteBytesAsync(ArraySegment<byte> data)
		{
			m_socketAwaitable.EventArgs.SetBuffer(data.Array, data.Offset, data.Count);
			await m_socket.SendAsync(m_socketAwaitable);
			return 0;
		}
#endif

#if VALUETASKSOURCE
		int IValueTaskSource<int>.GetResult(short token) => m_valueTaskSource.GetResult(token);
		ValueTaskSourceStatus IValueTaskSource<int>.GetStatus(short token) => m_valueTaskSource.GetStatus(token);
		void IValueTaskSource<int>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
			m_valueTaskSource.OnCompleted(continuation, state, token, flags);

		private void PropagateSocketAsyncEventArgsStatus()
		{
			if (m_socketEventArgs.SocketError != SocketError.Success)
				m_valueTaskSource.SetException(new SocketException((int) m_socketEventArgs.SocketError));
			else
				m_valueTaskSource.SetResult(m_socketEventArgs.BytesTransferred);
		}
#endif

		readonly Socket m_socket;
#if VALUETASKSOURCE
		ManualResetValueTaskSourceCore<int> m_valueTaskSource; // mutable struct; do not make this readonly
		readonly SocketAsyncEventArgs m_socketEventArgs;
#else
		readonly SocketAwaitable m_socketAwaitable;
#endif
		readonly Action m_closeSocket;
	}
}
