using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BytesColumnReader : IColumnReader
{
	public static BytesColumnReader Instance { get; } = new();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return data.ToArray();
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		throw new InvalidCastException($"Can't convert {columnDefinition.ColumnType} to Int32");
	}
}
