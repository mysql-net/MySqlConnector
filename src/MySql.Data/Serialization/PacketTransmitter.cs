using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using static System.FormattableString;

namespace MySql.Data.Serialization
{
	internal sealed class PacketTransmitter
	{
		public PacketTransmitter(Stream stream)
		{
			m_stream = stream;
			m_buffer = new byte[1024];
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

		// Continues a conversation with the server by receiving a response to a packet sent with 'Send' or 'SendReply'.
		public Task<PayloadData> TryReceiveReplyAsync(CancellationToken cancellationToken)
			=> DoReceiveAsync(cancellationToken, optional: true);

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

				if (bytesToSend <= m_buffer.Length - 4)
				{
					Array.Copy(data.Array, data.Offset, m_buffer, 4, bytesToSend);
					await m_stream.WriteAsync(m_buffer, 0, bytesToSend + 4, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					await m_stream.WriteAsync(m_buffer, 0, 4, cancellationToken).ConfigureAwait(false);
					await m_stream.WriteAsync(data.Array, data.Offset + bytesSent, bytesToSend, cancellationToken).ConfigureAwait(false);
				}

				bytesSent += bytesToSend;
			} while (bytesToSend == maxBytesToSend);
		}

		private async Task<PayloadData> DoReceiveAsync(CancellationToken cancellationToken, bool optional = false)
		{
			if (optional)
			{
				int bytesRead = await m_stream.ReadAvailableAsync(m_buffer, 0, 4, cancellationToken).ConfigureAwait(false);
				if (bytesRead < 4)
					return null;
			}
			else
			{
				await m_stream.ReadExactlyAsync(m_buffer, 0, 4, cancellationToken).ConfigureAwait(false);
			}
			int payloadLength = (int) SerializationUtility.ReadUInt32(m_buffer, 0, 3);
			if (m_buffer[3] != (byte) (m_sequenceId & 0xFF))
			{
				if (optional)
					return null;
				throw new InvalidOperationException(Invariant($"Packet received out-of-order. Expected {m_sequenceId & 0xFF}; got {m_buffer[3]}."));
			}
			m_sequenceId++;

			byte[] readData = m_buffer;
			if (payloadLength > m_buffer.Length)
				readData = new byte[payloadLength];

			await m_stream.ReadExactlyAsync(readData, 0, payloadLength, cancellationToken).ConfigureAwait(false);

			if (readData[0] == 0xFF)
			{
				var errorCode = (int) BitConverter.ToUInt16(readData, 1);
				var sqlState = Encoding.ASCII.GetString(readData, 4, 5);
				var message = Encoding.UTF8.GetString(readData, 9, payloadLength - 9);
				throw new MySqlException(errorCode, sqlState, message);
			}

			return new PayloadData(new ArraySegment<byte>(readData, 0, payloadLength));
		}

		readonly Stream m_stream;
		readonly byte[] m_buffer;
		int m_sequenceId;
	}
}
