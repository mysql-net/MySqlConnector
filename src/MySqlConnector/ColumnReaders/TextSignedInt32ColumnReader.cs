using System.Buffers.Text;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextSignedInt32ColumnReader : ColumnReader
{
	public static TextSignedInt32ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return !Utf8Parser.TryParse(data, out int value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;
	}

	public override int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return !Utf8Parser.TryParse(data, out int value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;
	}
}
