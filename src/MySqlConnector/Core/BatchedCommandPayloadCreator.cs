using System;
using System.Buffers.Binary;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core
{
	internal sealed class BatchedCommandPayloadCreator : ICommandPayloadCreator
	{
		public static ICommandPayloadCreator Instance { get; } = new BatchedCommandPayloadCreator();

		public bool WriteQueryCommand(ref CommandListPosition commandListPosition, ByteBufferWriter writer)
		{
			writer.Write((byte) CommandKind.Multi);
			bool? firstResult = default;
			bool wroteCommand;
			do
			{
				// save room for command length
				var position = writer.Position;
				writer.Write(Padding);

				wroteCommand = SingleCommandPayloadCreator.Instance.WriteQueryCommand(ref commandListPosition, writer);
				if (firstResult is null)
					firstResult = wroteCommand;

				// write command length
				var commandLength = writer.Position - position - Padding.Length;
				var span = writer.ArraySegment.AsSpan().Slice(position);
				span[0] = 0xFE;
				BinaryPrimitives.WriteUInt64LittleEndian(span.Slice(1), (ulong) commandLength);
			} while (wroteCommand);

			// remove the padding that was saved for the final command (which wasn't written)
			writer.TrimEnd(Padding.Length);
			return firstResult.Value;
		}

		static ReadOnlySpan<byte> Padding => new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
	}
}
