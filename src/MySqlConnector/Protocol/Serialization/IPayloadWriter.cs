using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
	enum FlushBehavior
	{
		Flush,
		Buffer,
	}

	internal interface IConversation
	{
		int GetNextSequenceNumber();
	}

	internal class Conversation : IConversation
	{
		public int GetNextSequenceNumber() => m_sequenceNumber++;

		public void Reset() => m_sequenceNumber = 0;

		private int m_sequenceNumber;
	}

	internal class Packet
	{
		public Packet(int sequenceNumber, ArraySegment<byte> contents)
		{
			SequenceNumber = sequenceNumber;
			Contents = contents;
		}

		public int SequenceNumber { get; }
		public ArraySegment<byte> Contents { get; }
	}

    internal interface IPayloadWriter
    {
	    ValueTask<int> WritePayloadAsync(IConversation conversation, ArraySegment<byte> payload, IOBehavior ioBehavior);
    }

	internal interface IPacketWriter
	{
	    ValueTask<int> WritePacketAsync(Packet packet, IOBehavior ioBehavior, FlushBehavior flushBehavior);
	}

	internal interface IByteWriter
	{
	    ValueTask<int> WriteBytesAsync(ArraySegment<byte> payload, IOBehavior ioBehavior);
	}

	internal interface IByteReader
	{
		ValueTask<int> ReadBytesAsync(byte[] buffer, int offset, int count, IOBehavior ioBehavior);
	}

	internal interface IPacketReader
	{
		ValueTask<Packet> ReadPacketAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior);
	}

	internal interface IPayloadReader
	{
		ValueTask<ArraySegment<byte>> ReadPayloadAsync(IConversation conversation, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior);
	}

	internal class PacketFormatter : IPayloadWriter
	{
		private readonly IPacketWriter m_packetWriter;

		public PacketFormatter(IPacketWriter packetWriter)
		{
			m_packetWriter = packetWriter;
		}

		public ValueTask<int> WritePayloadAsync(IConversation conversation, ArraySegment<byte> payload, IOBehavior ioBehavior)
		{
			if (payload.Count <= MaxPacketSize)
				return m_packetWriter.WritePacketAsync(new Packet(conversation.GetNextSequenceNumber(), payload), ioBehavior, FlushBehavior.Flush);

			var writeTask = default(ValueTask<int>);
			for (var bytesSent = 0; bytesSent < payload.Count; bytesSent += MaxPacketSize)
			{
				var contents = new ArraySegment<byte>(payload.Array, payload.Offset + bytesSent, Math.Min(MaxPacketSize, payload.Count - bytesSent));
				var flushBehavior = contents.Offset + contents.Count == payload.Offset + payload.Count ? FlushBehavior.Flush : FlushBehavior.Buffer;
				writeTask = writeTask.ContinueWith(x => m_packetWriter.WritePacketAsync(new Packet(conversation.GetNextSequenceNumber(), contents), ioBehavior, flushBehavior));
			}
			return writeTask;
		}

		public const int MaxPacketSize = 16777215;
	}

	internal class PayloadReader : IPayloadReader
	{
		private readonly IPacketReader m_packetReader;

		public PayloadReader(IPacketReader packetReader)
		{
			m_packetReader = packetReader;
		}

		public ValueTask<ArraySegment<byte>> ReadPayloadAsync(IConversation conversation, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior) =>
			ReadPayloadAsync(default(ArraySegment<byte>), conversation, protocolErrorBehavior, ioBehavior);

		private ValueTask<ArraySegment<byte>> ReadPayloadAsync(ArraySegment<byte> previousPayloads, IConversation conversation, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			return m_packetReader.ReadPacketAsync(protocolErrorBehavior, ioBehavior).ContinueWith(packet =>
				Continue(previousPayloads, packet, conversation, protocolErrorBehavior, ioBehavior));
		}

		private ValueTask<ArraySegment<byte>> Continue(ArraySegment<byte> previousPayloads, Packet packet, IConversation conversation, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			if (packet == null && protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
				return default(ValueTask<ArraySegment<byte>>);

			var sequenceNumber = conversation.GetNextSequenceNumber() % 256;
			if (packet.SequenceNumber != sequenceNumber)
			{
				if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
					return default(ValueTask<ArraySegment<byte>>);

				var exception = new InvalidOperationException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(sequenceNumber, packet.SequenceNumber));
				return ValueTaskExtensions.FromException<ArraySegment<byte>>(exception);
			}

			if (packet.Contents.Count < PacketFormatter.MaxPacketSize)
				return new ValueTask<ArraySegment<byte>>(packet.Contents);

			var previousPayloadsArray = previousPayloads.Array;
			if (previousPayloadsArray == null)
				previousPayloadsArray = new byte[PacketFormatter.MaxPacketSize + 1];
			else
				Array.Resize(ref previousPayloadsArray, previousPayloadsArray.Length * 2);

			Buffer.BlockCopy(packet.Contents.Array, packet.Contents.Offset, previousPayloadsArray, previousPayloads.Offset + previousPayloads.Count, packet.Contents.Count);
			previousPayloads = new ArraySegment<byte>(previousPayloadsArray, previousPayloads.Offset, previousPayloads.Count + packet.Contents.Count);

			return ReadPayloadAsync(previousPayloads, conversation, protocolErrorBehavior, ioBehavior);
		}
	}

	internal class PacketWriter : IPacketWriter
	{
		private readonly IByteWriter m_byteWriter;

		public PacketWriter(IByteWriter byteWriter)
		{
			m_byteWriter = byteWriter;
		}

		public ValueTask<int> WritePacketAsync(Packet packet, IOBehavior ioBehavior, FlushBehavior flushBehavior)
		{
			var packetLength = packet.Contents.Count;
			var bufferLength = packetLength + 4;
			var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
			SerializationUtility.WriteUInt32((uint) packetLength, buffer, 0, 3);
			buffer[3] = (byte) packet.SequenceNumber;
			Buffer.BlockCopy(packet.Contents.Array, packet.Contents.Offset, buffer, 4, packetLength);
			return m_byteWriter.WriteBytesAsync(new ArraySegment<byte>(buffer, 0, bufferLength), ioBehavior)
				.ContinueWith(x =>
				{
					ArrayPool<byte>.Shared.Return(buffer);
					return default(ValueTask<int>);
				});
		}
	}

	internal class PacketReader : IPacketReader
	{
		private readonly IByteReader m_byteReader;
		private readonly byte[] m_buffer;

		public PacketReader(IByteReader byteReader)
		{
			m_byteReader = byteReader;
			m_buffer = new byte[16384];
		}

		public ValueTask<Packet> ReadPacketAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			return ReadBytesAsync(m_buffer, 0, 4, ioBehavior)
				.ContinueWith(headerBytesRead =>
				{
					if (headerBytesRead < 4)
					{
						return protocolErrorBehavior == ProtocolErrorBehavior.Throw ?
							ValueTaskExtensions.FromException<Packet>(new EndOfStreamException()) :
							default(ValueTask<Packet>);
					}

					var payloadLength = (int) SerializationUtility.ReadUInt32(m_buffer, 0, 3);
					int sequenceNumber = m_buffer[3];

					var buffer = payloadLength <= m_buffer.Length ? m_buffer : new byte[payloadLength];
					return ReadBytesAsync(buffer, 0, payloadLength, ioBehavior)
						.ContinueWith(payloadBytesRead =>
						{
							if (payloadBytesRead < payloadLength)
							{
								return protocolErrorBehavior == ProtocolErrorBehavior.Throw ?
									ValueTaskExtensions.FromException<Packet>(new EndOfStreamException()) :
									default(ValueTask<Packet>);
							}

							return new ValueTask<Packet>(new Packet(sequenceNumber, new ArraySegment<byte>(buffer, 0, payloadLength)));
						});
				});
		}

		private ValueTask<int> ReadBytesAsync(byte[] buffer, int offset, int count, IOBehavior ioBehavior)
		{
			return m_byteReader.ReadBytesAsync(buffer, offset, count, ioBehavior)
				.ContinueWith(bytesRead =>
				{
					if (bytesRead == 0 || bytesRead == count)
						return new ValueTask<int>(bytesRead);

					return ReadBytesAsync(buffer, offset + bytesRead, count - bytesRead, ioBehavior);
				});
		}
	}

	internal class SocketByteWriter : IByteWriter
	{
		private readonly Socket m_socket;
		private readonly SocketAwaitable m_socketAwaitable;

		public SocketByteWriter(Socket socket)
		{
			m_socket = socket;
			var socketEventArgs = new SocketAsyncEventArgs();
			m_socketAwaitable = new SocketAwaitable(socketEventArgs);
		}

		public ValueTask<int> WriteBytesAsync(ArraySegment<byte> payload, IOBehavior ioBehavior)
		{
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				return new ValueTask<int>(DoWriteBytesAsync(payload));
			}
			else
			{
				m_socket.Send(payload.Array, payload.Offset, payload.Count, SocketFlags.None);
				return default(ValueTask<int>);
			}
		}

		private async Task<int> DoWriteBytesAsync(ArraySegment<byte> payload)
		{
			m_socketAwaitable.EventArgs.SetBuffer(payload.Array, payload.Offset, payload.Count);
			await m_socket.SendAsync(m_socketAwaitable);
			return 0;
		}
	}

	internal class SocketByteReader : IByteReader
	{
		private readonly Socket m_socket;
		private readonly SocketAwaitable m_socketAwaitable;

		public SocketByteReader(Socket socket)
		{
			m_socket = socket;
			var socketEventArgs = new SocketAsyncEventArgs();
			m_socketAwaitable = new SocketAwaitable(socketEventArgs);
		}

		public ValueTask<int> ReadBytesAsync(byte[] buffer, int offset, int count, IOBehavior ioBehavior)
		{
			return ioBehavior == IOBehavior.Asynchronous ?
				new ValueTask<int>(DoReadBytesAsync(buffer, offset, count)) :
				new ValueTask<int>(m_socket.Receive(buffer, offset, count, SocketFlags.None));
		}

		public async Task<int> DoReadBytesAsync(byte[] buffer, int offset, int count)
		{
			m_socketAwaitable.EventArgs.SetBuffer(buffer, offset, count);
			await m_socket.ReceiveAsync(m_socketAwaitable);
			return m_socketAwaitable.EventArgs.BytesTransferred;
		}
	}

	internal static class ValueTaskExtensions
	{
		public static ValueTask<TResult> ContinueWith<T, TResult>(this ValueTask<T> valueTask, Func<T, ValueTask<TResult>> continuation)
		{
			return valueTask.IsCompleted ? continuation(valueTask.Result) :
				new ValueTask<TResult>(valueTask.AsTask().ContinueWith(task => continuation(task.Result).AsTask()).Unwrap());
		}

		public static ValueTask<T> FromException<T>(Exception exception)
		{
			var tcs = new TaskCompletionSource<T>();
			tcs.SetException(exception);
			return new ValueTask<T>(tcs.Task);
		}
	}
}
