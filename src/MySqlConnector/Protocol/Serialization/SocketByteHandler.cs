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
		}

		public void Dispose() => m_socketAwaitable.EventArgs.Dispose();

		public ValueTask<int> ReadBytesAsync(ArraySegment<byte> buffer, IOBehavior ioBehavior)
		{
			return (ioBehavior == IOBehavior.Asynchronous) ?
				new ValueTask<int>(DoReadBytesAsync(buffer)) :
				new ValueTask<int>(m_socket.Receive(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None));

			async Task<int> DoReadBytesAsync(ArraySegment<byte> buffer_)
			{
				m_socketAwaitable.EventArgs.SetBuffer(buffer_.Array, buffer_.Offset, buffer_.Count);
				await m_socket.ReceiveAsync(m_socketAwaitable);
				return m_socketAwaitable.EventArgs.BytesTransferred;
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

			async Task<int> DoWriteBytesAsync(ArraySegment<byte> data_)
			{
				m_socketAwaitable.EventArgs.SetBuffer(data_.Array, data_.Offset, data_.Count);
				await m_socket.SendAsync(m_socketAwaitable);
				return 0;
			}
		}

		readonly Socket m_socket;
		readonly SocketAwaitable m_socketAwaitable;
	}
}
