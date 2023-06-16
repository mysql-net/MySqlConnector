using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal abstract class ColumnReader
{
	public abstract object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);

	public virtual int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		throw new InvalidCastException($"Can't convert {columnDefinition.ColumnType} to Int32");
	}
}
