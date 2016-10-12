using System;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
	internal interface IPayloadHandler
	{
		ValueTask<ArraySegment<byte>> ReadPayloadAsync(IConversation conversation, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior);
		ValueTask<int> WritePayloadAsync(IConversation conversation, ArraySegment<byte> payload, IOBehavior ioBehavior);
	}
}
