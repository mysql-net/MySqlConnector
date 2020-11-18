using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Serialization
{
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
			return ioBehavior == IOBehavior.Asynchronous ? new ValueTask<int>(DoReadBytesAsync(buffer)) :
				RemainingTimeout <= 0 ? ValueTaskExtensions.FromException<int>(MySqlException.CreateForTimeout()) :
				m_stream.CanTimeout ? DoReadBytesSync(buffer) :
				DoReadBytesSyncOverAsync(buffer);

			ValueTask<int> DoReadBytesSync(Memory<byte> buffer_)
			{
				m_stream.ReadTimeout = RemainingTimeout == Constants.InfiniteTimeout ? Timeout.Infinite : RemainingTimeout;
				var startTime = RemainingTimeout == Constants.InfiniteTimeout ? 0 : Environment.TickCount;
				int bytesRead;
				try
				{
					bytesRead = m_stream.Read(buffer_);
				}
				catch (Exception ex)
				{
					if (RemainingTimeout != Constants.InfiniteTimeout && ex is IOException ioException && ioException.InnerException is SocketException socketException && socketException.SocketErrorCode == SocketError.TimedOut)
						return ValueTaskExtensions.FromException<int>(MySqlException.CreateForTimeout(ex));
					return ValueTaskExtensions.FromException<int>(ex);
				}
				if (RemainingTimeout != Constants.InfiniteTimeout)
					RemainingTimeout -= unchecked(Environment.TickCount - startTime);
				return new ValueTask<int>(bytesRead);
			}

			ValueTask<int> DoReadBytesSyncOverAsync(Memory<byte> buffer_)
			{
				try
				{
					// handle timeout by setting a timer to close the stream in the background
					return new ValueTask<int>(DoReadBytesAsync(buffer_).GetAwaiter().GetResult());
				}
				catch (Exception ex)
				{
					return ValueTaskExtensions.FromException<int>(ex);
				}
			}

			async Task<int> DoReadBytesAsync(Memory<byte> buffer_)
			{
				var startTime = RemainingTimeout == Constants.InfiniteTimeout ? 0 : Environment.TickCount;
				var timerId = RemainingTimeout == Constants.InfiniteTimeout ? 0 : TimerQueue.Instance.Add(RemainingTimeout, m_closeStream);
				int bytesRead;
				try
				{
					bytesRead = await m_stream.ReadAsync(buffer_).ConfigureAwait(false);
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

		public ValueTask<int> WriteBytesAsync(ReadOnlyMemory<byte> data, IOBehavior ioBehavior)
		{
			if (ioBehavior == IOBehavior.Asynchronous)
				return new ValueTask<int>(DoWriteBytesAsync(data));

			try
			{
				m_stream.Write(data);
				return default;
			}
			catch (Exception ex)
			{
				return ValueTaskExtensions.FromException<int>(ex);
			}

			async Task<int> DoWriteBytesAsync(ReadOnlyMemory<byte> data_)
			{
				await m_stream.WriteAsync(data_).ConfigureAwait(false);
				return 0;
			}
		}

		readonly Stream m_stream;
		readonly Action m_closeStream;
	}
}
