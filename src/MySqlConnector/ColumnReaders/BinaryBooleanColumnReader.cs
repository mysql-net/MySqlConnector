using System.Runtime.CompilerServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryBooleanColumnReader() : ColumnReader(ColumnReaderKind.Boolean)
{
	public static BinaryBooleanColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	public override bool ReadBoolean(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		DoReadValue(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool DoReadValue(ReadOnlySpan<byte> data) =>
		data[0] != 0;
}
