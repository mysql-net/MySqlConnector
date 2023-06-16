namespace MySqlConnector.ColumnReaders;
using System.Buffers.Text;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

internal sealed class BinarySignedInt8ColumnReader : IColumnReader
{
	internal static BinarySignedInt8ColumnReader Instance { get; } = new BinarySignedInt8ColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return (sbyte) data[0];
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return (int) (sbyte) data[0];
	}
}
