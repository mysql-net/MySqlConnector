using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Plugins;

internal abstract class ZstandardPlugin
{
	/// <summary>
	/// The plugin to use when one hasn't been explicitly configured. This is <c>BuiltInZstandardPlugin</c> under .NET 11
	/// (which has a built-in Zstandard implementation) and <c>null</c> otherwise, which disables Zstandard compression unless
	/// the MySqlConnector.Zstandard package registers a plugin.
	/// </summary>
	public static ZstandardPlugin? Default =>
#if NET11_0_OR_GREATER
		BuiltInZstandardPlugin.Instance;
#else
		null;
#endif

	public abstract IPayloadHandler CreatePayloadHandler(IByteHandler byteHandler);
	public abstract int CompressionLevel { get; }
}
