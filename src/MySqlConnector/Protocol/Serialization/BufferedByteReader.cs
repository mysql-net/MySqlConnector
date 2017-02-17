using System;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class BufferedByteReader
	{
		public BufferedByteReader()
		{
			m_buffer = new byte[16384];
		}

		public ValueTask<ArraySegment<byte>> ReadBytesAsync(IByteHandler byteHandler, int count, IOBehavior ioBehavior)
		{
			if (m_remainingData.Count >= count)
			{
				var readBytes = m_remainingData.Slice(0, count);
				m_remainingData = m_remainingData.Slice(count);
				return new ValueTask<ArraySegment<byte>>(readBytes);
			}

			var buffer = count > m_buffer.Length ? new byte[count] : m_buffer;
			if (m_remainingData.Count > 0)
			{
				Buffer.BlockCopy(m_remainingData.Array, m_remainingData.Offset, buffer, 0, m_remainingData.Count);
				m_remainingData = new ArraySegment<byte>(buffer, 0, m_remainingData.Count);
			}

			return ReadBytesAsync(byteHandler, new ArraySegment<byte>(buffer, m_remainingData.Count, buffer.Length - m_remainingData.Count), count, ioBehavior);
		}

		private ValueTask<ArraySegment<byte>> ReadBytesAsync(IByteHandler byteHandler, ArraySegment<byte> buffer, int count, IOBehavior ioBehavior)
		{
			return byteHandler.ReadBytesAsync(buffer, ioBehavior)
				.ContinueWith(readBytesCount =>
				{
					if (readBytesCount == 0)
					{
						var data = m_remainingData;
						m_remainingData = default(ArraySegment<byte>);
						return new ValueTask<ArraySegment<byte>>(data);
					}

					var bufferSize = buffer.Offset + readBytesCount;
					if (bufferSize >= count)
					{
						var bufferBytes = new ArraySegment<byte>(buffer.Array, 0, bufferSize);
						var requestedBytes = bufferBytes.Slice(0, count);
						m_remainingData = bufferBytes.Slice(count);
						return new ValueTask<ArraySegment<byte>>(requestedBytes);
					}

					return ReadBytesAsync(byteHandler, new ArraySegment<byte>(buffer.Array, bufferSize, buffer.Array.Length - bufferSize), count, ioBehavior);
				});
		}

		ArraySegment<byte> m_remainingData;
		readonly byte[] m_buffer;
	}
}
