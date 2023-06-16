using System.Buffers.Text;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextSignedInt8ColumnReader : ColumnReader
{
	public static TextSignedInt8ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (!Utf8Parser.TryParse(data, out int value, out var bytesConsumed) || bytesConsumed != data.Length)
		{
			throw new FormatException();
		}
		return (sbyte) value;
	}

	public override int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (!Utf8Parser.TryParse(data, out int value, out var bytesConsumed) || bytesConsumed != data.Length)
		{
			throw new FormatException();
		}
		return value;
	}
}
