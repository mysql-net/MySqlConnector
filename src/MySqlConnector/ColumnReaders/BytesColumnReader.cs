using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BytesColumnReader : ColumnReader
{
	public static BytesColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return data.ToArray();
	}
}
