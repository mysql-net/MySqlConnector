namespace MySql.Data.Serialization
{
	internal sealed class HandshakeResponse41Packet
	{
		private static PayloadWriter CreateCapabilitiesPayload(ProtocolCapabilities serverCapabilities, ConnectionSettings cs, ProtocolCapabilities additionalCapabilities=0)
		{
			var writer = new PayloadWriter();

			writer.WriteInt32((int) (
				ProtocolCapabilities.Protocol41 |
				ProtocolCapabilities.LongPassword |
				ProtocolCapabilities.SecureConnection |
				(serverCapabilities & ProtocolCapabilities.PluginAuth) |
				(serverCapabilities & ProtocolCapabilities.PluginAuthLengthEncodedClientData) |
				ProtocolCapabilities.MultiStatements |
				ProtocolCapabilities.MultiResults |
				ProtocolCapabilities.PreparedStatementMultiResults |
				ProtocolCapabilities.LocalFiles |
				(string.IsNullOrWhiteSpace(cs.Database) ? 0 : ProtocolCapabilities.ConnectWithDatabase) |
				(cs.UseAffectedRows ? 0 : ProtocolCapabilities.FoundRows) |
				(cs.UseCompression ? ProtocolCapabilities.Compress : ProtocolCapabilities.None) |
				additionalCapabilities));
			writer.WriteInt32(0x4000_0000);
			writer.WriteByte((byte) CharacterSet.Utf8Mb4Binary);
			writer.Write(new byte[23]);

			return writer;
		}

		public static byte[] InitSsl(ProtocolCapabilities serverCapabilities, ConnectionSettings cs)
		{
			return CreateCapabilitiesPayload(serverCapabilities, cs, ProtocolCapabilities.Ssl).ToBytes();
		}

		public static byte[] Create(InitialHandshakePacket handshake, ConnectionSettings cs)
		{
			// TODO: verify server capabilities

			var writer = CreateCapabilitiesPayload(handshake.ProtocolCapabilities, cs);
			writer.WriteNullTerminatedString(cs.UserID);
			var authenticationResponse = AuthenticationUtility.CreateAuthenticationResponse(handshake.AuthPluginData, 0, cs.Password);
			writer.WriteByte((byte) authenticationResponse.Length);
			writer.Write(authenticationResponse);

			if (!string.IsNullOrWhiteSpace(cs.Database))
				writer.WriteNullTerminatedString(cs.Database);

			if ((handshake.ProtocolCapabilities & ProtocolCapabilities.PluginAuth) != 0)
				writer.WriteNullTerminatedString("mysql_native_password");

			return writer.ToBytes();
		}
	}
}
