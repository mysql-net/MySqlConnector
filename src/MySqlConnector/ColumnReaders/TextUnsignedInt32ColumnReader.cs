using System.Buffers.Text;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextUnsignedInt32ColumnReader() : ColumnReader(ColumnReaderKind.UnsignedInteger)
{
	public static TextUnsignedInt32ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override ulong ReadUInt64(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	private static uint DoReadValue(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out uint value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;
}
