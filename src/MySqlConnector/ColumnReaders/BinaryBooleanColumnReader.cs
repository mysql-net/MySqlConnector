using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryBooleanColumnReader : IColumnReader
{
	public static BinaryBooleanColumnReader Instance { get; } = new();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return data[0] != 0;
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return data[0] != 0 ? 1 : 0;
	}
}
