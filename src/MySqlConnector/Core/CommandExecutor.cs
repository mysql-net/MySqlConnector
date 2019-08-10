#nullable disable
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
			var commandListPosition = new CommandListPosition(commands);
			var command = commands[0];
			if (Log.IsDebugEnabled())
				Log.Debug("Session{0} ExecuteReader {1} CommandCount: {2}", command.Connection.Session.Id, ioBehavior, commands.Count);

			Dictionary<string, CachedProcedure> cachedProcedures = null;
			foreach (var command2 in commands)
			{
				if (command2.CommandType == CommandType.StoredProcedure)
				{
					if (cachedProcedures is null)
						cachedProcedures = new Dictionary<string, CachedProcedure>();
					if (!cachedProcedures.ContainsKey(command2.CommandText))
						cachedProcedures.Add(command2.CommandText, await command2.Connection.GetCachedProcedure(ioBehavior, command2.CommandText, cancellationToken).ConfigureAwait(false));
				}
			}

			var writer = new ByteBufferWriter();
			if (!payloadCreator.WriteQueryCommand(ref commandListPosition, cachedProcedures, writer))
				throw new InvalidOperationException("ICommandPayloadCreator failed to write query payload");

			cancellationToken.ThrowIfCancellationRequested();

			using (var payload = writer.ToPayloadData())
			using (command.CancellableCommand.RegisterCancel(cancellationToken))
			{
				command.Connection.Session.StartQuerying(command.CancellableCommand);
				command.SetLastInsertedId(-1);
				try
				{
					await command.Connection.Session.SendAsync(payload, ioBehavior, CancellationToken.None).ConfigureAwait(false);
					return await MySqlDataReader.CreateAsync(commandListPosition, payloadCreator, cachedProcedures, command, behavior, ioBehavior, cancellationToken).ConfigureAwait(false);
				}
				catch (MySqlException ex) when (ex.Number == (int) MySqlErrorCode.QueryInterrupted && cancellationToken.IsCancellationRequested)
				{
					Log.Warn("Session{0} query was interrupted", command.Connection.Session.Id);
					throw new OperationCanceledException(cancellationToken);
				}
				catch (Exception ex) when (payload.Span.Length > 4_194_304 && (ex is SocketException || ex is IOException || ex is MySqlProtocolException))
				{
					// the default MySQL Server value for max_allowed_packet (in MySQL 5.7) is 4MiB: https://dev.mysql.com/doc/refman/5.7/en/server-system-variables.html#sysvar_max_allowed_packet
					// use "decimal megabytes" (to round up) when creating the exception message
					int megabytes = payload.Span.Length / 1_000_000;
					throw new MySqlException("Error submitting {0}MB packet; ensure 'max_allowed_packet' is greater than {0}MB.".FormatInvariant(megabytes), ex);
				}
			}
		}

		static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(CommandExecutor));
	}
}
