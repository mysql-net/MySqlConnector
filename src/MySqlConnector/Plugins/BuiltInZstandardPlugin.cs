#if NET11_0_OR_GREATER
using System.IO.Compression;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Plugins;

/// <summary>
/// A <see cref="ZstandardPlugin"/> that uses the Zstandard implementation built in to .NET 11 and later. This is registered
/// automatically, so the MySqlConnector.Zstandard package isn't needed to use <c>Compress=True</c> with a server that
/// supports the <c>zstd</c> compression algorithm.
/// </summary>
internal sealed class BuiltInZstandardPlugin : ZstandardPlugin
{
	public static BuiltInZstandardPlugin Instance { get; } = new();

	public override IPayloadHandler CreatePayloadHandler(IByteHandler byteHandler) => new CompressedPayloadHandler(byteHandler, isZstandard: true);

	// This is sent to the server in the handshake response, and is the level libmysqlclient uses by default; it's also the
	// level ZstandardEncoder.TryCompress uses, so both ends of the connection compress at the same level.
	public override int CompressionLevel => ZstandardCompressionOptions.DefaultQuality;

	private BuiltInZstandardPlugin()
	{
	}
}
#endif
