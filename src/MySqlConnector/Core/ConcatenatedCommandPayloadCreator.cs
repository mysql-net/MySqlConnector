using System.Diagnostics;
using MySqlConnector.Logging;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core;

internal sealed class ConcatenatedCommandPayloadCreator : ICommandPayloadCreator
{
	public static ICommandPayloadCreator Instance { get; } = new ConcatenatedCommandPayloadCreator();

	public ValueTask SendCommandPrologueAsync(MySqlConnection connection, CommandListPosition commandListPosition, IOBehavior ioBehavior, CancellationToken cancellationToken) =>
		default;

	public bool WriteQueryCommand(ref CommandListPosition commandListPosition, IDictionary<string, CachedProcedure?> cachedProcedures, ByteBufferWriter writer, bool appendSemicolon, Activity? activity)
	{
		if (commandListPosition.CommandIndex == commandListPosition.CommandCount)
			return false;

		writer.Write((byte) CommandKind.Query);

		// ConcatenatedCommandPayloadCreator is only used by MySqlBatch, and MySqlBatchCommand doesn't expose attributes,
		// but we need to write query attributes if there is a current Activity (otherwise WriteAttributes will just write an empty collection)
		var command = commandListPosition.CommandAt(commandListPosition.CommandIndex);
		if (command.Connection!.Session.SupportsQueryAttributes)
			SingleCommandPayloadCreator.WriteAttributes(writer, command, activity);

		bool isComplete;
		do
		{
			command = commandListPosition.CommandAt(commandListPosition.CommandIndex);
			Log.PreparingCommandPayload(command.Logger, command.Connection!.Session.Id, command.CommandText!);

			isComplete = SingleCommandPayloadCreator.WriteQueryPayload(command, cachedProcedures, writer,
				commandListPosition.CommandIndex < commandListPosition.CommandCount - 1 || appendSemicolon,
				commandListPosition.CommandIndex == 0,
				commandListPosition.CommandIndex == commandListPosition.CommandCount - 1);
			commandListPosition.CommandIndex++;
		} while (commandListPosition.CommandIndex < commandListPosition.CommandCount && isComplete);

		return true;
	}
}
