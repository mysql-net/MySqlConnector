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
			var socketEventArgs = new SocketAsyncEventArgs();
			m_buffer = new byte[c_netBufferLength];
			socketEventArgs.SetBuffer(m_buffer, 0, 0);
			m_socketAwaitable = new SocketAwaitable(socketEventArgs);

			var socketByteWriter = new SocketByteWriter(m_socket);
			var packetWriter = new PacketWriter(socketByteWriter);
			m_payloadWriter = new PacketFormatter(packetWriter);
			m_conversation = new Conversation();
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
			if (m_end - m_offset > 4)
			{
				int payloadLength = (int) SerializationUtility.ReadUInt32(m_buffer, m_offset, 3);
				if (m_end - m_offset >= payloadLength + 4)
				{
					var sequenceId = m_conversation.GetNextSequenceNumber();
					if (m_buffer[m_offset + 3] != (byte) (sequenceId & 0xFF))
					{
						if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
							return new ValueTask<PayloadData>(default(PayloadData));
						throw new InvalidOperationException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(sequenceId & 0xFF, m_buffer[3]));
					}
					m_offset += 4;

					var offset = m_offset;
					m_offset += payloadLength;

					return new ValueTask<PayloadData>(new PayloadData(new ArraySegment<byte>(m_buffer, offset, payloadLength)));
				}
			}

			return new ValueTask<PayloadData>(DoReceiveAsync2(protocolErrorBehavior, ioBehavior, cancellationToken));
		}

		private async Task<PayloadData> DoReceiveAsync2(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			// common case: the payload is contained within one packet
			var payload = await ReceivePacketAsync(protocolErrorBehavior, ioBehavior, cancellationToken).ConfigureAwait(false);
			if (payload == null || payload.ArraySegment.Count != c_maxPacketSize)
				return payload;

			// concatenate all the data, starting with the array from the first payload (ASSUME: we can take ownership of this array)
			if (payload.ArraySegment.Offset != 0 || payload.ArraySegment.Count != payload.ArraySegment.Array.Length)
				throw new InvalidOperationException("Expected to be able to reuse underlying array");
			var payloadBytes = payload.ArraySegment.Array;

			do
			{
				payload = await ReceivePacketAsync(protocolErrorBehavior, ioBehavior, cancellationToken).ConfigureAwait(false);

				var oldLength = payloadBytes.Length;
				Array.Resize(ref payloadBytes, payloadBytes.Length + payload.ArraySegment.Count);
				Buffer.BlockCopy(payload.ArraySegment.Array, payload.ArraySegment.Offset, payloadBytes, oldLength, payload.ArraySegment.Count);
			} while (payload.ArraySegment.Count == c_maxPacketSize);

			return new PayloadData(new ArraySegment<byte>(payloadBytes));
		}

		private async Task<PayloadData> ReceivePacketAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (m_end - m_offset < 4)
			{
				if (m_end - m_offset > 0)
					Buffer.BlockCopy(m_buffer, m_offset, m_buffer, 0, m_end - m_offset);
				m_end -= m_offset;
				m_offset = 0;
			}

			// read packet header
			int offset = m_end;
			int count = m_buffer.Length - m_end;
			while (m_end - m_offset < 4)
			{
				int bytesRead;
				if (ioBehavior == IOBehavior.Asynchronous)
				{
					m_socketAwaitable.EventArgs.SetBuffer(offset, count);
					await m_socket.ReceiveAsync(m_socketAwaitable);
					bytesRead = m_socketAwaitable.EventArgs.BytesTransferred;
				}
				else
				{
					bytesRead = m_socket.Receive(m_buffer, offset, count, SocketFlags.None);
				}

				if (bytesRead <= 0)
				{
					if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
						return null;
					throw new EndOfStreamException();
				}
				offset += bytesRead;
				m_end += bytesRead;
				count -= bytesRead;
			}

			// decode packet header
			int payloadLength = (int) SerializationUtility.ReadUInt32(m_buffer, m_offset, 3);
			var sequenceId = m_conversation.GetNextSequenceNumber();
			if (m_buffer[m_offset + 3] != (byte) (sequenceId & 0xFF))
			{
				if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
					return null;
				throw new InvalidOperationException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(sequenceId & 0xFF, m_buffer[3]));
			}
			m_offset += 4;

			if (m_end - m_offset >= payloadLength)
			{
				offset = m_offset;
				m_offset += payloadLength;
				return new PayloadData(new ArraySegment<byte>(m_buffer, offset, payloadLength));
			}

			// allocate a larger buffer if necessary
			var readData = m_buffer;
			if (payloadLength > m_buffer.Length)
			{
				readData = new byte[payloadLength];
				if (ioBehavior == IOBehavior.Asynchronous)
					m_socketAwaitable.EventArgs.SetBuffer(readData, 0, 0);
			}
			Buffer.BlockCopy(m_buffer, m_offset, readData, 0, m_end - m_offset);
			m_end -= m_offset;
			m_offset = 0;

			// read payload
			offset = m_end;
			count = readData.Length - m_end;
			while (m_end < payloadLength)
			{
				int bytesRead;
				if (ioBehavior == IOBehavior.Asynchronous)
				{
					m_socketAwaitable.EventArgs.SetBuffer(offset, count);
					await m_socket.ReceiveAsync(m_socketAwaitable);
					bytesRead = m_socketAwaitable.EventArgs.BytesTransferred;
				}
				else
				{
					bytesRead = m_socket.Receive(readData, offset, count, SocketFlags.None);
				}
				if (bytesRead <= 0)
					throw new EndOfStreamException();
				offset += bytesRead;
				m_end += bytesRead;
				count -= bytesRead;
			}

			// switch back to original buffer if a larger one was allocated
			if (payloadLength > m_buffer.Length)
			{
				if (ioBehavior == IOBehavior.Asynchronous)
					m_socketAwaitable.EventArgs.SetBuffer(m_buffer, 0, 0);
				m_end = 0;
			}

			if (payloadLength <= m_buffer.Length)
				m_offset = payloadLength;

			return new PayloadData(new ArraySegment<byte>(readData, 0, payloadLength));
		}

		const int c_maxPacketSize = 16777215;
		const int c_netBufferLength = 16384;

		readonly Socket m_socket;
		readonly SocketAwaitable m_socketAwaitable;
		readonly byte[] m_buffer;
		int m_offset;
		int m_end;
		private PacketFormatter m_payloadWriter;
		private Conversation m_conversation;
	}
}
