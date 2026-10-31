using System.Buffers.Text;
using System.Runtime.CompilerServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextUnsignedInt64ColumnReader() : ColumnReader(ColumnReaderKind.UnsignedInteger)
{
	public static TextUnsignedInt64ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override ulong ReadUInt64(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ulong DoReadValue(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out ulong value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;
}
