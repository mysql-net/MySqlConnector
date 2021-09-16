using System.Collections.Generic;
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
