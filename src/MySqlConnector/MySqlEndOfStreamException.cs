namespace MySqlConnector;

[Serializable]
public sealed class MySqlEndOfStreamException : EndOfStreamException
{
	internal MySqlEndOfStreamException(int expectedByteCount, int readByteCount)
		: base("An incomplete response was received from the server")
	{
		ExpectedByteCount = expectedByteCount;
		ReadByteCount = readByteCount;
	}

	public int ExpectedByteCount { get; }
	public int ReadByteCount { get; }
}
