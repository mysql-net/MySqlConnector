namespace MySqlConnector.ColumnReaders;
using System.Buffers.Text;
using System.Text;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

internal sealed class Guid36ColumnReader : IColumnReader
{
	internal static Guid36ColumnReader Instance { get; } = new Guid36ColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return Utf8Parser.TryParse(data, out Guid guid, out int guid36BytesConsumed, 'D') &&
		       guid36BytesConsumed == 36
			? guid
			: throw new FormatException(
				$"Could not parse CHAR(36) value as Guid: {Encoding.UTF8.GetString(data)}");
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		throw new InvalidCastException($"Can't convert {columnDefinition.ColumnType} to Int32");
	}
}
