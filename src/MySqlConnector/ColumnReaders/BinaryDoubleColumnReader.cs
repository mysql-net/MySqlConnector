namespace MySqlConnector.ColumnReaders;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

internal sealed class BinaryDoubleColumnReader : IColumnReader
{
	internal static BinaryDoubleColumnReader Instance { get; } = new BinaryDoubleColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return MemoryMarshal.Read<double>(data);
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		throw new InvalidCastException($"Can't convert {columnDefinition.ColumnType} to Int32");
	}
}
