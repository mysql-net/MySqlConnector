using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class PacketHandler
	{
		public PacketHandler(IProtocolLayer protocolLayer)
		{
			m_protocolLayer = protocolLayer;
		}

		public ValueTask<Packet> ReadPacketAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			return ReadBytesAsync(4, ioBehavior)
				.ContinueWith(headerBytes =>
				{
					if (headerBytes.Count < 4)
					{
						return protocolErrorBehavior == ProtocolErrorBehavior.Throw ?
							ValueTaskExtensions.FromException<Packet>(new EndOfStreamException()) :
							default(ValueTask<Packet>);
					}

					var payloadLength = (int) SerializationUtility.ReadUInt32(headerBytes.Array, headerBytes.Offset, 3);
					int sequenceNumber = headerBytes.Array[headerBytes.Offset + 3];

					return ReadBytesAsync(payloadLength, ioBehavior)
						.ContinueWith(payloadBytes =>
						{
							if (payloadBytes.Count < payloadLength)
							{
								return protocolErrorBehavior == ProtocolErrorBehavior.Throw ?
									ValueTaskExtensions.FromException<Packet>(new EndOfStreamException()) :
									default(ValueTask<Packet>);
							}

							return new ValueTask<Packet>(new Packet(sequenceNumber, payloadBytes));
						});
				});
		}

		public ValueTask<int> WritePacketAsync(int sequenceNumber, ArraySegment<byte> contents, IOBehavior ioBehavior)
		{
			var bufferLength = contents.Count + 4;
			var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
			SerializationUtility.WriteUInt32((uint) contents.Count, buffer, 0, 3);
			buffer[3] = (byte) sequenceNumber;
			Buffer.BlockCopy(contents.Array, contents.Offset, buffer, 4, contents.Count);
			return m_protocolLayer.WriteAsync(new ArraySegment<byte>(buffer, 0, bufferLength), ioBehavior)
				.ContinueWith(x =>
				{
					ArrayPool<byte>.Shared.Return(buffer);
					return default(ValueTask<int>);
				});
		}

		private ValueTask<ArraySegment<byte>> ReadBytesAsync(int count, IOBehavior ioBehavior)
		{
			if (m_remainingData.Count >= count)
			{
				var readBytes = new ArraySegment<byte>(m_remainingData.Array, m_remainingData.Offset, count);
				m_remainingData = new ArraySegment<byte>(m_remainingData.Array, m_remainingData.Offset + count, m_remainingData.Count - count);
				return new ValueTask<ArraySegment<byte>>(readBytes);
			}

			// save data from m_remainingData.Array because calling ReadAsync may invalidate it
			var buffer = new byte[Math.Max(count, 16384)];
			if (m_remainingData.Count > 0)
				Buffer.BlockCopy(m_remainingData.Array, m_remainingData.Offset, buffer, 0, m_remainingData.Count);
			var previousReadBytes = new ArraySegment<byte>(buffer, 0, m_remainingData.Count);

			return ReadBytesAsync(previousReadBytes, count, ioBehavior);
		}

		private ValueTask<ArraySegment<byte>> ReadBytesAsync(ArraySegment<byte> previousReadBytes, int count, IOBehavior ioBehavior)
		{
			return m_protocolLayer.ReadAsync(count - previousReadBytes.Count, ProtocolErrorBehavior.Throw, ioBehavior)
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

					return ReadBytesAsync(previousReadBytes, count, ioBehavior);
				});
		}

		private readonly IProtocolLayer m_protocolLayer;
		private ArraySegment<byte> m_remainingData;
	}
}
