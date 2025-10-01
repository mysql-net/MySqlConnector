using System.Buffers.Binary;

namespace MySqlConnector.Protocol.Serialization;

internal ref struct ByteArrayReader
{
	public ByteArrayReader(ReadOnlySpan<byte> buffer)
	{
		m_buffer = buffer;
		m_offset = 0;
		m_maxOffset = buffer.Length;
	}

	public int Offset
	{
		readonly get => m_offset;
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			ArgumentOutOfRangeException.ThrowIfGreaterThan(value, m_maxOffset);
			m_offset = value;
		}
	}

	public byte ReadByte()
	{
		VerifyRead(1);
		return m_buffer[m_offset++];
	}

	public void ReadByte(byte value)
	{
		if (ReadByte() != value)
			throw new FormatException($"Expected to read 0x{value:X2} but got 0x{m_buffer[m_offset - 1]:X2}");
	}

	public short ReadInt16()
	{
		VerifyRead(2);
		var result = BinaryPrimitives.ReadInt16LittleEndian(m_buffer[m_offset..]);
		m_offset += 2;
		return result;
	}

	public ushort ReadUInt16()
	{
		VerifyRead(2);
		var result = BinaryPrimitives.ReadUInt16LittleEndian(m_buffer[m_offset..]);
		m_offset += 2;
		return result;
	}

	public int ReadInt32()
	{
		VerifyRead(4);
		var result = BinaryPrimitives.ReadInt32LittleEndian(m_buffer[m_offset..]);
		m_offset += 4;
		return result;
	}

	public uint ReadUInt32()
	{
		VerifyRead(4);
		var result = BinaryPrimitives.ReadUInt32LittleEndian(m_buffer[m_offset..]);
		m_offset += 4;
		return result;
	}

	public uint ReadFixedLengthUInt32(int length)
	{
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 4);
		VerifyRead(length);
		uint result = 0;
		for (var i = 0; i < length; i++)
			result |= ((uint) m_buffer[m_offset + i]) << (8 * i);
		m_offset += length;
		return result;
	}

	public ulong ReadFixedLengthUInt64(int length)
	{
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(length, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 8);
		VerifyRead(length);
		ulong result = 0;
		for (var i = 0; i < length; i++)
			result |= ((ulong) m_buffer[m_offset + i]) << (8 * i);
		m_offset += length;
		return result;
	}

	public ReadOnlySpan<byte> ReadNullTerminatedByteString()
	{
		int index = m_offset;
		while (index < m_maxOffset && m_buffer[index] != 0)
			index++;
		if (index == m_maxOffset)
			throw new FormatException("Read past end of buffer looking for NUL.");
		var substring = m_buffer[m_offset..index];
		m_offset = index + 1;
		return substring;
	}

	public ReadOnlySpan<byte> ReadNullOrEofTerminatedByteString()
	{
		int index = m_offset;
		while (index < m_maxOffset && m_buffer[index] != 0)
			index++;
		var substring = m_buffer[m_offset..index];
		if (index < m_maxOffset && m_buffer[index] == 0)
			index++;
		m_offset = index;
		return substring;
	}

	public ReadOnlySpan<byte> ReadByteString(int length)
	{
		VerifyRead(length);
		var result = m_buffer.Slice(m_offset, length);
		m_offset += length;
		return result;
	}

	public ulong ReadLengthEncodedInteger()
	{
		var encodedLength = m_buffer[m_offset++];
		return encodedLength switch
		{
			0xFB => throw new FormatException("Length-encoded integer cannot have 0xFB prefix byte."),
			0xFC => ReadFixedLengthUInt32(2),
			0xFD => ReadFixedLengthUInt32(3),
			0xFE => ReadFixedLengthUInt64(8),
			0xFF => throw new FormatException("Length-encoded integer cannot have 0xFF prefix byte."),
			_ => encodedLength,
		};
	}

	public int ReadLengthEncodedIntegerOrNull()
	{
		if (m_buffer[m_offset] == 0xFB)
		{
			// "NULL is sent as 0xfb" (https://dev.mysql.com/doc/internals/en/com-query-response.html#packet-ProtocolText::ResultsetRow)
			m_offset++;
			return -1;
		}
		return checked((int) ReadLengthEncodedInteger());
	}

	public ReadOnlySpan<byte> ReadLengthEncodedByteString()
	{
		var length = checked((int) ReadLengthEncodedInteger());
		var result = m_buffer.Slice(m_offset, length);
		m_offset += length;
		return result;
	}

	public readonly int BytesRemaining => m_maxOffset - m_offset;

	private readonly void VerifyRead(int length)
	{
		if (m_offset + length > m_maxOffset)
			throw new InvalidOperationException("Read past end of buffer.");
	}

	private readonly ReadOnlySpan<byte> m_buffer;
	private readonly int m_maxOffset;
	private int m_offset;
}
