using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinarySignedInt8ColumnReader : IColumnReader
{
	public static BinarySignedInt8ColumnReader Instance { get; } = new();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return (sbyte) data[0];
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return (int) (sbyte) data[0];
	}
}
