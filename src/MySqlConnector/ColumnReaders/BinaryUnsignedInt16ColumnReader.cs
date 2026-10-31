using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryUnsignedInt16ColumnReader() : ColumnReader(ColumnReaderKind.UnsignedInteger)
{
	public static BinaryUnsignedInt16ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override ulong ReadUInt64(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static ushort DoReadValue(ReadOnlySpan<byte> data) =>
		MemoryMarshal.Read<ushort>(data);
}
