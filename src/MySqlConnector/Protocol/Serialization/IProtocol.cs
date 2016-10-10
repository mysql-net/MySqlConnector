using System;
using System.Buffers;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
	public enum FlushBehavior
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

		public void StartNew() => m_sequenceNumber = 0;

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

    internal interface IPayloadHandler
    {
	    ValueTask<ArraySegment<byte>> ReadPayloadAsync(IConversation conversation, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior);

	    ValueTask<int> WritePayloadAsync(IConversation conversation, ArraySegment<byte> payload, IOBehavior ioBehavior);
    }

	internal interface IPacketHandler
	{
		ValueTask<Packet> ReadPacketAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior);

		ValueTask<int> WritePacketAsync(Packet packet, IOBehavior ioBehavior, FlushBehavior flushBehavior);
	}

	internal interface IByteHandler
	{
		ValueTask<int> ReadBytesAsync(byte[] buffer, int offset, int count, IOBehavior ioBehavior);

		ValueTask<int> WriteBytesAsync(ArraySegment<byte> payload, IOBehavior ioBehavior);
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
