using System.Runtime.CompilerServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryUnsignedInt8ColumnReader() : ColumnReader(ColumnReaderKind.UnsignedInteger)
{
	public static BinaryUnsignedInt8ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override ulong ReadUInt64(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static byte DoReadValue(ReadOnlySpan<byte> data) =>
		data[0];
}
