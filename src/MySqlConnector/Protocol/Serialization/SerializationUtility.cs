namespace MySqlConnector.Protocol.Serialization;

internal static class SerializationUtility
{
	public static uint ReadUInt32(ReadOnlySpan<byte> span)
	{
		uint value = 0;
		for (var i = 0; i < span.Length; i++)
			value |= ((uint) span[i]) << (8 * i);
		return value;
	}

	public static void WriteUInt32(uint value, byte[] buffer, int offset, int count)
	{
		for (var i = 0; i < count; i++)
		{
			buffer[offset + i] = (byte) (value & 0xFF);
			value >>= 8;
		}
	}

	public static void WriteUInt32(uint value, Span<byte> buffer)
	{
		for (var i = 0; i < buffer.Length; i++)
		{
			buffer[i] = (byte) (value & 0xFF);
			value >>= 8;
		}
	}
}
