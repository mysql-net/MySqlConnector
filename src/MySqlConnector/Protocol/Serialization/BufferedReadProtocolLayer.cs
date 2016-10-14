using System;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	/// <summary>
	/// <see cref="BufferedReadProtocolLayer"/> implements <see cref="IProtocolLayer"/> and provides buffering on calls to <see cref="ReadAsync"/>.
	/// It will read from <see cref="IProtocolLayer.NextLayer"/> as many times as necessary to return the requested amount of data. If extra
	/// data is read, it will be buffered and the next call to <see cref="ReadAsync"/> will return it immediately from the buffer.
	/// </summary>
	internal sealed class BufferedReadProtocolLayer : BaseProtocolLayer
	{
		public BufferedReadProtocolLayer(IProtocolLayer nextLayer)
			: base(nextLayer)
		{
		}

		public override ValueTask<ArraySegment<byte>> ReadAsync(int? count, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			if (!count.HasValue)
				throw new ArgumentException("count must be specified for BufferedReadProtocolLayer.ReadAsync", nameof(count));

			if (m_remainingData.Count >= count.Value)
			{
				var readBytes = new ArraySegment<byte>(m_remainingData.Array, m_remainingData.Offset, count.Value);
				m_remainingData = new ArraySegment<byte>(m_remainingData.Array, m_remainingData.Offset + count.Value, m_remainingData.Count - count.Value);
				return new ValueTask<ArraySegment<byte>>(readBytes);
			}

			// save data from m_remainingData.Array because calling ReadAsync may invalidate it
			var buffer = new byte[Math.Max(count.Value, 16384)];
			if (m_remainingData.Count > 0)
				Buffer.BlockCopy(m_remainingData.Array, m_remainingData.Offset, buffer, 0, m_remainingData.Count);
			var previousReadBytes = new ArraySegment<byte>(buffer, 0, m_remainingData.Count);

			return ReadBytesAsync(previousReadBytes, count.Value, protocolErrorBehavior, ioBehavior);
		}


		public override ValueTask<int> WriteAsync(ArraySegment<byte> data, IOBehavior ioBehavior) =>
			NextLayer.WriteAsync(data, ioBehavior);

		public override ValueTask<int> FlushAsync(IOBehavior ioBehavior) =>
			NextLayer.FlushAsync(ioBehavior);

		private ValueTask<ArraySegment<byte>> ReadBytesAsync(ArraySegment<byte> previousReadBytes, int count, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			return NextLayer.ReadAsync(count - previousReadBytes.Count, protocolErrorBehavior, ioBehavior)
				.ContinueWith(readBytes =>
				{
					if (readBytes.Count == 0)
						return new ValueTask<ArraySegment<byte>>(previousReadBytes);

					var previousReadBytesArray = previousReadBytes.Array;
					if (previousReadBytesArray.Length < previousReadBytes.Count + readBytes.Count)
						Array.Resize(ref previousReadBytesArray, Math.Max(previousReadBytesArray.Length * 2, previousReadBytes.Count + readBytes.Count));

					Buffer.BlockCopy(readBytes.Array, readBytes.Offset, previousReadBytesArray, previousReadBytes.Offset + previousReadBytes.Count, readBytes.Count);
					previousReadBytes = new ArraySegment<byte>(previousReadBytesArray, previousReadBytes.Offset, previousReadBytes.Count + readBytes.Count);

					if (previousReadBytes.Count >= count)
					{
						m_remainingData = new ArraySegment<byte>(previousReadBytes.Array, previousReadBytes.Offset + count, previousReadBytes.Count - count);
						return new ValueTask<ArraySegment<byte>>(new ArraySegment<byte>(previousReadBytes.Array, previousReadBytes.Offset, count));
					}

					return ReadBytesAsync(previousReadBytes, count, protocolErrorBehavior, ioBehavior);
				});
		}

		ArraySegment<byte> m_remainingData;
	}
}
