using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal abstract class SignedIntegerColumnReader : ColumnReader
{
	public abstract long ReadInt64(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
