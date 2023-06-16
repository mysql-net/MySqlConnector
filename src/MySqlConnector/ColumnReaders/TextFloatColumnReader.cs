using System.Buffers.Text;
using System.Text;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Utilities;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextFloatColumnReader : IColumnReader
{
	internal static TextFloatColumnReader Instance { get; } = new TextFloatColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (Utf8Parser.TryParse(data, out float floatValue, out var floatBytesConsumed) && floatBytesConsumed == data.Length)
			return floatValue;
		ReadOnlySpan<byte> floatInfinity = "-inf"u8;
		if (data.SequenceEqual(floatInfinity))
			return float.NegativeInfinity;
		if (data.SequenceEqual(floatInfinity.Slice(1)))
			return float.PositiveInfinity;
		if (data.SequenceEqual("nan"u8))
			return float.NaN;
		throw new FormatException($"Couldn't parse value as float: {Encoding.UTF8.GetString(data)}");
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		throw new InvalidCastException($"Can't convert {columnDefinition.ColumnType} to Int32");
	}
}
