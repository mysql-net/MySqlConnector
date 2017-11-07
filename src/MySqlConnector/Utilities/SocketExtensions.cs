using System.Net.Sockets;

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
	}
}
