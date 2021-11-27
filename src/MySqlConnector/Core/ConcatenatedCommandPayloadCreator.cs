using MySqlConnector.Logging;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core;

internal sealed class ConcatenatedCommandPayloadCreator : ICommandPayloadCreator
{
	public static ICommandPayloadCreator Instance { get; } = new ConcatenatedCommandPayloadCreator();

	public bool WriteQueryCommand(ref CommandListPosition commandListPosition, IDictionary<string, CachedProcedure?> cachedProcedures, ByteBufferWriter writer)
	{
		if (commandListPosition.CommandIndex == commandListPosition.Commands.Count)
			return false;

		writer.Write((byte) CommandKind.Query);

		// ConcatenatedCommandPayloadCreator is only used by MySqlBatch, and MySqlBatchCommand doesn't expose attributes,
		// so just write an empty attribute set if the server needs it.
		if (commandListPosition.Commands[commandListPosition.CommandIndex].Connection!.Session.SupportsQueryAttributes)
		{
			// attribute count
			writer.WriteLengthEncodedInteger(0);

			// attribute set count (always 1)
			writer.Write((byte) 1);
		}

		bool isComplete;
		do
		{
			var command = commandListPosition.Commands[commandListPosition.CommandIndex];
			if (Log.IsTraceEnabled())
				Log.Trace("Session{0} Preparing command payload; CommandText: {1}", command.Connection!.Session.Id, command.CommandText);

			isComplete = SingleCommandPayloadCreator.WriteQueryPayload(command, cachedProcedures, writer);
			commandListPosition.CommandIndex++;
		}
		while (commandListPosition.CommandIndex < commandListPosition.Commands.Count && isComplete);

		return true;
	}

	static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(ConcatenatedCommandPayloadCreator));
}
