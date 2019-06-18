using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySqlConnector.Logging;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal static class CommandExecutor
	{
		public static async Task<DbDataReader> ExecuteReaderAsync(IReadOnlyList<IMySqlCommand> commands, ICommandPayloadCreator payloadCreator, CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			// TODO: if (Log.IsDebugEnabled())
			//	Log.Debug("Session{0} ExecuteBehavior {1} CommandText: {2}", m_command.Connection.Session.Id, ioBehavior, commandText);
			var commandListPosition = new CommandListPosition(commands);
			var command = commands[0];
			ByteBufferWriter writer = new ByteBufferWriter();
			if (!payloadCreator.WriteQueryCommand(ref commandListPosition, writer))
				throw new InvalidOperationException("ICommandPayloadCreator failed to write query payload");
			using (var payload = writer.ToPayloadData())
			using (command.RegisterCancel(cancellationToken))
			{
				command.Connection.Session.StartQuerying(command);
				command.SetLastInsertedId(-1);
				try
				{
					await command.Connection.Session.SendAsync(payload, ioBehavior, CancellationToken.None).ConfigureAwait(false);
					return await MySqlDataReader.CreateAsync(commandListPosition, payloadCreator, command, behavior, ioBehavior).ConfigureAwait(false);
				}
				catch (MySqlException ex) when (ex.Number == (int) MySqlErrorCode.QueryInterrupted && cancellationToken.IsCancellationRequested)
				{
					Log.Warn("Session{0} query was interrupted", command.Connection.Session.Id);
					throw new OperationCanceledException(cancellationToken);
				}
				catch (Exception ex) when (payload.ArraySegment.Count > 4_194_304 && (ex is SocketException || ex is IOException || ex is MySqlProtocolException))
				{
					// the default MySQL Server value for max_allowed_packet (in MySQL 5.7) is 4MiB: https://dev.mysql.com/doc/refman/5.7/en/server-system-variables.html#sysvar_max_allowed_packet
					// use "decimal megabytes" (to round up) when creating the exception message
					int megabytes = payload.ArraySegment.Count / 1_000_000;
					throw new MySqlException("Error submitting {0}MB packet; ensure 'max_allowed_packet' is greater than {0}MB.".FormatInvariant(megabytes), ex);
				}
			}
		}

		static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(CommandExecutor));
	}
}