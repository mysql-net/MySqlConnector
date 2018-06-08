using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
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

		public ValueTask<int> ReadBytesAsync(ArraySegment<byte> buffer, IOBehavior ioBehavior)
		{
			return ioBehavior == IOBehavior.Asynchronous ? new ValueTask<int>(DoReadBytesAsync(buffer)) :
				RemainingTimeout <= 0 ? ValueTaskExtensions.FromException<int>(MySqlException.CreateForTimeout()) :
				m_stream.CanTimeout ? DoReadBytesSync(buffer) :
				DoReadBytesSyncOverAsync(buffer);

			ValueTask<int> DoReadBytesSync(ArraySegment<byte> buffer_)
			{
				m_stream.ReadTimeout = RemainingTimeout == Constants.InfiniteTimeout ? Timeout.Infinite : RemainingTimeout;
				var startTime = RemainingTimeout == Constants.InfiniteTimeout ? 0 : Environment.TickCount;
				int bytesRead;
				try
				{
					bytesRead = m_stream.Read(buffer_.Array, buffer_.Offset, buffer_.Count);
				}
				catch (Exception ex)
				{
					if (ex is IOException && RemainingTimeout != Constants.InfiniteTimeout)
						return ValueTaskExtensions.FromException<int>(MySqlException.CreateForTimeout(ex));
					return ValueTaskExtensions.FromException<int>(ex);
				}
				if (RemainingTimeout != Constants.InfiniteTimeout)
					RemainingTimeout -= unchecked(Environment.TickCount - startTime);
				return new ValueTask<int>(bytesRead);
			}

			ValueTask<int> DoReadBytesSyncOverAsync(ArraySegment<byte> buffer_)
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

			async Task<int> DoReadBytesAsync(ArraySegment<byte> buffer_)
			{
				var startTime = RemainingTimeout == Constants.InfiniteTimeout ? 0 : Environment.TickCount;
				var timerId = RemainingTimeout == Constants.InfiniteTimeout ? 0 : TimerQueue.Instance.Add(RemainingTimeout, m_closeStream);
				int bytesRead;
				try
				{
					bytesRead = await m_stream.ReadAsync(buffer_.Array, buffer_.Offset, buffer_.Count).ConfigureAwait(false);
				}
				catch (Exception ex) when (ex is ObjectDisposedException || ex is IOException)
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

		public ValueTask<int> WriteBytesAsync(ArraySegment<byte> data, IOBehavior ioBehavior)
		{
			if (ioBehavior == IOBehavior.Asynchronous)
				return new ValueTask<int>(DoWriteBytesAsync(data));

			try
			{
				m_stream.Write(data.Array, data.Offset, data.Count);
				return default;
			}
			catch (Exception ex)
			{
				return ValueTaskExtensions.FromException<int>(ex);
			}

			async Task<int> DoWriteBytesAsync(ArraySegment<byte> data_)
			{
				await m_stream.WriteAsync(data_.Array, data_.Offset, data_.Count).ConfigureAwait(false);
				return 0;
			}
		}

		readonly Stream m_stream;
		readonly Action m_closeStream;
	}
}
