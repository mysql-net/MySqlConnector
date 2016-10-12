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

			var authenticationResponse = AuthenticationUtility.CreateAuthenticationResponse(handshake.AuthPluginData, 0, password);
			writer.WriteByte((byte) authenticationResponse.Length);
			writer.Write(authenticationResponse);

			if (!string.IsNullOrWhiteSpace(database))
				writer.WriteNullTerminatedString(database);

			writer.WriteNullTerminatedString("mysql_native_password");

			return writer.ToBytes();
		}
	}
}
