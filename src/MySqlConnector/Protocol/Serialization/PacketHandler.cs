using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class PacketHandler : IPacketHandler
	{
		public PacketHandler(IByteHandler byteHandler)
		{
			m_byteHandler = byteHandler;
			m_buffer = new byte[16384];
		}

		public ValueTask<Packet> ReadPacketAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			return ReadBytesAsync(0, m_buffer, 0, 4, ioBehavior)
				.ContinueWith(headerBytesRead =>
				{
					if (headerBytesRead < 4)
					{
						return protocolErrorBehavior == ProtocolErrorBehavior.Throw ?
							ValueTaskExtensions.FromException<Packet>(new EndOfStreamException()) :
							default(ValueTask<Packet>);
					}

					var payloadLength = (int) SerializationUtility.ReadUInt32(m_buffer, 0, 3);
					int sequenceNumber = m_buffer[3];

					var buffer = payloadLength <= m_buffer.Length ? m_buffer : new byte[payloadLength];
					return ReadBytesAsync(0, buffer, 0, payloadLength, ioBehavior)
						.ContinueWith(payloadBytesRead =>
						{
							if (payloadBytesRead < payloadLength)
							{
								return protocolErrorBehavior == ProtocolErrorBehavior.Throw ?
									ValueTaskExtensions.FromException<Packet>(new EndOfStreamException()) :
									default(ValueTask<Packet>);
							}

							return new ValueTask<Packet>(new Packet(sequenceNumber, new ArraySegment<byte>(buffer, 0, payloadLength)));
						});
				});
		}

		public ValueTask<int> WritePacketAsync(Packet packet, IOBehavior ioBehavior)
		{
			var packetLength = packet.Contents.Count;
			var bufferLength = packetLength + 4;
			var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
			SerializationUtility.WriteUInt32((uint) packetLength, buffer, 0, 3);
			buffer[3] = (byte) packet.SequenceNumber;
			Buffer.BlockCopy(packet.Contents.Array, packet.Contents.Offset, buffer, 4, packetLength);
			return m_byteHandler.WriteBytesAsync(new ArraySegment<byte>(buffer, 0, bufferLength), ioBehavior)
				.ContinueWith(x =>
				{
					ArrayPool<byte>.Shared.Return(buffer);
					return default(ValueTask<int>);
				});
		}

		private ValueTask<int> ReadBytesAsync(int previousBytesRead, byte[] buffer, int offset, int count, IOBehavior ioBehavior)
		{
			return m_byteHandler.ReadBytesAsync(buffer, offset, count, ioBehavior)
				.ContinueWith(bytesRead =>
				{
					if (bytesRead == 0 || bytesRead == count)
						return new ValueTask<int>(previousBytesRead + bytesRead);

					return ReadBytesAsync(previousBytesRead + bytesRead, buffer, offset + bytesRead, count - bytesRead, ioBehavior);
				});
		}

		private readonly IByteHandler m_byteHandler;
		private readonly byte[] m_buffer;
	}
}
