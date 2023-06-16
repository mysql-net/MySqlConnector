using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryUnsignedInt8ColumnReader : IColumnReader
{
	internal static BinaryUnsignedInt8ColumnReader Instance { get; } = new BinaryUnsignedInt8ColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return (byte) data[0];
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return (int) (byte) data[0];
	}
}
