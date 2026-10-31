using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal closed class GuidColumnReader : ColumnReader
{
	public abstract Guid ReadGuid(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
