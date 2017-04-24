using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
	internal static class ProtocolUtility
	{
		public static ValueTask<Packet> ReadPacketAsync(BufferedByteReader bufferedByteReader, IByteHandler byteHandler, Func<int> getNextSequenceNumber, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			var headerBytesTask = bufferedByteReader.ReadBytesAsync(byteHandler, 4, ioBehavior);
			if (headerBytesTask.IsCompleted)
				return ReadPacketAfterHeader(headerBytesTask.Result, bufferedByteReader, byteHandler, getNextSequenceNumber, protocolErrorBehavior, ioBehavior);
			return AddContinuation(headerBytesTask, bufferedByteReader, byteHandler, getNextSequenceNumber, protocolErrorBehavior, ioBehavior);

			// NOTE: use a local function (with no captures) to defer creation of lambda objects
			ValueTask<Packet> AddContinuation(ValueTask<ArraySegment<byte>> headerBytes_, BufferedByteReader bufferedByteReader_, IByteHandler byteHandler_, Func<int> getNextSequenceNumber_, ProtocolErrorBehavior protocolErrorBehavior_, IOBehavior ioBehavior_) =>
				headerBytes_.ContinueWith(x => ReadPacketAfterHeader(x, bufferedByteReader_, byteHandler_, getNextSequenceNumber_, protocolErrorBehavior_, ioBehavior_));
		}

		private static ValueTask<Packet> ReadPacketAfterHeader(ArraySegment<byte> headerBytes, BufferedByteReader bufferedByteReader, IByteHandler byteHandler, Func<int> getNextSequenceNumber, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			if (headerBytes.Count < 4)
			{
				return protocolErrorBehavior == ProtocolErrorBehavior.Throw ?
					ValueTaskExtensions.FromException<Packet>(new EndOfStreamException()) :
					default(ValueTask<Packet>);
			}

			var payloadLength = (int) SerializationUtility.ReadUInt32(headerBytes.Array, headerBytes.Offset, 3);
			int packetSequenceNumber = headerBytes.Array[headerBytes.Offset + 3];

			var expectedSequenceNumber = getNextSequenceNumber() % 256;
			if (expectedSequenceNumber != -1 && packetSequenceNumber != expectedSequenceNumber)
			{
				if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
					return default(ValueTask<Packet>);

				var exception = MySqlProtocolException.CreateForPacketOutOfOrder(expectedSequenceNumber, packetSequenceNumber);
				return ValueTaskExtensions.FromException<Packet>(exception);
			}

			var payloadBytesTask = bufferedByteReader.ReadBytesAsync(byteHandler, payloadLength, ioBehavior);
			if (payloadBytesTask.IsCompleted)
				return CreatePacketFromPayload(payloadBytesTask.Result, payloadLength, packetSequenceNumber, protocolErrorBehavior);
			return AddContinuation(payloadBytesTask, payloadLength, packetSequenceNumber, protocolErrorBehavior);

			// NOTE: use a local function (with no captures) to defer creation of lambda objects
			ValueTask<Packet> AddContinuation(ValueTask<ArraySegment<byte>> payloadBytesTask_, int payloadLength_, int packetSequenceNumber_, ProtocolErrorBehavior protocolErrorBehavior_)
				=> payloadBytesTask_.ContinueWith(x => CreatePacketFromPayload(x, payloadLength_, packetSequenceNumber_, protocolErrorBehavior_));
		}

		private static ValueTask<Packet> CreatePacketFromPayload(ArraySegment<byte> payloadBytes, int payloadLength, int packetSequenceNumber, ProtocolErrorBehavior protocolErrorBehavior) =>
			payloadBytes.Count >= payloadLength ? new ValueTask<Packet>(new Packet(packetSequenceNumber, payloadBytes)) :
				protocolErrorBehavior == ProtocolErrorBehavior.Throw ? ValueTaskExtensions.FromException<Packet>(new EndOfStreamException()) :
				default(ValueTask<Packet>);

		public static ValueTask<ArraySegment<byte>> ReadPayloadAsync(BufferedByteReader bufferedByteReader, IByteHandler byteHandler, Func<int> getNextSequenceNumber, ArraySegmentHolder<byte> cache, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			cache.Clear();
			return DoReadPayloadAsync(bufferedByteReader, byteHandler, getNextSequenceNumber, cache, protocolErrorBehavior, ioBehavior);
		}

		private static ValueTask<ArraySegment<byte>> DoReadPayloadAsync(BufferedByteReader bufferedByteReader, IByteHandler byteHandler, Func<int> getNextSequenceNumber, ArraySegmentHolder<byte> previousPayloads, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			var readPacketTask = ReadPacketAsync(bufferedByteReader, byteHandler, getNextSequenceNumber, protocolErrorBehavior, ioBehavior);
			while (readPacketTask.IsCompleted)
			{
				ValueTask<ArraySegment<byte>> result;
				if (HasReadPayload(previousPayloads, readPacketTask.Result, protocolErrorBehavior, out result))
					return result;

				readPacketTask = ReadPacketAsync(bufferedByteReader, byteHandler, getNextSequenceNumber, protocolErrorBehavior, ioBehavior);
			}

			return AddContinuation(readPacketTask, bufferedByteReader, byteHandler, getNextSequenceNumber, previousPayloads, protocolErrorBehavior, ioBehavior);

			// NOTE: use a local function (with no captures) to defer creation of lambda objects
			ValueTask<ArraySegment<byte>> AddContinuation(ValueTask<Packet> readPacketTask_, BufferedByteReader bufferedByteReader_, IByteHandler byteHandler_, Func<int> getNextSequenceNumber_, ArraySegmentHolder<byte> previousPayloads_, ProtocolErrorBehavior protocolErrorBehavior_, IOBehavior ioBehavior_)
			{
				return readPacketTask_.ContinueWith(packet =>
					HasReadPayload(previousPayloads_, packet, protocolErrorBehavior_, out var result_) ? result_ :
						DoReadPayloadAsync(bufferedByteReader_, byteHandler_, getNextSequenceNumber_, previousPayloads_, protocolErrorBehavior_, ioBehavior_));
			}
		}

		private static bool HasReadPayload(ArraySegmentHolder<byte> previousPayloads, Packet packet, ProtocolErrorBehavior protocolErrorBehavior, out ValueTask<ArraySegment<byte>> result)
		{
			if (packet == null && protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
			{
				result = default(ValueTask<ArraySegment<byte>>);
				return true;
			}

			if (previousPayloads.Count == 0 && packet.Contents.Count < MaxPacketSize)
			{
				result = new ValueTask<ArraySegment<byte>>(packet.Contents);
				return true;
			}

			var previousPayloadsArray = previousPayloads.Array;
			if (previousPayloadsArray == null)
				previousPayloadsArray = new byte[ProtocolUtility.MaxPacketSize + 1];
			else if (previousPayloads.Offset + previousPayloads.Count + packet.Contents.Count > previousPayloadsArray.Length)
				Array.Resize(ref previousPayloadsArray, previousPayloadsArray.Length * 2);

			Buffer.BlockCopy(packet.Contents.Array, packet.Contents.Offset, previousPayloadsArray, previousPayloads.Offset + previousPayloads.Count, packet.Contents.Count);
			previousPayloads.ArraySegment = new ArraySegment<byte>(previousPayloadsArray, previousPayloads.Offset, previousPayloads.Count + packet.Contents.Count);

			if (packet.Contents.Count < ProtocolUtility.MaxPacketSize)
			{
				result = new ValueTask<ArraySegment<byte>>(previousPayloads.ArraySegment);
				return true;
			}

			result = default(ValueTask<ArraySegment<byte>>);
			return false;
		}

		public static ValueTask<int> WritePayloadAsync(IByteHandler byteHandler, Func<int> getNextSequenceNumber, ArraySegment<byte> payload, IOBehavior ioBehavior)
		{
			return payload.Count <= MaxPacketSize ? WritePacketAsync(byteHandler, getNextSequenceNumber(), payload, ioBehavior) :
				CreateTask(byteHandler, getNextSequenceNumber, payload, ioBehavior);

			// NOTE: use a local function (with no captures) to defer creation of lambda objects
			ValueTask<int> CreateTask(IByteHandler byteHandler_, Func<int> getNextSequenceNumber_, ArraySegment<byte> payload_, IOBehavior ioBehavior_)
			{
				var writeTask = default(ValueTask<int>);
				for (var bytesSent = 0; bytesSent < payload_.Count; bytesSent += MaxPacketSize)
				{
					var contents = new ArraySegment<byte>(payload_.Array, payload_.Offset + bytesSent, Math.Min(MaxPacketSize, payload_.Count - bytesSent));
					writeTask = writeTask.ContinueWith(x => WritePacketAsync(byteHandler_, getNextSequenceNumber_(), contents, ioBehavior_));
				}
				return writeTask;
			}
		}

		public static ValueTask<int> WritePacketAsync(IByteHandler byteHandler, int sequenceNumber, ArraySegment<byte> contents, IOBehavior ioBehavior)
		{
			var bufferLength = contents.Count + 4;
			var buffer = ArrayPool<byte>.Shared.Rent(bufferLength);
			SerializationUtility.WriteUInt32((uint) contents.Count, buffer, 0, 3);
			buffer[3] = (byte) sequenceNumber;
			Buffer.BlockCopy(contents.Array, contents.Offset, buffer, 4, contents.Count);
			var task = byteHandler.WriteBytesAsync(new ArraySegment<byte>(buffer, 0, bufferLength), ioBehavior);
			if (task.IsCompletedSuccessfully)
			{
				ArrayPool<byte>.Shared.Return(buffer);
				return default(ValueTask<int>);
			}
			return AddContinuation(task, buffer);

			// NOTE: use a local function (with no captures) to defer creation of lambda objects
			ValueTask<int> AddContinuation(ValueTask<int> task_, byte[] buffer_)
			{
				return task_.ContinueWith(x =>
				{
					ArrayPool<byte>.Shared.Return(buffer_);
					return default(ValueTask<int>);
				});
			}
		}

		public const int MaxPacketSize = 16777215;
	}
}
