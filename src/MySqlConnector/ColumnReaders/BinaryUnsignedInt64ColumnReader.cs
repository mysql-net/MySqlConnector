namespace MySqlConnector.ColumnReaders;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

internal sealed class BinaryUnsignedInt64ColumnReader : IColumnReader
{
	internal static BinaryUnsignedInt64ColumnReader Instance { get; } = new BinaryUnsignedInt64ColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<ulong>(data);
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return checked((int) MemoryMarshal.Read<ulong>(data));
	}
}
