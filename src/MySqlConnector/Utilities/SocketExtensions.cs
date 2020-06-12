using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MySqlConnector.Utilities
{
	internal static class SocketExtensions
	{
#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0
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
#endif

#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0
		public static void SetBuffer(this SocketAsyncEventArgs args, Memory<byte> buffer)
		{
			MemoryMarshal.TryGetArray<byte>(buffer, out var arraySegment);
			args.SetBuffer(arraySegment.Array, arraySegment.Offset, arraySegment.Count);
		}

		public static int Send(this Socket socket, ReadOnlyMemory<byte> data, SocketFlags flags)
		{
			MemoryMarshal.TryGetArray(data, out var arraySegment);
			return socket.Send(arraySegment.Array, arraySegment.Offset, arraySegment.Count, flags);
		}
#else
		public static int Send(this Socket socket, ReadOnlyMemory<byte> data, SocketFlags flags) => socket.Send(data.Span, flags);
#endif

		public static void SetKeepAlive(this Socket socket, uint keepAliveTimeSeconds)
		{
			// Always use the OS Default Keepalive settings
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
			if (keepAliveTimeSeconds == 0)
				return;

			// If keepAliveTimeSeconds > 0, override keepalive options on the socket
#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0 || NETSTANDARD2_1 || NETCOREAPP2_1
			if (Utility.IsWindows())
			{
				// http://stackoverflow.com/a/11834055/1419658
				// Windows takes time in milliseconds
				var keepAliveTimeMillis = keepAliveTimeSeconds > uint.MaxValue / 1000 ? uint.MaxValue : keepAliveTimeSeconds * 1000;
				var inOptionValues = new byte[12];
				inOptionValues[0] = 1;
				inOptionValues[4] = (byte) (keepAliveTimeMillis & 0xFF);
				inOptionValues[5] = (byte) ((keepAliveTimeMillis >> 8) & 0xFF);
				inOptionValues[6] = (byte) ((keepAliveTimeMillis >> 16) & 0xFF);
				inOptionValues[7] = (byte) ((keepAliveTimeMillis >> 24) & 0xFF);
				inOptionValues[8] = 0xE8;
				inOptionValues[9] = 0x03;
				socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
			}
			// Unix not supported: The appropriate socket options to set Keepalive options are not exposd in .NET
			// https://github.com/dotnet/corefx/issues/14237
			// Unix will still respect the OS Default Keepalive settings
#else
			socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 1);
			socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, (int) keepAliveTimeSeconds);
#endif
		}
	}
}
