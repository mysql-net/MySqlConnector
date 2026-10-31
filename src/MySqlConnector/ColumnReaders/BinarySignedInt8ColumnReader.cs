using System.Runtime.CompilerServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinarySignedInt8ColumnReader() : ColumnReader(ColumnReaderKind.SignedInteger)
{
	public static BinarySignedInt8ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override long ReadInt64(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static sbyte DoReadValue(ReadOnlySpan<byte> data) =>
		(sbyte) data[0];
}
