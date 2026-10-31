using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal abstract class GuidColumnReader : ColumnReader
{
	public abstract Guid ReadGuid(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
