using System.Buffers.Text;
using System.Runtime.CompilerServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class DecimalColumnReader : ColumnReader
{
	public static DecimalColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override int? TryReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		(int) DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static decimal DoReadValue(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out decimal decimalValue, out int bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : decimalValue;
}
