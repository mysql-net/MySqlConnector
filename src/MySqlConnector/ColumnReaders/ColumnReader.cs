using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal abstract class ColumnReader
{
	public abstract object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);

	public virtual int? TryReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		default;
}
