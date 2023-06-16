using System.Buffers.Text;
using System.Runtime.CompilerServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextUnsignedInt8ColumnReader : ColumnReader
{
	public static TextUnsignedInt8ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte DoReadValue(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out byte value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;
}
