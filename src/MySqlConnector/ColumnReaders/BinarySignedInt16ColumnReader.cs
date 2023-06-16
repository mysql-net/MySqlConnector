using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinarySignedInt16ColumnReader : ColumnReader
{
	public static BinarySignedInt16ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<short>(data);
	}

	public override int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<short>(data);
	}
}
