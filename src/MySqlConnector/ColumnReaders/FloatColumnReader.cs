using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal abstract class FloatColumnReader : ColumnReader
{
	public abstract float ReadSingle(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
