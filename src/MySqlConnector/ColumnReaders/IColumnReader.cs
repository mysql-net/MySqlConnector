using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal interface IColumnReader
{
	object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);

	int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
