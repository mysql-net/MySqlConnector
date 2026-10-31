using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal abstract class DoubleColumnReader : ColumnReader
{
	public abstract double ReadDouble(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
