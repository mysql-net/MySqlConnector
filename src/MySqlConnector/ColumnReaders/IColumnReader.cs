namespace MySqlConnector.ColumnReaders;
using MySqlConnector.Protocol.Payloads;

internal interface IColumnReader
{
	object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);

	int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);
}
