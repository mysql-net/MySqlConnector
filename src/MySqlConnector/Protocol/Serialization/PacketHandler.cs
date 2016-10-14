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
			return m_protocolLayer.ReadAsync(4, protocolErrorBehavior, ioBehavior)
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

					return m_protocolLayer.ReadAsync(payloadLength, protocolErrorBehavior, ioBehavior)
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

		private readonly IProtocolLayer m_protocolLayer;
	}
}
