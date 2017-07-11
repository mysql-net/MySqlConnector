using System;
using System.IO;
using System.Text;

namespace MySql.Data.Serialization
{
	internal sealed class PayloadWriter
	{
		public PayloadWriter()
		{
			m_stream = new MemoryStream();
			m_writer = new BinaryWriter(m_stream);
		}

		public void WriteByte(byte value) => m_writer.Write(value);
		public void WriteInt32(int value) => m_writer.Write(value);
		public void WriteUInt32(uint value) => m_writer.Write(value);
		public void Write(byte[] value) => m_writer.Write(value);
		public void Write(ArraySegment<byte> value) => m_writer.Write(value.Array, value.Offset, value.Count);

		public void WriteLengthEncodedInteger(ulong value)
		{
			if (value < 251)
			{
				m_writer.Write((byte) value);
			}
			else if (value < 65536)
			{
				m_writer.Write((byte) 0xfc);
				m_writer.Write((ushort) value);
			}
			else if (value < 16777216)
			{
				m_writer.Write((byte) 0xfd);
				m_writer.Write((byte) (value & 0xFF));
				m_writer.Write((byte) ((value >> 8) & 0xFF));
				m_writer.Write((byte) ((value >> 16) & 0xFF));
			}
			else
			{
				m_writer.Write((byte) 0xfe);
				m_writer.Write(value);
			}
		}

		public void WriteLengthEncodedString(string value)
		{
			var bytes = Encoding.UTF8.GetBytes(value);
			WriteLengthEncodedInteger((ulong) bytes.Length);
			m_writer.Write(bytes);
		}

		public void WriteNullTerminatedString(string value)
		{
			var bytes = Encoding.UTF8.GetBytes(value);
			m_writer.Write(bytes);
			m_writer.Write((byte) 0);
		}

		public byte[] ToBytes()
		{
			m_writer.Flush();
			using (m_writer)
			using (m_stream)
				return m_stream.ToArray();
		}

		readonly MemoryStream m_stream;
		readonly BinaryWriter m_writer;
	}
}
