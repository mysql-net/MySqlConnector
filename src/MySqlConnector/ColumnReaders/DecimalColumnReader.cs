using System.Buffers.Text;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class DecimalColumnReader : ColumnReader
{
	public static DecimalColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		!Utf8Parser.TryParse(data, out decimal decimalValue, out int bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : decimalValue;

	public override int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		!Utf8Parser.TryParse(data, out decimal decimalValue, out int bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : (int) decimalValue;
}
