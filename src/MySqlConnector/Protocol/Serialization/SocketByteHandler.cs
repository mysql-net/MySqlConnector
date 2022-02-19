using System.Net.Sockets;
using System.Runtime.InteropServices;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Serialization;

internal sealed class SocketByteHandler : IByteHandler
{
	public SocketByteHandler(Socket socket)
	{
		m_socket = socket;
#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
		m_socketAwaitable = new(new());
#endif
		m_closeSocket = socket.Dispose;
		RemainingTimeout = Constants.InfiniteTimeout;
	}

#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
	public void Dispose() => m_socketAwaitable.EventArgs.Dispose();
#else
	public void Dispose()
	{
	}
#endif

	public int RemainingTimeout { get; set; }

	public ValueTask<int> ReadBytesAsync(Memory<byte> buffer, IOBehavior ioBehavior) =>
		ioBehavior == IOBehavior.Asynchronous ? DoReadBytesAsync(buffer) : DoReadBytesSync(buffer);

	private ValueTask<int> DoReadBytesSync(Memory<byte> buffer)
	{
#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
		MemoryMarshal.TryGetArray<byte>(buffer, out var arraySegment);
#endif

		try
		{
			if (RemainingTimeout == Constants.InfiniteTimeout)
#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
				return new ValueTask<int>(m_socket.Receive(arraySegment.Array, arraySegment.Offset, arraySegment.Count, SocketFlags.None));
#else
				return new ValueTask<int>(m_socket.Receive(buffer.Span, SocketFlags.None));
#endif

			while (RemainingTimeout > 0)
			{
				var startTime = Environment.TickCount;
				if (m_socket.Poll(Math.Min(int.MaxValue / 1000, RemainingTimeout) * 1000, SelectMode.SelectRead))
				{
#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
					var bytesRead = m_socket.Receive(arraySegment.Array, arraySegment.Offset, arraySegment.Count, SocketFlags.None);
#else
					var bytesRead = m_socket.Receive(buffer.Span, SocketFlags.None);
#endif
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

	private async ValueTask<int> DoReadBytesAsync(Memory<byte> buffer)
	{
		var startTime = RemainingTimeout == Constants.InfiniteTimeout ? 0 : Environment.TickCount;
		var timerId = RemainingTimeout switch
		{
			Constants.InfiniteTimeout => 0u,
			<= 0 => throw MySqlException.CreateForTimeout(),
			_ => TimerQueue.Instance.Add(RemainingTimeout, m_closeSocket),
		};
#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
		m_socketAwaitable.EventArgs.SetBuffer(buffer);
#endif
		int bytesRead;
		try
		{
#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
			await m_socket.ReceiveAsync(m_socketAwaitable);
			bytesRead = m_socketAwaitable.EventArgs.BytesTransferred;
#else
			bytesRead = await m_socket.ReceiveAsync(buffer, SocketFlags.None).ConfigureAwait(false);
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

	public ValueTask<int> WriteBytesAsync(ReadOnlyMemory<byte> data, IOBehavior ioBehavior)
	{
		if (ioBehavior == IOBehavior.Asynchronous)
			return DoWriteBytesAsync(data);

		try
		{
			m_socket.Send(data, SocketFlags.None);
			return default;
		}
		catch (Exception ex)
		{
			return ValueTaskExtensions.FromException<int>(ex);
		}
	}

	private async ValueTask<int> DoWriteBytesAsync(ReadOnlyMemory<byte> data)
	{
#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
		m_socketAwaitable.EventArgs.SetBuffer(MemoryMarshal.AsMemory(data));
		await m_socket.SendAsync(m_socketAwaitable);
#else
		await m_socket.SendAsync(data, SocketFlags.None).ConfigureAwait(false);
#endif
		return 0;
	}

	private readonly Socket m_socket;
#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
	private readonly SocketAwaitable m_socketAwaitable;
#endif
	private readonly Action m_closeSocket;
}
