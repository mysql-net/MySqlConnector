using System;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class StandardPayloadHandler : IPayloadHandler
	{
		public StandardPayloadHandler(IByteHandler byteHandler)
		{
			ByteHandler = byteHandler;
		}

		public void StartNewConversation()
		{
			m_sequenceNumber = 0;
		}

		public IByteHandler ByteHandler
		{
			get { return m_byteHandler; }
			set
			{
				m_byteHandler = value ?? throw new ArgumentNullException(nameof(value));
				m_bufferedByteReader = new BufferedByteReader();
			}
		}

		public ValueTask<ArraySegment<byte>> ReadPayloadAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior) =>
			ProtocolUtility.ReadPayloadAsync(m_bufferedByteReader, m_byteHandler, () => GetNextSequenceNumber(), default(ArraySegment<byte>), protocolErrorBehavior, ioBehavior);

		public ValueTask<int> WritePayloadAsync(ArraySegment<byte> payload, IOBehavior ioBehavior) =>
			ProtocolUtility.WritePayloadAsync(m_byteHandler, GetNextSequenceNumber, payload, ioBehavior);

		private int GetNextSequenceNumber() => m_sequenceNumber++;

		IByteHandler m_byteHandler;
		BufferedByteReader m_bufferedByteReader;
		int m_sequenceNumber;
	}
}
