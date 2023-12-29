using System.Net.Sockets;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Serialization;

internal sealed class StreamByteHandler : IByteHandler
{
	public StreamByteHandler(Stream stream)
	{
		m_stream = stream;
		m_closeStream = m_stream.Dispose;
		RemainingTimeout = Constants.InfiniteTimeout;
	}

	public void Dispose() => m_stream.Dispose();

	public int RemainingTimeout { get; set; }

	public ValueTask<int> ReadBytesAsync(Memory<byte> buffer, IOBehavior ioBehavior)
	{
		return
			RemainingTimeout <= 0 ? ValueTaskExtensions.FromException<int>(MySqlException.CreateForTimeout()) :
			ioBehavior == IOBehavior.Asynchronous ? new ValueTask<int>(DoReadBytesAsync(buffer)) :
			m_stream.CanTimeout ? DoReadBytesSync(buffer) :
			DoReadBytesSyncOverAsync(buffer);

		ValueTask<int> DoReadBytesSync(Memory<byte> buffer)
		{
			m_stream.ReadTimeout = RemainingTimeout == Constants.InfiniteTimeout ? Timeout.Infinite : RemainingTimeout;
			var startTime = RemainingTimeout == Constants.InfiniteTimeout ? 0 : Environment.TickCount;
			int bytesRead;
			try
			{
				bytesRead = m_stream.Read(buffer);
			}
			catch (Exception ex)
			{
				if (RemainingTimeout != Constants.InfiniteTimeout && ex is IOException { InnerException: SocketException { SocketErrorCode: SocketError.TimedOut } })
					return ValueTaskExtensions.FromException<int>(MySqlException.CreateForTimeout(ex));
				return ValueTaskExtensions.FromException<int>(ex);
			}
			if (RemainingTimeout != Constants.InfiniteTimeout)
				RemainingTimeout -= unchecked(Environment.TickCount - startTime);
			return new ValueTask<int>(bytesRead);
		}

		ValueTask<int> DoReadBytesSyncOverAsync(Memory<byte> buffer)
		{
			try
			{
				// handle timeout by setting a timer to close the stream in the background
				return new ValueTask<int>(DoReadBytesAsync(buffer).GetAwaiter().GetResult());
			}
			catch (Exception ex)
			{
				return ValueTaskExtensions.FromException<int>(ex);
			}
		}

		async Task<int> DoReadBytesAsync(Memory<byte> buffer)
		{
			var startTime = RemainingTimeout == Constants.InfiniteTimeout ? 0 : Environment.TickCount;
			var timerId = RemainingTimeout == Constants.InfiniteTimeout ? 0 : TimerQueue.Instance.Add(RemainingTimeout, m_closeStream);
			int bytesRead;
			try
			{
				bytesRead = await m_stream.ReadAsync(buffer).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is ObjectDisposedException or IOException)
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
	}

	public ValueTask WriteBytesAsync(ReadOnlyMemory<byte> data, IOBehavior ioBehavior)
	{
		if (ioBehavior == IOBehavior.Asynchronous)
			return DoWriteBytesAsync(data);

		try
		{
			m_stream.Write(data);
			return default;
		}
		catch (Exception ex)
		{
			return ValueTaskExtensions.FromException(ex);
		}

		async ValueTask DoWriteBytesAsync(ReadOnlyMemory<byte> data) =>
			await m_stream.WriteAsync(data).ConfigureAwait(false);
	}

	private readonly Stream m_stream;
	private readonly Action m_closeStream;
}
