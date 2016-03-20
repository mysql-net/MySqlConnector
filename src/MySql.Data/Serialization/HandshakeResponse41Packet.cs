using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MySql.Data.Serialization
{
    internal sealed class HandshakeResponse41Packet : Packet
    {
	    public static byte[] Create(InitialHandshakePacket handshake, string userName, string password, string database)
	    {
			// TODO: verify server capabilities

			var writer = new PayloadWriter();

		    writer.Write((int) (
				ProtocolCapabilities.Protocol41 |
				ProtocolCapabilities.LongPassword |
				ProtocolCapabilities.SecureConnection |
				ProtocolCapabilities.PluginAuth |
				ProtocolCapabilities.PluginAuthLengthEncodedClientData |
				ProtocolCapabilities.ConnectWithDatabase));
			writer.Write(0x40000000);
		    writer.Write((byte) 46); // utf8mb4_bin
			writer.Write(new byte[23]);
		    writer.WriteNullTerminatedString(userName);

		    using (var sha1 = SHA1.Create())
		    {
				var combined = new byte[40];
				Array.Copy(handshake.AuthPluginData, combined, 20);

				var passwordBytes = Encoding.UTF8.GetBytes(password);
			    var hashedPassword = sha1.ComputeHash(passwordBytes);

				var doubleHashedPassword = sha1.ComputeHash(hashedPassword);
			    Array.Copy(doubleHashedPassword, 0, combined, 20, 20);

			    var xorBytes = sha1.ComputeHash(combined);

				for (int i = 0; i < hashedPassword.Length; i++)
				    hashedPassword[i] ^= xorBytes[i];
				writer.WriteLengthEncodedInteger(20);
			    writer.Write(hashedPassword);
		    }

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

		public void Write(byte value) => m_writer.Write(value);
		public void Write(int value) => m_writer.Write(value);
		public void Write(byte[] value) => m_writer.Write(value);

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
			var payloadLength = (int) m_stream.Length;
			var result = new byte[payloadLength + 4];

			result[0] = (byte) ((payloadLength & 0xFF));
			result[1] = (byte) ((payloadLength >> 8) & 0xFF);
			result[2] = (byte) ((payloadLength >> 16) & 0xFF);
			result[3] = 1;

			m_stream.Position = 0;
			m_stream.Read(result, 4, payloadLength);
			return result;
		}

		readonly MemoryStream m_stream;
		readonly BinaryWriter m_writer;
	}
}
