using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class SocketProtocolLayer : BaseProtocolLayer
	{
		public SocketProtocolLayer(Socket socket)
		{
			m_socket = socket;
			var socketEventArgs = new SocketAsyncEventArgs();
			m_socketAwaitable = new SocketAwaitable(socketEventArgs);
			m_buffer = new byte[16384];
		}

		public override ValueTask<ArraySegment<byte>> ReadAsync(int? count, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			if (!count.HasValue)
				throw new ArgumentException("count must be specified for SocketProtocolLayer.ReadAsync", nameof(count));

			var buffer = count.Value < m_buffer.Length ? m_buffer : new byte[count.Value];
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				return new ValueTask<ArraySegment<byte>>(DoReadBytesAsync(buffer, 0, count.Value));
			}
			else
			{
				var bytesRead = m_socket.Receive(buffer, 0, count.Value, SocketFlags.None);
				return new ValueTask<ArraySegment<byte>>(new ArraySegment<byte>(buffer, 0, bytesRead));
			}
		}

		public override ValueTask<int> WriteAsync(ArraySegment<byte> data, IOBehavior ioBehavior)
		{
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				return new ValueTask<int>(DoWriteBytesAsync(data));
			}
			else
			{
				m_socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
				return default(ValueTask<int>);
			}
		}

		public override ValueTask<int> FlushAsync(IOBehavior ioBehavior)
		{
			return default(ValueTask<int>);
		}

		public async Task<ArraySegment<byte>> DoReadBytesAsync(byte[] buffer, int offset, int count)
		{
			m_socketAwaitable.EventArgs.SetBuffer(buffer, offset, count);
			await m_socket.ReceiveAsync(m_socketAwaitable);
			return new ArraySegment<byte>(buffer, 0, m_socketAwaitable.EventArgs.BytesTransferred);
		}

		private async Task<int> DoWriteBytesAsync(ArraySegment<byte> payload)
		{
			m_socketAwaitable.EventArgs.SetBuffer(payload.Array, payload.Offset, payload.Count);
			await m_socket.SendAsync(m_socketAwaitable);
			return 0;
		}

		private readonly Socket m_socket;
		private readonly SocketAwaitable m_socketAwaitable;
		private byte[] m_buffer;
	}
}
