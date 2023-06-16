using System.Buffers.Text;
using System.Text;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Utilities;

namespace MySqlConnector.ColumnReaders;

internal sealed class Guid32ColumnReader : IColumnReader
{
	internal static Guid32ColumnReader Instance { get; } = new Guid32ColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		return Utf8Parser.TryParse(data, out Guid guid, out int guid32BytesConsumed, 'N') &&
		       guid32BytesConsumed == 32
			? guid
			: throw new FormatException(
				$"Could not parse CHAR(32) value as Guid: {Encoding.UTF8.GetString(data)}");
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		throw new InvalidCastException($"Can't convert {columnDefinition.ColumnType} to Int32");
	}
}
