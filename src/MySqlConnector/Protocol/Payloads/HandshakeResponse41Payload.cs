using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads;

internal static class HandshakeResponse41Payload
{
	private static ByteBufferWriter CreateCapabilitiesPayload(ProtocolCapabilities serverCapabilities, ConnectionSettings cs, bool useCompression, CharacterSet characterSet, ProtocolCapabilities additionalCapabilities = 0)
	{
		var writer = new ByteBufferWriter();

		writer.Write((int) (
			ProtocolCapabilities.Protocol41 |
			(cs.InteractiveSession ? (serverCapabilities & ProtocolCapabilities.Interactive) : 0) |
			(serverCapabilities & ProtocolCapabilities.LongPassword) |
			(serverCapabilities & ProtocolCapabilities.Transactions) |
			ProtocolCapabilities.SecureConnection |
			(serverCapabilities & ProtocolCapabilities.PluginAuth) |
			(serverCapabilities & ProtocolCapabilities.PluginAuthLengthEncodedClientData) |
			ProtocolCapabilities.MultiStatements |
			ProtocolCapabilities.MultiResults |
			(cs.AllowLoadLocalInfile ? ProtocolCapabilities.LocalFiles : 0) |
			(string.IsNullOrWhiteSpace(cs.Database) ? 0 : ProtocolCapabilities.ConnectWithDatabase) |
			(cs.UseAffectedRows ? 0 : ProtocolCapabilities.FoundRows) |
			(useCompression ? ProtocolCapabilities.Compress : ProtocolCapabilities.None) |
			(serverCapabilities & ProtocolCapabilities.ConnectionAttributes) |
			(serverCapabilities & ProtocolCapabilities.SessionTrack) |
			(serverCapabilities & ProtocolCapabilities.DeprecateEof) |
			(serverCapabilities & ProtocolCapabilities.QueryAttributes) |
			additionalCapabilities));
		writer.Write(0x4000_0000);
		writer.Write((byte) characterSet);

		// NOTE: not new byte[19]; see https://github.com/dotnet/roslyn/issues/33088
		ReadOnlySpan<byte> padding = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		writer.Write(padding);

		if ((serverCapabilities & ProtocolCapabilities.LongPassword) == 0)
		{
			// MariaDB writes extended capabilities at the end of the padding
			writer.Write((int) (((long) (serverCapabilities & ProtocolCapabilities.MariaDbComMulti)) >> 32));
		}
		else
		{
			writer.Write(0u);
		}

		return writer;
	}

	public static PayloadData CreateWithSsl(ProtocolCapabilities serverCapabilities, ConnectionSettings cs, bool useCompression, CharacterSet characterSet) =>
		CreateCapabilitiesPayload(serverCapabilities, cs, useCompression, characterSet, ProtocolCapabilities.Ssl).ToPayloadData();

	public static PayloadData Create(InitialHandshakePayload handshake, ConnectionSettings cs, string password, bool useCompression, CharacterSet characterSet, byte[]? connectionAttributes)
	{
		// TODO: verify server capabilities
		var writer = CreateCapabilitiesPayload(handshake.ProtocolCapabilities, cs, useCompression, characterSet);
		writer.WriteNullTerminatedString(cs.UserID);
		var authenticationResponse = AuthenticationUtility.CreateAuthenticationResponse(handshake.AuthPluginData, password);
		writer.Write((byte) authenticationResponse.Length);
		writer.Write(authenticationResponse);

		if (!string.IsNullOrWhiteSpace(cs.Database))
			writer.WriteNullTerminatedString(cs.Database);

		if ((handshake.ProtocolCapabilities & ProtocolCapabilities.PluginAuth) != 0)
			writer.WriteNullTerminatedString("mysql_native_password");

		if (connectionAttributes is not null)
			writer.Write(connectionAttributes);

		return writer.ToPayloadData();
	}
}
