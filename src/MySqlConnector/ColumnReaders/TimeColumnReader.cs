using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal closed class TimeColumnReader : ColumnReader
{
	public abstract TimeSpan ReadTimeSpan(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
