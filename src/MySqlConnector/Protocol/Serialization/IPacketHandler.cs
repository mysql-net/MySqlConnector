using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	internal interface IPacketHandler
	{
		void SetByteHandler(IByteHandler byteHandler);
		ValueTask<Packet> ReadPacketAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior);
		ValueTask<int> WritePacketAsync(Packet packet, IOBehavior ioBehavior);
	}
}
