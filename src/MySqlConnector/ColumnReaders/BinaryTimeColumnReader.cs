using System.Runtime.InteropServices;
using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryTimeColumnReader : IColumnReader
{
	internal static BinaryTimeColumnReader Instance { get; } = new BinaryTimeColumnReader();

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (data.Length == 0)
			return TimeSpan.Zero;

		var isNegative = data[0];
		var days = MemoryMarshal.Read<int>(data[1..]);
		var hours = (int) data[5];
		var minutes = (int) data[6];
		var seconds = (int) data[7];
		var microseconds = data.Length == 8 ? 0 : MemoryMarshal.Read<int>(data[8..]);

		if (isNegative != 0)
		{
			days = -days;
			hours = -hours;
			minutes = -minutes;
			seconds = -seconds;
			microseconds = -microseconds;
		}

#if NET7_0_OR_GREATER
		return new TimeSpan(days, hours, minutes, seconds, microseconds / 1000, microseconds % 1000);
#else
		return new TimeSpan(days, hours, minutes, seconds) + TimeSpan.FromTicks(microseconds * 10);
#endif
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		throw new InvalidCastException($"Can't convert {columnDefinition.ColumnType} to Int32");
	}
}
