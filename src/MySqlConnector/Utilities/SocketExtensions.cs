using System;
using System.Net.Sockets;

namespace MySqlConnector.Utilities
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

		public static void SetKeepAlive(this Socket socket, uint keepAliveTimeSeconds)
		{
			// Always use the OS Default Keepalive settings
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
			if (keepAliveTimeSeconds == 0)
				return;

			// If keepAliveTimeSeconds > 0, override keepalive options on the socket
			const uint keepAliveIntervalMillis = 1000;
			if (Utility.IsWindows())
			{
				// http://stackoverflow.com/a/11834055/1419658
				// Windows takes time in milliseconds
				var keepAliveTimeMillis = keepAliveTimeSeconds > uint.MaxValue / 1000 ? uint.MaxValue : keepAliveTimeSeconds * 1000;
				var inOptionValues = new byte[sizeof(uint) * 3];
				BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
				BitConverter.GetBytes(keepAliveTimeMillis).CopyTo(inOptionValues, sizeof(uint));
				BitConverter.GetBytes(keepAliveIntervalMillis).CopyTo(inOptionValues, sizeof(uint) * 2);
				socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
			}
			// Unix not supported: The appropriate socket options to set Keepalive options are not exposd in .NET
			// https://github.com/dotnet/corefx/issues/14237
			// Unix will still respect the OS Default Keepalive settings
		}
	}
}
