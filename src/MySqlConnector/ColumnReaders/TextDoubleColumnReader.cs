using System.Buffers.Text;
using System.Text;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Utilities;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextDoubleColumnReader : ColumnReader
{
	public static TextDoubleColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (Utf8Parser.TryParse(data, out double doubleValue, out var doubleBytesConsumed) && doubleBytesConsumed == data.Length)
			return doubleValue;
		ReadOnlySpan<byte> doubleInfinity = "-inf"u8;
		if (data.SequenceEqual(doubleInfinity))
			return double.NegativeInfinity;
		if (data.SequenceEqual(doubleInfinity.Slice(1)))
			return double.PositiveInfinity;
		if (data.SequenceEqual("nan"u8))
			return double.NaN;
		throw new FormatException($"Couldn't parse value as double: {Encoding.UTF8.GetString(data)}");
	}
}
