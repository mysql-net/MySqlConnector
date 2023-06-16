using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryUnsignedInt64ColumnReader : ColumnReader
{
	public static BinaryUnsignedInt64ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<ulong>(data);
	}

	public override int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return checked((int) MemoryMarshal.Read<ulong>(data));
	}
}
