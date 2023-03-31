using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads;

internal static class HandshakeResponse41Payload
{
	private static ByteBufferWriter CreateCapabilitiesPayload(ProtocolCapabilities serverCapabilities, ConnectionSettings cs, bool useCompression, CharacterSet characterSet, ProtocolCapabilities additionalCapabilities = 0)
	{
		var writer = new ByteBufferWriter();

		var clientCapabilities = (ProtocolCapabilities.Protocol41 |
		                          (cs.InteractiveSession ? ProtocolCapabilities.Interactive : 0) |
		                          ProtocolCapabilities.LongPassword |
		                          ProtocolCapabilities.Transactions |
		                          ProtocolCapabilities.SecureConnection |
		                          ProtocolCapabilities.PluginAuth |
		                          ProtocolCapabilities.PluginAuthLengthEncodedClientData |
		                          ProtocolCapabilities.MultiStatements |
		                          ProtocolCapabilities.MultiResults |
		                          (cs.AllowLoadLocalInfile ? ProtocolCapabilities.LocalFiles : 0) |
		                          (string.IsNullOrWhiteSpace(cs.Database)
			                          ? 0
			                          : ProtocolCapabilities.ConnectWithDatabase) |
		                          (cs.UseAffectedRows ? 0 : ProtocolCapabilities.FoundRows) |
		                          (useCompression ? ProtocolCapabilities.Compress : ProtocolCapabilities.None) |
		                          ProtocolCapabilities.ConnectionAttributes |
		                          ProtocolCapabilities.SessionTrack |
		                          ProtocolCapabilities.DeprecateEof |
		                          ProtocolCapabilities.QueryAttributes |
		                          ProtocolCapabilities.MariaDbComMulti |
		                          ProtocolCapabilities.MariaDbCacheMetadata |
		                          additionalCapabilities) & serverCapabilities;

		writer.Write((int) clientCapabilities);
		writer.Write(0x4000_0000);
		writer.Write((byte) characterSet);

		// NOTE: not new byte[19]; see https://github.com/dotnet/roslyn/issues/33088
		ReadOnlySpan<byte> padding = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		writer.Write(padding);

		if ((serverCapabilities & ProtocolCapabilities.LongPassword) == 0)
		{
			// MariaDB writes extended capabilities at the end of the padding
			writer.Write((int) ((ulong) clientCapabilities >> 32));
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
			writer.Write("mysql_native_password\0"u8);

		if (connectionAttributes is not null)
			writer.Write(connectionAttributes);

		return writer.ToPayloadData();
	}
}
