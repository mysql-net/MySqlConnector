using System;

namespace MySql.Data
{
    internal sealed class ByteArrayReader
    {
	    public ByteArrayReader(byte[] buffer, int offset, int length)
	    {
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (offset < 0)
				throw new ArgumentOutOfRangeException(nameof(offset));
			if (offset + length > buffer.Length)
				throw new ArgumentOutOfRangeException(nameof(length));

		    m_buffer = buffer;
			m_maxOffset = offset + length;
		    m_offset = offset;
	    }

		public byte ReadByte()
		{
			VerifyRead(1);
			return m_buffer[m_offset++];
		}

		public void ReadByte(byte value)
	    {
			if (ReadByte() != value)
				throw new FormatException($"Expected to read 0x{value:X2} but got 0x{m_buffer[m_offset]:X2}");
	    }

		public short ReadInt16()
		{
			VerifyRead(2);
			var result = BitConverter.ToInt16(m_buffer, m_offset);
			m_offset += 2;
			return result;
		}

		public ushort ReadUInt16()
		{
			VerifyRead(2);
			var result = BitConverter.ToUInt16(m_buffer, m_offset);
			m_offset += 2;
			return result;
		}

		public int ReadInt32()
	    {
		    VerifyRead(4);
		    var result = BitConverter.ToInt32(m_buffer, m_offset);
		    m_offset += 4;
		    return result;
	    }

		// TODO: Span<byte>
	    public byte[] ReadNullTerminatedByteString()
	    {
		    int index = m_offset;
		    while (index < m_maxOffset && m_buffer[index] != 0)
			    index++;
			if (index == m_maxOffset)
				throw new FormatException($"Read past end of buffer looking for NUL.");
			byte[] substring = new byte[index - m_offset];
		    Array.Copy(m_buffer, m_offset, substring, 0, substring.Length);
		    m_offset = index + 1;
		    return substring;
	    }

	    public byte[] ReadByteString(int length)
	    {
		    VerifyRead(length);
		    var result = new byte[length];
		    Array.Copy(m_buffer, m_offset, result, 0, result.Length);
		    m_offset += length;
		    return result;
	    }

		public int BytesRemaining => m_maxOffset - m_offset ;

	    private void VerifyRead(int length)
	    {
		    if (m_offset + length > m_maxOffset)
				throw new InvalidOperationException("Read past end of buffer.");
	    }

		readonly byte[] m_buffer;
		readonly int m_maxOffset;
		int m_offset;
	}
}
