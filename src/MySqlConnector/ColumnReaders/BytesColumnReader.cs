using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BytesColumnReader() : ColumnReader(ColumnReaderKind.Bytes)
{
	public static BytesColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		data.ToArray();
}
