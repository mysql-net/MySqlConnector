using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Serialization;

internal sealed class BufferedByteReader
{
	public BufferedByteReader() => m_buffer = new byte[16384];

	public ValueTask<ArraySegment<byte>> ReadBytesAsync(IByteHandler byteHandler, int count, IOBehavior ioBehavior)
	{
		// check if read can be satisfied from the buffer
		if (m_remainingData.Count >= count)
		{
			var readBytes = m_remainingData.Slice(0, count);
			m_remainingData = m_remainingData.Slice(count);
			return new ValueTask<ArraySegment<byte>>(readBytes);
		}

		// get a buffer big enough to hold all the data, and move any buffered data to the beginning
		var buffer = m_buffer;
		if (count > buffer.Length)
		{
			// reuse the buffer from a previous large read (if it's big enough) instead of allocating a new (possibly LOH) array for every large packet
			if (m_largeBuffer is null || m_largeBuffer.Length < count)
				m_largeBuffer = new byte[count];
			buffer = m_largeBuffer;
		}
		if (m_remainingData.Count > 0)
		{
			Buffer.BlockCopy(m_remainingData.Array!, m_remainingData.Offset, buffer, 0, m_remainingData.Count);
			m_remainingData = new(buffer, 0, m_remainingData.Count);
		}

		return ReadBytesAsync(byteHandler, new(buffer, m_remainingData.Count, buffer.Length - m_remainingData.Count), count, ioBehavior);
	}

	// Stops reusing the buffer allocated for large reads, so that it isn't kept alive while the session is idle in the pool.
	public void ReleaseLargeBuffer() => m_largeBuffer = null;

	private async ValueTask<ArraySegment<byte>> ReadBytesAsync(IByteHandler byteHandler, ArraySegment<byte> buffer, int totalBytesToRead, IOBehavior ioBehavior)
	{
		while (true)
		{
			var readBytesCount = await byteHandler.ReadBytesAsync(buffer, ioBehavior).ConfigureAwait(false);
			if (readBytesCount == 0)
			{
				var data = m_remainingData;
				m_remainingData = default;
				return data;
			}

			var bufferSize = buffer.Offset + readBytesCount;
			if (bufferSize >= totalBytesToRead)
			{
				var bufferBytes = new ArraySegment<byte>(buffer.Array!, 0, bufferSize);
				var requestedBytes = bufferBytes.Slice(0, totalBytesToRead);
				m_remainingData = bufferBytes.Slice(totalBytesToRead);
				return requestedBytes;
			}

			buffer = buffer.Slice(readBytesCount);
		}
	}

	private readonly byte[] m_buffer;
	private byte[]? m_largeBuffer;
	private ArraySegment<byte> m_remainingData;
}
