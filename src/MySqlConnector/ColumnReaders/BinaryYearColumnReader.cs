using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryYearColumnReader : ColumnReader
{
	public static BinaryYearColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return (int) MemoryMarshal.Read<short>(data);
	}

	public override int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<short>(data);
	}
}
