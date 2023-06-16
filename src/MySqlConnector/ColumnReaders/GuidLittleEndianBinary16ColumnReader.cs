using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class GuidLittleEndianBinary16ColumnReader : ColumnReader
{
	public static GuidLittleEndianBinary16ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		return new Guid(data);
#else
		return new Guid(data.ToArray());
#endif
	}
}
