using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Serialization;

internal sealed class StandardPayloadHandler : IPayloadHandler
{
	public StandardPayloadHandler(IByteHandler byteHandler)
	{
		ByteHandler = byteHandler;
		m_getNextSequenceNumber = () => m_sequenceNumber++;
	}

	public void Dispose()
	{
		m_bufferedByteReader = null;
		Utility.Dispose(ref m_byteHandler);
	}

	public void StartNewConversation() =>
		m_sequenceNumber = 0;

	public void SetNextSequenceNumber(int sequenceNumber) =>
		m_sequenceNumber = (byte) sequenceNumber;

	public IByteHandler ByteHandler
	{
		get
		{
			if (m_byteHandler is null)
				throw new ObjectDisposedException(nameof(StandardPayloadHandler));
			return m_byteHandler;
		}
		set
		{
			m_byteHandler = value ?? throw new ArgumentNullException(nameof(value));
			m_bufferedByteReader = new();
		}
	}

	public ValueTask<ArraySegment<byte>> ReadPayloadAsync(ArraySegmentHolder<byte> cache, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior) =>
		ProtocolUtility.ReadPayloadAsync(m_bufferedByteReader!, m_byteHandler!, m_getNextSequenceNumber, cache, protocolErrorBehavior, ioBehavior);

	public ValueTask<int> WritePayloadAsync(ReadOnlyMemory<byte> payload, IOBehavior ioBehavior) =>
		ProtocolUtility.WritePayloadAsync(m_byteHandler!, m_getNextSequenceNumber, payload, ioBehavior);

	private readonly Func<int> m_getNextSequenceNumber;
	private IByteHandler? m_byteHandler;
	private BufferedByteReader? m_bufferedByteReader;
	private byte m_sequenceNumber;
}
