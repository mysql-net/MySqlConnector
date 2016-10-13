using System;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class PayloadProtocolLayer : BaseProtocolLayer
	{
		public PayloadProtocolLayer(IProtocolLayer nextLayer)
			: base(nextLayer)
		{
			m_packetHandler = new PacketHandler(nextLayer);
		}

		public override ValueTask<ArraySegment<byte>> ReadAsync(int? count, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			if (count.HasValue)
				throw new ArgumentException("count must not be specified for PayloadProtocolLayer.ReadAsync", nameof(count));

			return ReadPayloadAsync(default(ArraySegment<byte>), protocolErrorBehavior, ioBehavior);
		}

		public override ValueTask<int> WriteAsync(ArraySegment<byte> data, IOBehavior ioBehavior)
		{
			if (data.Count <= MaxPacketSize)
			{
				return m_packetHandler.WritePacketAsync(GetNextSequenceNumber(), data, ioBehavior)
					.ContinueWith(_ => NextLayer.FlushAsync(ioBehavior));
			}

			var writeTask = default(ValueTask<int>);
			for (var bytesSent = 0; bytesSent < data.Count; bytesSent += MaxPacketSize)
			{
				var contents = new ArraySegment<byte>(data.Array, data.Offset + bytesSent, Math.Min(MaxPacketSize, data.Count - bytesSent));
				writeTask = writeTask.ContinueWith(x => m_packetHandler.WritePacketAsync(GetNextSequenceNumber(), contents, ioBehavior));
			}
			return writeTask.ContinueWith(_ => NextLayer.FlushAsync(ioBehavior));
		}

		public override ValueTask<int> FlushAsync(IOBehavior ioBehavior)
		{
			throw new NotSupportedException("PayloadProtocolLayer doesn't support FlushAsync.");
		}

		protected override void OnStartNewConveration()
		{
			m_sequenceNumber = 0;
			base.OnStartNewConveration();
		}

		protected override void OnNextLayerChanged()
		{
			m_packetHandler = new PacketHandler(NextLayer);
			base.OnNextLayerChanged();
		}

		private ValueTask<ArraySegment<byte>> ReadPayloadAsync(ArraySegment<byte> previousPayloads, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			return m_packetHandler.ReadPacketAsync(protocolErrorBehavior, ioBehavior).ContinueWith(packet =>
				ContinueRead(previousPayloads, packet, protocolErrorBehavior, ioBehavior));
		}

		private ValueTask<ArraySegment<byte>> ContinueRead(ArraySegment<byte> previousPayloads, Packet packet, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			if (packet == null && protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
				return default(ValueTask<ArraySegment<byte>>);

			var sequenceNumber = GetNextSequenceNumber() % 256;
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
				ReadPayloadAsync(previousPayloads, protocolErrorBehavior, ioBehavior);
		}

		private int GetNextSequenceNumber() => m_sequenceNumber++;

		public const int MaxPacketSize = 16777215;

		private PacketHandler m_packetHandler;
		private int m_sequenceNumber;
	}
}
