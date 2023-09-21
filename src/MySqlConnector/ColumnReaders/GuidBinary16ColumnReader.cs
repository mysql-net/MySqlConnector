using System.Runtime.CompilerServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class GuidBinary16ColumnReader : ColumnReader
{
	public static GuidBinary16ColumnReader Instance { get; } = new();

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ReadGuid(data);

#if NET5_0_OR_GREATER
	[SkipLocalsInit]
#endif
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Guid ReadGuid(ReadOnlySpan<byte> data) =>
#if NET8_0_OR_GREATER
		new(data, bigEndian: true);
#elif NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		new(stackalloc byte[16] { data[3], data[2], data[1], data[0], data[5], data[4], data[7], data[6], data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15] });
#else
		new(new[] { data[3], data[2], data[1], data[0], data[5], data[4], data[7], data[6], data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15] });
#endif
}
