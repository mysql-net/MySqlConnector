using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core
{
	internal sealed class BatchedCommandPayloadCreator : ICommandPayloadCreator
	{
		public bool WriteQueryCommand(ref CommandListPosition commandListPosition, ByteBufferWriter writer)
		{
			bool? firstResult = default;
			bool wroteCommand;
			do
			{
				wroteCommand = SingleCommandPayloadCreator.WriteSingleQueryCommand(ref commandListPosition, writer);
				if (firstResult is null)
					firstResult = wroteCommand;
			} while (wroteCommand);
			return firstResult.Value;
		}
	}
}
