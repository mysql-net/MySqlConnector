using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal closed class DoubleColumnReader : ColumnReader
{
	public abstract double ReadDouble(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
