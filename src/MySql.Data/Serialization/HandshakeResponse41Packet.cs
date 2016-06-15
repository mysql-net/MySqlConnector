using System;
using System.IO;
using System.Text;

namespace MySql.Data.Serialization
{
	internal sealed class HandshakeResponse41Packet
	{
		public static byte[] Create(InitialHandshakePacket handshake, string userName, string password, string database)
		{
			// TODO: verify server capabilities

			var writer = new PayloadWriter();

			writer.WriteInt32((int) (
				ProtocolCapabilities.Protocol41 |
				ProtocolCapabilities.LongPassword |
				ProtocolCapabilities.SecureConnection |
				ProtocolCapabilities.PluginAuth |
				ProtocolCapabilities.PluginAuthLengthEncodedClientData |
				ProtocolCapabilities.MultiStatements |
				ProtocolCapabilities.MultiResults |
				ProtocolCapabilities.PreparedStatementMultiResults |
				(string.IsNullOrWhiteSpace(database) ? 0 : ProtocolCapabilities.ConnectWithDatabase)));
			writer.WriteInt32(0x40000000);
			writer.WriteByte((byte) CharacterSet.Utf8Mb4Binary);
			writer.Write(new byte[23]);
			writer.WriteNullTerminatedString(userName);

			writer.WriteLengthEncodedInteger(20);
			var hashedPassword = AuthenticationUtility.HashPassword(handshake.AuthPluginData, 0, password);
			writer.Write(hashedPassword);

			if (!string.IsNullOrWhiteSpace(database))
				writer.WriteNullTerminatedString(database);

			writer.WriteNullTerminatedString("mysql_native_password");

			return writer.ToBytes();
		}
	}

	class PayloadWriter
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

		public void WriteNullTerminatedString(string value)
		{
			var bytes = Encoding.UTF8.GetBytes(value);
			m_writer.Write(bytes);
			m_writer.Write((byte) 0);
		}

		public byte[] ToBytes()
		{
			m_writer.Flush();
			return m_stream.ToArray();
		}

		readonly MemoryStream m_stream;
		readonly BinaryWriter m_writer;
	}
}
