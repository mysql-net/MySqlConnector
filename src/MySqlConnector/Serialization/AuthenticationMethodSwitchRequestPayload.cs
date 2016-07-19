using System.Text;

namespace MySql.Data.Serialization
{
	internal class AuthenticationMethodSwitchRequestPayload
	{
		public string Name { get; }
		public byte[] Data { get; }

		public static byte Signature = 0xFE;

		public static AuthenticationMethodSwitchRequestPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(Signature);
			var name = Encoding.UTF8.GetString(reader.ReadNullTerminatedByteString());
			var data = reader.ReadByteString(reader.BytesRemaining);
			return new AuthenticationMethodSwitchRequestPayload(name, data);
		}

		private AuthenticationMethodSwitchRequestPayload(string name, byte[] data)
		{
			Name = name;
			Data = data;
		}
	}
}
