using System.Buffers.Text;
using System.Runtime.CompilerServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextSignedInt64ColumnReader : ColumnReader
{
	public static TextSignedInt64ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override int? TryReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		checked((int) DoReadValue(data));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static long DoReadValue(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out long value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;
}
