using System.Text;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	internal class AuthenticationMethodSwitchRequestPayload
	{
		public string Name { get; }
		public byte[] Data { get; }

		public const byte Signature = 0xFE;

		public static AuthenticationMethodSwitchRequestPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(Signature);
			string name;
			byte[] data;
			if (payload.ArraySegment.Count == 1)
			{
				// if the packet is just the header byte (0xFE), it's an "Old Authentication Method Switch Request Packet"
				// (possibly sent by a server that doesn't support CLIENT_PLUGIN_AUTH)
				name = "mysql_old_password";
				data = new byte[0];
			}
			else
			{
				name = Encoding.UTF8.GetString(reader.ReadNullTerminatedByteString());
				data = reader.ReadByteString(reader.BytesRemaining);
			}
			return new AuthenticationMethodSwitchRequestPayload(name, data);
		}

		private AuthenticationMethodSwitchRequestPayload(string name, byte[] data)
		{
			Name = name;
			Data = data;
		}
	}
}
