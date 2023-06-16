using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class GuidLittleEndianBinary16ColumnReader : IColumnReader
{
	public static GuidLittleEndianBinary16ColumnReader Instance { get; } = new();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		return new Guid(data);
#else
		return new Guid(data.ToArray());
#endif
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		throw new InvalidCastException($"Can't convert {columnDefinition.ColumnType} to Int32");
	}
}
