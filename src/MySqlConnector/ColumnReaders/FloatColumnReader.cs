using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal closed class FloatColumnReader : ColumnReader
{
	public abstract float ReadSingle(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
