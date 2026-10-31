using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal closed class SignedIntegerColumnReader : ColumnReader
{
	public abstract long ReadInt64(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
