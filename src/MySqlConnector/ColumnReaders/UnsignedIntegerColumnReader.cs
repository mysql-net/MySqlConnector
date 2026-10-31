using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal closed class UnsignedIntegerColumnReader : ColumnReader
{
	public abstract ulong ReadUInt64(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
