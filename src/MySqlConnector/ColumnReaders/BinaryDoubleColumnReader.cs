using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryDoubleColumnReader : ColumnReader
{
	public static BinaryDoubleColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<double>(data);
	}
}
