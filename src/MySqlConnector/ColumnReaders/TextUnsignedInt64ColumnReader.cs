using System.Buffers.Text;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextUnsignedInt64ColumnReader : IColumnReader
{
	public static TextUnsignedInt64ColumnReader Instance { get; } = new();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return !Utf8Parser.TryParse(data, out ulong value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (!Utf8Parser.TryParse(data, out ulong value, out var bytesConsumed) || bytesConsumed != data.Length)
		{
			throw new FormatException();
		}
		return checked((int) value);
	}
}
