using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Payloads;

internal static class HandshakeResponse41Payload
{
	private static ByteBufferWriter CreateCapabilitiesPayload(ProtocolCapabilities serverCapabilities, ConnectionSettings cs, CompressionMethod compressionMethod, CharacterSet characterSet, ProtocolCapabilities additionalCapabilities = 0)
	{
		var writer = new ByteBufferWriter();

		var clientCapabilities =
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
			(compressionMethod == CompressionMethod.Zlib ? ProtocolCapabilities.Compress : ProtocolCapabilities.None) |
			(serverCapabilities & ProtocolCapabilities.ConnectionAttributes) |
			(serverCapabilities & ProtocolCapabilities.SessionTrack) |
			(serverCapabilities & ProtocolCapabilities.DeprecateEof) |
			(compressionMethod == CompressionMethod.Zstandard ? ProtocolCapabilities.ZstandardCompressionAlgorithm : 0) |
			(serverCapabilities & ProtocolCapabilities.QueryAttributes) |
			(serverCapabilities & ProtocolCapabilities.MariaDbCacheMetadata) |
			additionalCapabilities;

		writer.Write((int) clientCapabilities);
		writer.Write(0x4000_0000);
		writer.Write((byte) characterSet);

		// NOTE: not new byte[19]; see https://github.com/dotnet/roslyn/issues/33088
		ReadOnlySpan<byte> padding = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
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

	public static PayloadData CreateWithSsl(ProtocolCapabilities serverCapabilities, ConnectionSettings cs, CompressionMethod compressionMethod, CharacterSet characterSet) =>
		CreateCapabilitiesPayload(serverCapabilities, cs, compressionMethod, characterSet, ProtocolCapabilities.Ssl).ToPayloadData();

	public static PayloadData Create(InitialHandshakePayload handshake, ConnectionSettings cs, string password, bool useCachingSha2, CompressionMethod compressionMethod, int? compressionLevel, CharacterSet characterSet, byte[]? connectionAttributes)
	{
		// TODO: verify server capabilities
		var writer = CreateCapabilitiesPayload(handshake.ProtocolCapabilities, cs, compressionMethod, characterSet);
		writer.WriteNullTerminatedString(cs.UserID);

		var authenticationResponse = useCachingSha2 ? AuthenticationUtility.CreateScrambleResponse(Utility.TrimZeroByte(handshake.AuthPluginData.AsSpan()), password) :
			AuthenticationUtility.CreateAuthenticationResponse(handshake.AuthPluginData, password);
		writer.Write((byte) authenticationResponse.Length);
		writer.Write(authenticationResponse);

		if (!string.IsNullOrWhiteSpace(cs.Database))
			writer.WriteNullTerminatedString(cs.Database);

		if ((handshake.ProtocolCapabilities & ProtocolCapabilities.PluginAuth) != 0)
			writer.Write(useCachingSha2 ? "caching_sha2_password\0"u8 : "mysql_native_password\0"u8);

		if (connectionAttributes is not null)
			writer.Write(connectionAttributes);

		// Zstandard compression level
		if (compressionMethod == CompressionMethod.Zstandard)
			writer.Write((byte) (compressionLevel ?? 10));

		return writer.ToPayloadData();
	}
}
