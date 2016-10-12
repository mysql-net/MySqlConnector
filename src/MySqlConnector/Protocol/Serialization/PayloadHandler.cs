using System;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class PayloadHandler : IPayloadHandler
	{
		public PayloadHandler(IPacketHandler packetHandler)
		{
			m_packetHandler = packetHandler;
		}

		public void SetByteHandler(IByteHandler byteHandler)
		{
			m_packetHandler.SetByteHandler(byteHandler);
		}

		public ValueTask<ArraySegment<byte>> ReadPayloadAsync(IConversation conversation, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior) =>
			ReadPayloadAsync(default(ArraySegment<byte>), conversation, protocolErrorBehavior, ioBehavior);

		public ValueTask<int> WritePayloadAsync(IConversation conversation, ArraySegment<byte> payload, IOBehavior ioBehavior)
		{
			if (payload.Count <= MaxPacketSize)
				return m_packetHandler.WritePacketAsync(new Packet(conversation.GetNextSequenceNumber(), payload), ioBehavior);

			var writeTask = default(ValueTask<int>);
			for (var bytesSent = 0; bytesSent < payload.Count; bytesSent += MaxPacketSize)
			{
				var contents = new ArraySegment<byte>(payload.Array, payload.Offset + bytesSent, Math.Min(MaxPacketSize, payload.Count - bytesSent));
				writeTask = writeTask.ContinueWith(x => m_packetHandler.WritePacketAsync(new Packet(conversation.GetNextSequenceNumber(), contents), ioBehavior));
			}
			return writeTask;
		}

		private ValueTask<ArraySegment<byte>> ReadPayloadAsync(ArraySegment<byte> previousPayloads, IConversation conversation, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			return m_packetHandler.ReadPacketAsync(protocolErrorBehavior, ioBehavior).ContinueWith(packet =>
				ContinueRead(previousPayloads, packet, conversation, protocolErrorBehavior, ioBehavior));
		}

		private ValueTask<ArraySegment<byte>> ContinueRead(ArraySegment<byte> previousPayloads, Packet packet, IConversation conversation, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			if (packet == null && protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
				return default(ValueTask<ArraySegment<byte>>);

			var sequenceNumber = conversation.GetNextSequenceNumber() % 256;
			if (packet.SequenceNumber != sequenceNumber)
			{
				if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
					return default(ValueTask<ArraySegment<byte>>);

				var exception = new InvalidOperationException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(sequenceNumber, packet.SequenceNumber));
				return ValueTaskExtensions.FromException<ArraySegment<byte>>(exception);
			}

			var previousPayloadsArray = previousPayloads.Array;
			if (previousPayloadsArray == null && packet.Contents.Count < MaxPacketSize)
				return new ValueTask<ArraySegment<byte>>(packet.Contents);

			if (previousPayloadsArray == null)
				previousPayloadsArray = new byte[MaxPacketSize + 1];
			else if (previousPayloads.Offset + previousPayloads.Count + packet.Contents.Count > previousPayloadsArray.Length)
				Array.Resize(ref previousPayloadsArray, previousPayloadsArray.Length * 2);

			Buffer.BlockCopy(packet.Contents.Array, packet.Contents.Offset, previousPayloadsArray, previousPayloads.Offset + previousPayloads.Count, packet.Contents.Count);
			previousPayloads = new ArraySegment<byte>(previousPayloadsArray, previousPayloads.Offset, previousPayloads.Count + packet.Contents.Count);

			return packet.Contents.Count < MaxPacketSize ?
				new ValueTask<ArraySegment<byte>>(previousPayloads) :
				ReadPayloadAsync(previousPayloads, conversation, protocolErrorBehavior, ioBehavior);
		}

		public const int MaxPacketSize = 16777215;

		private readonly IPacketHandler m_packetHandler;
	}
}
