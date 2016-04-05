using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data
{
	internal static class SocketExtensions
	{
		public static SocketAwaitable ReceiveAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			if (!socket.ReceiveAsync(awaitable.EventArgs))
				awaitable.WasCompleted = true;
			return awaitable;
		}

		public static SocketAwaitable SendAsync(this Socket socket, SocketAwaitable awaitable)
		{
			awaitable.Reset();
			if (!socket.SendAsync(awaitable.EventArgs))
				awaitable.WasCompleted = true;
			return awaitable;
		}

		public static async Task<int> ReadAvailableAsync(this Socket socket, SocketAwaitable awaitable, CancellationToken cancellationToken)
		{
			int totalBytesRead = 0;
			int offset = awaitable.EventArgs.Offset;
			int count = awaitable.EventArgs.Count;
			while (count > 0)
			{
				await socket.ReceiveAsync(awaitable);
				int bytesRead = awaitable.EventArgs.BytesTransferred;
				if (bytesRead <= 0)
					break;
				totalBytesRead += bytesRead;
				offset += bytesRead;
				count -= bytesRead;
				awaitable.EventArgs.SetBuffer(offset, count);
			}
			return totalBytesRead;
		}

		public static async Task ReadExactlyAsync(this Socket socket, SocketAwaitable awaitable, CancellationToken cancellationToken)
		{
			int count = awaitable.EventArgs.Count;
			var bytesRead = await socket.ReadAvailableAsync(awaitable, cancellationToken).ConfigureAwait(false);
			if (bytesRead != count)
				throw new EndOfStreamException();
		}
	}
}
