using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal closed class DateTimeColumnReader : ColumnReader
{
	public abstract DateTime ReadDateTime(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);

	public abstract MySqlDateTime ReadMySqlDateTime(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
