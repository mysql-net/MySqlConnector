using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class AuthenticationMoreDataPayload
	{
		public byte[] Data { get; }

		public const byte Signature = 0x01;

		public static AuthenticationMoreDataPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			reader.ReadByte(Signature);
			return new AuthenticationMoreDataPayload(reader.ReadByteString(reader.BytesRemaining));
		}

		private AuthenticationMoreDataPayload(byte[] data) => Data = data;
	}
}
