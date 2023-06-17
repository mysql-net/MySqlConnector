using System.Buffers.Text;
using System.Text;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Utilities;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextFloatColumnReader : ColumnReader
{
	public static TextFloatColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (Utf8Parser.TryParse(data, out float floatValue, out var floatBytesConsumed) && floatBytesConsumed == data.Length)
			return floatValue;
		if (data.SequenceEqual("-inf"u8))
			return float.NegativeInfinity;
		if (data.SequenceEqual("inf"u8))
			return float.PositiveInfinity;
		if (data.SequenceEqual("nan"u8))
			return float.NaN;
		throw new FormatException($"Couldn't parse value as float: {Encoding.UTF8.GetString(data)}");
	}
}
