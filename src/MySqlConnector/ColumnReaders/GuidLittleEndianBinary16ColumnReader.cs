using System.Runtime.CompilerServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class GuidLittleEndianBinary16ColumnReader : ColumnReader
{
	public static GuidLittleEndianBinary16ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ReadGuid(data);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Guid ReadGuid(ReadOnlySpan<byte> data) =>
#if NET8_0_OR_GREATER
		new(data, bigEndian: false);
#elif NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		new(data);
#else
		new(data.ToArray());
#endif
}
