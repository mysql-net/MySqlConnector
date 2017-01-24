using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class SocketByteHandler : IByteHandler
	{
		public SocketByteHandler(Socket socket)
		{
			m_socket = socket;
			var socketEventArgs = new SocketAsyncEventArgs();
			m_socketAwaitable = new SocketAwaitable(socketEventArgs);
			m_buffer = new byte[16384];
		}

		public ValueTask<ArraySegment<byte>> ReadBytesAsync(int count, IOBehavior ioBehavior)
		{
			var buffer = count < m_buffer.Length ? m_buffer : new byte[count];
			if (ioBehavior == IOBehavior.Asynchronous && m_socket.Available < count)
			{
				return new ValueTask<ArraySegment<byte>>(DoReadBytesAsync(buffer, 0, count));
			}
			else
			{
				var bytesRead = m_socket.Receive(buffer, 0, count, SocketFlags.None);
				return new ValueTask<ArraySegment<byte>>(new ArraySegment<byte>(buffer, 0, bytesRead));
			}
		}

		public ValueTask<int> WriteBytesAsync(ArraySegment<byte> data, IOBehavior ioBehavior)
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

		private async Task<ArraySegment<byte>> DoReadBytesAsync(byte[] buffer, int offset, int count)
		{
			m_socketAwaitable.EventArgs.SetBuffer(buffer, offset, count);
			await m_socket.ReceiveAsync(m_socketAwaitable);
			return new ArraySegment<byte>(buffer, 0, m_socketAwaitable.EventArgs.BytesTransferred);
		}

		private async Task<int> DoWriteBytesAsync(ArraySegment<byte> payload)
		{
			if (payload.Count <= m_buffer.Length)
			{
				Buffer.BlockCopy(payload.Array, payload.Offset, m_buffer, 0, payload.Count);
				m_socketAwaitable.EventArgs.SetBuffer(m_buffer, 0, payload.Count);
			}
			else
			{
				m_socketAwaitable.EventArgs.SetBuffer(payload.Array, payload.Offset, payload.Count);
			}
			await m_socket.SendAsync(m_socketAwaitable);
			return 0;
		}

		readonly Socket m_socket;
		readonly SocketAwaitable m_socketAwaitable;
		readonly byte[] m_buffer;
	}
}
