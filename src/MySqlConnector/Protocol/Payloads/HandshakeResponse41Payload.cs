using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads;

internal static class HandshakeResponse41Payload
{
	public static ProtocolCapabilities CreateClientCapabilities(ProtocolCapabilities serverCapabilities, ConnectionSettings cs, bool useCompression, ProtocolCapabilities additionalCapabilities = 0) =>
		(ProtocolCapabilities.Protocol41 |
		                          (cs.InteractiveSession ? ProtocolCapabilities.Interactive : 0) |
		                          ProtocolCapabilities.LongPassword |
		                          ProtocolCapabilities.Transactions |
		                          ProtocolCapabilities.SecureConnection |
		                          ProtocolCapabilities.PluginAuth |
		                          ProtocolCapabilities.PluginAuthLengthEncodedClientData |
		                          ProtocolCapabilities.MultiStatements |
		                          ProtocolCapabilities.MultiResults |
		                          ProtocolCapabilities.PreparedStatementMultiResults |
		                          (cs.AllowLoadLocalInfile ? ProtocolCapabilities.LocalFiles : 0) |
		                          (string.IsNullOrWhiteSpace(cs.Database)
			                          ? 0
			                          : ProtocolCapabilities.ConnectWithDatabase) |
		                          (cs.UseAffectedRows ? 0 : ProtocolCapabilities.FoundRows) |
		                          (useCompression ? ProtocolCapabilities.Compress : ProtocolCapabilities.None) |
		                          ProtocolCapabilities.ConnectionAttributes |
		                          ProtocolCapabilities.SessionTrack |

		                          // ProtocolCapabilities.DeprecateEof |
		                          ProtocolCapabilities.QueryAttributes |
		                          ProtocolCapabilities.MariaDbComMulti |
		                          ProtocolCapabilities.MariaDbCacheMetadata |
		                          additionalCapabilities) & serverCapabilities;

	private static ByteBufferWriter CreateCapabilitiesPayload(ProtocolCapabilities clientCapabilities, CharacterSet characterSet)
	{
		var writer = new ByteBufferWriter();
		writer.Write((int) clientCapabilities);
		writer.Write(0x4000_0000);
		writer.Write((byte) characterSet);

		// NOTE: not new byte[19]; see https://github.com/dotnet/roslyn/issues/33088
		ReadOnlySpan<byte> padding = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		writer.Write(padding);

		if ((clientCapabilities & ProtocolCapabilities.LongPassword) == 0)
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

	public static PayloadData CreateWithSsl(ProtocolCapabilities clientCapabilities, CharacterSet characterSet) =>
		CreateCapabilitiesPayload(clientCapabilities, characterSet).ToPayloadData();

	public static PayloadData Create(InitialHandshakePayload handshake, ProtocolCapabilities clientCapabilities, ConnectionSettings cs, string password, CharacterSet characterSet, byte[]? connectionAttributes)
	{
		// TODO: verify server capabilities
		var writer = CreateCapabilitiesPayload(clientCapabilities, characterSet);
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
