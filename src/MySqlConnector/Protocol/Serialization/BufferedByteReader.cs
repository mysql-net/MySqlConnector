using System;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class BufferedByteReader
	{
		public ValueTask<ArraySegment<byte>> ReadBytesAsync(IByteHandler byteHandler, int count, IOBehavior ioBehavior)
		{
			if (m_remainingData.Count >= count)
			{
				var readBytes = m_remainingData.Slice(0, count);
				m_remainingData = m_remainingData.Slice(count);
				return new ValueTask<ArraySegment<byte>>(readBytes);
			}

			if (m_remainingData.Count == 0)
				return ReadBytesAsync(byteHandler, default(ArraySegment<byte>), count, ioBehavior);

			// save data from m_remainingData.Array because calling ReadAsync may invalidate it
			var buffer = new byte[Math.Max(count, 16384)];
			Buffer.BlockCopy(m_remainingData.Array, m_remainingData.Offset, buffer, 0, m_remainingData.Count);
			var previousReadBytes = new ArraySegment<byte>(buffer, 0, m_remainingData.Count);

			return ReadBytesAsync(byteHandler, previousReadBytes, count, ioBehavior);
		}

		private ValueTask<ArraySegment<byte>> ReadBytesAsync(IByteHandler byteHandler, ArraySegment<byte> previousReadBytes, int count, IOBehavior ioBehavior)
		{
			return byteHandler.ReadBytesAsync(count - previousReadBytes.Count, ioBehavior)
				.ContinueWith(readBytes =>
				{
					if (readBytes.Count == 0)
						return new ValueTask<ArraySegment<byte>>(previousReadBytes);

					if (previousReadBytes.Array == null && readBytes.Count >= count)
					{
						m_remainingData = readBytes.Slice(count);
						return new ValueTask<ArraySegment<byte>>(readBytes.Slice(0, count));
					}

					var previousReadBytesArray = previousReadBytes.Array;
					if (previousReadBytesArray == null)
						previousReadBytesArray = new byte[Math.Max(count, 16384)];
					else if (previousReadBytesArray.Length < previousReadBytes.Count + readBytes.Count)
						Array.Resize(ref previousReadBytesArray, Math.Max(previousReadBytesArray.Length * 2, previousReadBytes.Count + readBytes.Count));

					Buffer.BlockCopy(readBytes.Array, readBytes.Offset, previousReadBytesArray, previousReadBytes.Offset + previousReadBytes.Count, readBytes.Count);
					previousReadBytes = new ArraySegment<byte>(previousReadBytesArray, previousReadBytes.Offset, previousReadBytes.Count + readBytes.Count);

					if (previousReadBytes.Count >= count)
					{
						m_remainingData = previousReadBytes.Slice(count);
						return new ValueTask<ArraySegment<byte>>(previousReadBytes.Slice(0, count));
					}

					return ReadBytesAsync(byteHandler, previousReadBytes, count, ioBehavior);
				});
		}

		ArraySegment<byte> m_remainingData;
	}
}
