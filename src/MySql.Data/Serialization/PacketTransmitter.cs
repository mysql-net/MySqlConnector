using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.Serialization
{
	internal sealed class PacketTransmitter
	{
		public PacketTransmitter(Stream stream)
		{
			m_stream = stream;
			m_buffer = new byte[256];
		}

		// Starts a new conversation with the server by sending the first packet.
		public Task SendAsync(PayloadData payload, CancellationToken cancellationToken)
		{
			m_sequenceId = 0;
			return DoSendAsync(payload, cancellationToken);
		}

		// Starts a new conversation with the server by receiving the first packet.
		public Task<PayloadData> ReceiveAsync(CancellationToken cancellationToken)
		{
			m_sequenceId = 0;
			return DoReceiveAsync(cancellationToken);
		}

		// Continues a conversation with the server by receiving a response to a packet sent with 'Send' or 'SendReply'.
		public Task<PayloadData> ReceiveReplyAsync(CancellationToken cancellationToken)
			=> DoReceiveAsync(cancellationToken);

		// Continues a conversation with the server by sending a reply to a packet received with 'Receive' or 'ReceiveReply'.
		public Task SendReplyAsync(PayloadData payload, CancellationToken cancellationToken)
			=> DoSendAsync(payload, cancellationToken);

		private async Task DoSendAsync(PayloadData payload, CancellationToken cancellationToken)
		{
			var bytesSent = 0;
			var data = payload.ArraySegment;
			const int maxBytesToSend = 16777215;
			int bytesToSend;
			do
			{
				// break payload into packets of at most (2^24)-1 bytes
				bytesToSend = Math.Min(data.Count - bytesSent, maxBytesToSend);

				// write four-byte packet header; https://dev.mysql.com/doc/internals/en/mysql-packet.html
				SerializationUtility.WriteUInt32((uint) bytesToSend, m_buffer, 0, 3);
				m_buffer[3] = (byte) m_sequenceId;
				m_sequenceId++;
				await m_stream.WriteAsync(m_buffer, 0, 4, cancellationToken);

				// write payload
				await m_stream.WriteAsync(data.Array, data.Offset + bytesSent, bytesToSend, cancellationToken);

				bytesSent += bytesToSend;
			} while (bytesToSend == maxBytesToSend);
		}

		private async Task<PayloadData> DoReceiveAsync(CancellationToken cancellationToken)
		{
			await m_stream.ReadExactlyAsync(m_buffer, 0, 4, cancellationToken);
			int payloadLength = (int) SerializationUtility.ReadUInt32(m_buffer, 0, 3);
			if (m_buffer[3] != (byte) (m_sequenceId & 0xFF))
				throw new InvalidOperationException("Packet received out-of-order.");
			m_sequenceId++;
			if (payloadLength > m_buffer.Length)
				throw new NotSupportedException("TODO: Can't read long payloads.");
			await m_stream.ReadExactlyAsync(m_buffer, 0, payloadLength, cancellationToken);
			return new PayloadData(new ArraySegment<byte>(m_buffer, 0, payloadLength));
		}

		readonly Stream m_stream;
		readonly byte[] m_buffer;
		int m_sequenceId;
	}
}
