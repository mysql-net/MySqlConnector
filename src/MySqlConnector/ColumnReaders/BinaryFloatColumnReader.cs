using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryFloatColumnReader : ColumnReader
{
	public static BinaryFloatColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<float>(data);
	}
}
