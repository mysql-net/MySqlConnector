using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class SocketByteHandler : IByteHandler
	{
		public SocketByteHandler(Socket socket)
		{
			m_socket = socket;
			var socketEventArgs = new SocketAsyncEventArgs();
			m_socketAwaitable = new SocketAwaitable(socketEventArgs);
		}

		public ValueTask<int> ReadBytesAsync(byte[] buffer, int offset, int count, IOBehavior ioBehavior)
		{
			return ioBehavior == IOBehavior.Asynchronous ?
				new ValueTask<int>(DoReadBytesAsync(buffer, offset, count)) :
				new ValueTask<int>(m_socket.Receive(buffer, offset, count, SocketFlags.None));
		}

		public ValueTask<int> WriteBytesAsync(ArraySegment<byte> payload, IOBehavior ioBehavior)
		{
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				return new ValueTask<int>(DoWriteBytesAsync(payload));
			}
			else
			{
				m_socket.Send(payload.Array, payload.Offset, payload.Count, SocketFlags.None);
				return default(ValueTask<int>);
			}
		}

		public async Task<int> DoReadBytesAsync(byte[] buffer, int offset, int count)
		{
			m_socketAwaitable.EventArgs.SetBuffer(buffer, offset, count);
			await m_socket.ReceiveAsync(m_socketAwaitable);
			return m_socketAwaitable.EventArgs.BytesTransferred;
		}

		private async Task<int> DoWriteBytesAsync(ArraySegment<byte> payload)
		{
			m_socketAwaitable.EventArgs.SetBuffer(payload.Array, payload.Offset, payload.Count);
			await m_socket.SendAsync(m_socketAwaitable);
			return 0;
		}

		private readonly Socket m_socket;
		private readonly SocketAwaitable m_socketAwaitable;
	}
}
