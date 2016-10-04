using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

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
		}

		// Starts a new conversation with the server by sending the first packet.
		public Task SendAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			m_sequenceId = 0;
			return DoSendAsync(payload, ioBehavior, cancellationToken);
		}

		// Starts a new conversation with the server by receiving the first packet.
		public ValueTask<PayloadData> ReceiveAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			m_sequenceId = 0;
			return DoReceiveAsync(ProtocolErrorBehavior.Throw, ioBehavior, cancellationToken);
		}

		// Continues a conversation with the server by receiving a response to a packet sent with 'Send' or 'SendReply'.
		public ValueTask<PayloadData> ReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
			=> DoReceiveAsync(ProtocolErrorBehavior.Throw, ioBehavior, cancellationToken);

		// Continues a conversation with the server by receiving a response to a packet sent with 'Send' or 'SendReply'.
		public ValueTask<PayloadData> TryReceiveReplyAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
			=> DoReceiveAsync(ProtocolErrorBehavior.Ignore, ioBehavior, cancellationToken);

		// Continues a conversation with the server by sending a reply to a packet received with 'Receive' or 'ReceiveReply'.
		public Task SendReplyAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
			=> DoSendAsync(payload, ioBehavior, cancellationToken);

		private async Task DoSendAsync(PayloadData payload, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			var bytesSent = 0;
			var data = payload.ArraySegment;
			int bytesToSend;
			do
			{
				// break payload into packets of at most (2^24)-1 bytes
				bytesToSend = Math.Min(data.Count - bytesSent, c_maxPacketSize);

				// write four-byte packet header; https://dev.mysql.com/doc/internals/en/mysql-packet.html
				SerializationUtility.WriteUInt32((uint) bytesToSend, m_buffer, 0, 3);
				m_buffer[3] = (byte) m_sequenceId;
				m_sequenceId++;

				if (bytesToSend <= m_buffer.Length - 4)
				{
					Buffer.BlockCopy(data.Array, data.Offset + bytesSent, m_buffer, 4, bytesToSend);
					var count = bytesToSend + 4;
					if (ioBehavior == IOBehavior.Asynchronous)
					{
						m_socketAwaitable.EventArgs.SetBuffer(0, count);
						await m_socket.SendAsync(m_socketAwaitable);
					}
					else
					{
						m_socket.Send(m_buffer, 0, count, SocketFlags.None);
					}
				}
				else
				{
					if (ioBehavior == IOBehavior.Asynchronous)
					{
						m_socketAwaitable.EventArgs.SetBuffer(null, 0, 0);
						m_socketAwaitable.EventArgs.BufferList = new[] { new ArraySegment<byte>(m_buffer, 0, 4), new ArraySegment<byte>(data.Array, data.Offset + bytesSent, bytesToSend) };
						await m_socket.SendAsync(m_socketAwaitable);
						m_socketAwaitable.EventArgs.BufferList = null;
						m_socketAwaitable.EventArgs.SetBuffer(m_buffer, 0, 0);
					}
					else
					{
						m_socket.Send(m_buffer, 0, 4, SocketFlags.None);
						m_socket.Send(data.Array, data.Offset + bytesSent, bytesToSend, SocketFlags.None);
					}
				}

				bytesSent += bytesToSend;
			} while (bytesToSend == c_maxPacketSize);
		}

		private ValueTask<PayloadData> DoReceiveAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (m_end - m_offset > 4)
			{
				int payloadLength = (int) SerializationUtility.ReadUInt32(m_buffer, m_offset, 3);
				if (m_end - m_offset >= payloadLength + 4)
				{
					if (m_buffer[m_offset + 3] != (byte) (m_sequenceId & 0xFF))
					{
						if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
							return new ValueTask<PayloadData>(default(PayloadData));
						throw new InvalidOperationException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(m_sequenceId & 0xFF, m_buffer[3]));
					}
					m_sequenceId++;
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
			if (m_buffer[m_offset + 3] != (byte) (m_sequenceId & 0xFF))
			{
				if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
					return null;
				throw new InvalidOperationException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(m_sequenceId & 0xFF, m_buffer[3]));
			}
			m_sequenceId++;
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
		int m_sequenceId;
		readonly byte[] m_buffer;
		int m_offset;
		int m_end;
	}
}
