using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryBooleanColumnReader : ColumnReader
{
	public static BinaryBooleanColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return data[0] != 0;
	}

	public override int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return data[0] != 0 ? 1 : 0;
	}
}
