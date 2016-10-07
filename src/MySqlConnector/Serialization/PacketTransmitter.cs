using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Protocol.Serialization;

namespace MySql.Data.Serialization
{
	internal sealed class PacketTransmitter
	{
		public PacketTransmitter(Socket socket)
		{
			m_socket = socket;

			var socketByteWriter = new SocketByteWriter(m_socket);
			var packetWriter = new PacketWriter(socketByteWriter);
			m_payloadWriter = new PacketFormatter(packetWriter);
			m_conversation = new Conversation();

			var socketByteReader = new SocketByteReader(m_socket);
			var packetReader = new PacketReader(socketByteReader);
			m_payloadReader = new PayloadReader(packetReader);
		}

		// Starts a new conversation with the server by sending the first packet.
		public ValueTask<int> SendAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			m_conversation.Reset();
			return DoSendAsync(payload, ioBehavior, cancellationToken);
		}

		// Starts a new conversation with the server by receiving the first packet.
		public ValueTask<PayloadData> ReceiveAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			m_conversation.Reset();
			return DoReceiveAsync(ProtocolErrorBehavior.Throw, ioBehavior, cancellationToken);
		}

		// Continues a conversation with the server by receiving a response to a packet sent with 'Send' or 'SendReply'.
		public ValueTask<PayloadData> ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
			=> DoReceiveAsync(ProtocolErrorBehavior.Throw, ioBehavior, cancellationToken);

		// Continues a conversation with the server by receiving a response to a packet sent with 'Send' or 'SendReply'.
		public ValueTask<PayloadData> TryReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
			=> DoReceiveAsync(ProtocolErrorBehavior.Ignore, ioBehavior, cancellationToken);

		// Continues a conversation with the server by sending a reply to a packet received with 'Receive' or 'ReceiveReply'.
		public ValueTask<int> SendReplyAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
			=> DoSendAsync(payload, ioBehavior, cancellationToken);

		private ValueTask<int> DoSendAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			return m_payloadWriter.WritePayloadAsync(m_conversation, payload.ArraySegment, ioBehavior);
		}

		private ValueTask<PayloadData> DoReceiveAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			return m_payloadReader.ReadPayloadAsync(m_conversation, protocolErrorBehavior, ioBehavior)
				.ContinueWith(x => new ValueTask<PayloadData>(new PayloadData(x)));
		}

		const int c_netBufferLength = 16384;

		readonly Socket m_socket;
		private PacketFormatter m_payloadWriter;
		private Conversation m_conversation;
		private PayloadReader m_payloadReader;
	}
}
