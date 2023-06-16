namespace MySqlConnector.ColumnReaders;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

internal sealed class BinarySignedInt32ColumnReader : IColumnReader
{
	internal static BinarySignedInt32ColumnReader Instance { get; } = new BinarySignedInt32ColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<int>(data);
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<int>(data);
	}
}
