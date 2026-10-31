using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryDoubleColumnReader : DoubleColumnReader
{
	public static BinaryDoubleColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ReadDouble(data, columnDefinition);

	public override double ReadDouble(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		MemoryMarshal.Read<double>(data);
}
