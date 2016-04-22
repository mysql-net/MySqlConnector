using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MySql.Data.Serialization
{
	internal sealed class PacketTransmitter
	{
		public PacketTransmitter(Socket socket)
		{
			m_socket = socket;
			var socketEventArgs = new SocketAsyncEventArgs();
			m_buffer = new byte[4096];
			socketEventArgs.SetBuffer(m_buffer, 0, 0);
			m_socketAwaitable = new SocketAwaitable(socketEventArgs);
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
					Buffer.BlockCopy(data.Array, data.Offset, m_buffer, 4, bytesToSend);
					m_socketAwaitable.EventArgs.SetBuffer(0, bytesToSend + 4);
					await m_socket.SendAsync(m_socketAwaitable);
				}
				else
				{
					m_socketAwaitable.EventArgs.SetBuffer(null, 0, 0);
					m_socketAwaitable.EventArgs.BufferList = new[] { new ArraySegment<byte>(m_buffer, 0, 4), data };
					await m_socket.SendAsync(m_socketAwaitable);
					m_socketAwaitable.EventArgs.BufferList = null;
					m_socketAwaitable.EventArgs.SetBuffer(m_buffer, 0, 0);
				}

				bytesSent += bytesToSend;
			} while (bytesToSend == maxBytesToSend);
		}

		private async Task<PayloadData> DoReceiveAsync(CancellationToken cancellationToken, bool optional = false)
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
				m_socketAwaitable.EventArgs.SetBuffer(offset, count);
				await m_socket.ReceiveAsync(m_socketAwaitable);
				int bytesRead = m_socketAwaitable.EventArgs.BytesTransferred;
				if (bytesRead <= 0)
				{
					if (optional)
						return null;
					throw new EndOfStreamException();
				}
				offset += bytesRead;
				m_end += bytesRead;
				count -= bytesRead;
			}

			// decode packet header
			int payloadLength = (int) SerializationUtility.ReadUInt32(m_buffer, m_offset, 3);
			if (m_buffer[m_offset + 3] != (byte) (m_sequenceId & 0xFF))
			{
				if (optional)
					return null;
				throw new InvalidOperationException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(m_sequenceId & 0xFF, m_buffer[3]));
			}
			m_sequenceId++;
			m_offset += 4;

			if (m_end - m_offset >= payloadLength)
			{
				// check for error
				if (m_buffer[m_offset] == 0xFF)
				{
					var errorCode = (int) BitConverter.ToUInt16(m_buffer, m_offset + 1);
					var sqlState = Encoding.ASCII.GetString(m_buffer, m_offset + 4, 5);
					var message = Encoding.UTF8.GetString(m_buffer, m_offset + 9, payloadLength - 9);
					throw new MySqlException(errorCode, sqlState, message);
				}

				offset = m_offset;
				m_offset += payloadLength;
				return new PayloadData(new ArraySegment<byte>(m_buffer, offset, payloadLength));
			}

			// allocate a larger buffer if necessary
			var readData = m_buffer;
			if (payloadLength > m_buffer.Length)
			{
				readData = new byte[payloadLength];
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
				m_socketAwaitable.EventArgs.SetBuffer(offset, count);
				await m_socket.ReceiveAsync(m_socketAwaitable);
				int bytesRead = m_socketAwaitable.EventArgs.BytesTransferred;
				if (bytesRead <= 0)
					throw new EndOfStreamException();
				offset += bytesRead;
				m_end += bytesRead;
				count -= bytesRead;
			}

			// switch back to original buffer if a larger one was allocated
			if (payloadLength > m_buffer.Length)
			{
				m_socketAwaitable.EventArgs.SetBuffer(m_buffer, 0, 0);
				m_end = 0;
			}

			// check for error
			if (readData[0] == 0xFF)
			{
				var errorCode = (int) BitConverter.ToUInt16(readData, 1);
				var sqlState = Encoding.ASCII.GetString(readData, 4, 5);
				var message = Encoding.UTF8.GetString(readData, 9, payloadLength - 9);
				throw new MySqlException(errorCode, sqlState, message);
			}

			if (payloadLength <= m_buffer.Length)
				m_offset = payloadLength;

			return new PayloadData(new ArraySegment<byte>(readData, 0, payloadLength));
		}

		readonly Socket m_socket;
		readonly SocketAwaitable m_socketAwaitable;
		int m_sequenceId;
		readonly byte[] m_buffer;
		int m_offset;
		int m_end;
	}
}
