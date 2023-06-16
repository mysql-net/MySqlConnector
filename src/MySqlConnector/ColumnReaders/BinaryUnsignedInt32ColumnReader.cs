namespace MySqlConnector.ColumnReaders;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

internal sealed class BinaryUnsignedInt32ColumnReader : IColumnReader
{
	internal static BinaryUnsignedInt32ColumnReader Instance { get; } = new BinaryUnsignedInt32ColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<uint>(data);
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return checked((int) MemoryMarshal.Read<uint>(data));
	}
}
