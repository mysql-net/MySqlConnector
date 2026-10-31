using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal abstract class TimeColumnReader : ColumnReader
{
	public abstract TimeSpan ReadTimeSpan(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
