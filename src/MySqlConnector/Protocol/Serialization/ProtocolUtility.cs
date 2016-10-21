using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
    internal static class ProtocolUtility
    {
		public static ValueTask<Packet> ReadPacketAsync(BufferedByteReader bufferedByteReader, IByteHandler byteHandler, Func<int> getNextSequenceNumber, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			return bufferedByteReader.ReadBytesAsync(byteHandler, 4, ioBehavior)
				.ContinueWith(headerBytes =>
				{
					if (headerBytes.Count < 4)
					{
						return protocolErrorBehavior == ProtocolErrorBehavior.Throw ?
							ValueTaskExtensions.FromException<Packet>(new EndOfStreamException()) :
							default(ValueTask<Packet>);
					}

					var payloadLength = (int) SerializationUtility.ReadUInt32(headerBytes.Array, headerBytes.Offset, 3);
					int packetSequenceNumber = headerBytes.Array[headerBytes.Offset + 3];

					var expectedSequenceNumber = getNextSequenceNumber() % 256;
					if (packetSequenceNumber != expectedSequenceNumber)
					{
						if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
							return default(ValueTask<Packet>);

						var exception = new InvalidOperationException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(expectedSequenceNumber, packetSequenceNumber));
						return ValueTaskExtensions.FromException<Packet>(exception);
					}

					return bufferedByteReader.ReadBytesAsync(byteHandler, payloadLength, ioBehavior)
						.ContinueWith(payloadBytes =>
						{
							if (payloadBytes.Count < payloadLength)
							{
								return protocolErrorBehavior == ProtocolErrorBehavior.Throw ?
									ValueTaskExtensions.FromException<Packet>(new EndOfStreamException()) :
									default(ValueTask<Packet>);
							}

							return new ValueTask<Packet>(new Packet(packetSequenceNumber, payloadBytes));
						});
				});
		}

		public static ValueTask<ArraySegment<byte>> ReadPayloadAsync(BufferedByteReader bufferedByteReader, IByteHandler byteHandler, Func<int> getNextSequenceNumber, ArraySegment<byte> previousPayloads, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			return ReadPacketAsync(bufferedByteReader, byteHandler, getNextSequenceNumber, protocolErrorBehavior, ioBehavior).ContinueWith(packet =>
				ContinueRead(bufferedByteReader, byteHandler, getNextSequenceNumber, previousPayloads, packet, protocolErrorBehavior, ioBehavior));
		}

		private static ValueTask<ArraySegment<byte>> ContinueRead(BufferedByteReader bufferedByteReader, IByteHandler byteHandler, Func<int> getNextSequenceNumber, ArraySegment<byte> previousPayloads, Packet packet, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			if (packet == null && protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
				return default(ValueTask<ArraySegment<byte>>);

			var previousPayloadsArray = previousPayloads.Array;
			if (previousPayloadsArray == null && packet.Contents.Count < MaxPacketSize)
				return new ValueTask<ArraySegment<byte>>(packet.Contents);

			if (previousPayloadsArray == null)
				previousPayloadsArray = new byte[ProtocolUtility.MaxPacketSize + 1];
			else if (previousPayloads.Offset + previousPayloads.Count + packet.Contents.Count > previousPayloadsArray.Length)
				Array.Resize(ref previousPayloadsArray, previousPayloadsArray.Length * 2);

			Buffer.BlockCopy(packet.Contents.Array, packet.Contents.Offset, previousPayloadsArray, previousPayloads.Offset + previousPayloads.Count, packet.Contents.Count);
			previousPayloads = new ArraySegment<byte>(previousPayloadsArray, previousPayloads.Offset, previousPayloads.Count + packet.Contents.Count);

			return packet.Contents.Count < ProtocolUtility.MaxPacketSize ?
				new ValueTask<ArraySegment<byte>>(previousPayloads) :
				ReadPayloadAsync(bufferedByteReader, byteHandler, getNextSequenceNumber, previousPayloads, protocolErrorBehavior, ioBehavior);
		}

		public static ValueTask<int> WritePayloadAsync(IByteHandler byteHandler, Func<int> getNextSequenceNumber, ArraySegment<byte> payload, IOBehavior ioBehavior)
		{
			if (payload.Count <= MaxPacketSize)
				return WritePacketAsync(byteHandler, getNextSequenceNumber(), payload, ioBehavior);

			var writeTask = default(ValueTask<int>);
			for (var bytesSent = 0; bytesSent < payload.Count; bytesSent += MaxPacketSize)
			{
				var contents = new ArraySegment<byte>(payload.Array, payload.Offset + bytesSent, Math.Min(MaxPacketSize, payload.Count - bytesSent));
				writeTask = writeTask.ContinueWith(x => WritePacketAsync(byteHandler, getNextSequenceNumber(), contents, ioBehavior));
			}
			return writeTask;
		}

		public static ValueTask<int> WritePacketAsync(IByteHandler byteHandler, int sequenceNumber, ArraySegment<byte> contents, IOBehavior ioBehavior)
		{
			var bufferLength = contents.Count + 4;
			var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
			SerializationUtility.WriteUInt32((uint) contents.Count, buffer, 0, 3);
			buffer[3] = (byte) sequenceNumber;
			Buffer.BlockCopy(contents.Array, contents.Offset, buffer, 4, contents.Count);
			return byteHandler.WriteBytesAsync(new ArraySegment<byte>(buffer, 0, bufferLength), ioBehavior)
				.ContinueWith(x =>
				{
					ArrayPool<byte>.Shared.Return(buffer);
					return default(ValueTask<int>);
				});
		}

	    public const int MaxPacketSize = 16777215;
    }
}
