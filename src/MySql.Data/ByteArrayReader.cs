using System;
using static System.FormattableString;

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

		public ByteArrayReader(ArraySegment<byte> arraySegment)
			: this(arraySegment.Array, arraySegment.Offset, arraySegment.Count)
		{
		}

		public int Offset
		{
			get { return m_offset; }
			set
			{
				if (value < 0 || value > m_maxOffset)
					throw new ArgumentOutOfRangeException(nameof(value), Invariant($"value must be between 0 and {m_maxOffset}"));
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

		public uint ReadUInt32()
		{
			VerifyRead(4);
			var result = BitConverter.ToUInt32(m_buffer, m_offset);
			m_offset += 4;
			return result;
		}

		public uint ReadFixedLengthUInt32(int length)
		{
			if (length <= 0 || length > 4)
				throw new ArgumentOutOfRangeException(nameof(length));
			VerifyRead(length);
			uint result = 0;
			for (int i = 0; i < length; i++)
				result |= ((uint) m_buffer[m_offset + i]) << (8 * i);
			m_offset += length;
			return result;
		}

		public ulong ReadFixedLengthUInt64(int length)
		{
			if (length <= 0 || length > 8)
				throw new ArgumentOutOfRangeException(nameof(length));
			VerifyRead(length);
			ulong result = 0;
			for (int i = 0; i < length; i++)
				result |= ((ulong) m_buffer[m_offset + i]) << (8 * i);
			m_offset += length;
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

		public ulong ReadLengthEncodedInteger()
		{
			byte encodedLength = m_buffer[m_offset++];
			switch (encodedLength)
			{
			case 0xFC:
				return ReadFixedLengthUInt32(2);
			case 0xFD:
				return ReadFixedLengthUInt32(3);
			case 0xFE:
				return ReadFixedLengthUInt64(8);
			case 0xFF:
				throw new FormatException("Length-encoded integer cannot have 0xFF prefix byte.");
			default:
				return encodedLength;
			}
		}

		public ArraySegment<byte> ReadLengthEncodedByteString()
		{
			var length = checked((int) ReadLengthEncodedInteger());
			var result = new ArraySegment<byte>(m_buffer, m_offset, length);
			m_offset += length;
			return result;
		}

		public int BytesRemaining => m_maxOffset - m_offset;

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
