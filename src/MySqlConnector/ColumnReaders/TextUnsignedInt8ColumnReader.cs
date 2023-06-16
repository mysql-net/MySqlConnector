using System.Buffers.Text;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextUnsignedInt8ColumnReader : IColumnReader
{
	public static TextUnsignedInt8ColumnReader Instance { get; } = new();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (!Utf8Parser.TryParse(data, out int value, out var bytesConsumed) || bytesConsumed != data.Length)
		{
			throw new FormatException();
		}
		return (byte) value;
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (!Utf8Parser.TryParse(data, out int value, out var bytesConsumed) || bytesConsumed != data.Length)
		{
			throw new FormatException();
		}
		return value;
	}
}
