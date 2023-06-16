namespace MySqlConnector.ColumnReaders;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

internal sealed class BinarySignedInt16ColumnReader : IColumnReader
{
	internal static BinarySignedInt16ColumnReader Instance { get; } = new BinarySignedInt16ColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<short>(data);
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return (int) MemoryMarshal.Read<short>(data);
	}
}
