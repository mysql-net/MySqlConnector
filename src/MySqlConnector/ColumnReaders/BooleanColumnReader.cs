using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal closed class BooleanColumnReader : ColumnReader
{
	public abstract bool ReadBoolean(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
