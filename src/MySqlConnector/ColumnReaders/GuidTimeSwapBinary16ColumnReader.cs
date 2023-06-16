using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class GuidTimeSwapBinary16ColumnReader : ColumnReader
{
	public static GuidTimeSwapBinary16ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		return new Guid(stackalloc byte[16] { data[7], data[6], data[5], data[4], data[3], data[2], data[1], data[0], data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15] });
#else
		return new Guid(new[]
		{
			data[7], data[6], data[5], data[4], data[3], data[2], data[1], data[0], data[8], data[9],
			data[10], data[11], data[12], data[13], data[14], data[15],
		});
#endif
	}
}
