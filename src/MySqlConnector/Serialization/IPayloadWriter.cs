using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace MySql.Data.Serialization
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
		ValueTask<ArraySegment<byte>> ReadBytesAsync(IOBehavior ioBehavior);
	}

	internal interface IPacketReader
	{
		ValueTask<Packet> ReadPacketAsync(IConversation conversation, IOBehavior ioBehavior);
	}

	internal interface IPayloadReader
	{
		ValueTask<ArraySegment<byte>> ReadPayloadAsync(IConversation conversation, IOBehavior ioBehavior);
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

		const int MaxPacketSize = 16777215;
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

	internal static class ValueTaskExtensions
	{
		public static ValueTask<TResult> ContinueWith<T, TResult>(this ValueTask<T> valueTask, Func<T, ValueTask<TResult>> continuation)
		{
			return valueTask.IsCompleted ? continuation(valueTask.Result) :
				new ValueTask<TResult>(valueTask.AsTask().ContinueWith(task => continuation(task.Result).AsTask()).Unwrap());
		}
	}
}
