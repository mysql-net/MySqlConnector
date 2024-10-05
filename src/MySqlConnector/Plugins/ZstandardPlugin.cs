using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Plugins;

internal abstract class ZstandardPlugin
{
	public abstract IPayloadHandler CreatePayloadHandler(IByteHandler byteHandler);
	public abstract int CompressionLevel { get; }
}
