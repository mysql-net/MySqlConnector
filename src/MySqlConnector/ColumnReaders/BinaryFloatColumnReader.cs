using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryFloatColumnReader : FloatColumnReader
{
	public static BinaryFloatColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ReadSingle(data, columnDefinition);

	public override float ReadSingle(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		MemoryMarshal.Read<float>(data);
}
