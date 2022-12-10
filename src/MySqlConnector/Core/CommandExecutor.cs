using System.Diagnostics;
using System.Net.Sockets;
using MySqlConnector.Logging;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal static class CommandExecutor
{
	public static async Task<MySqlDataReader> ExecuteReaderAsync(IReadOnlyList<IMySqlCommand> commands, ICommandPayloadCreator payloadCreator, CommandBehavior behavior, Activity? activity, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		try
		{
			cancellationToken.ThrowIfCancellationRequested();
			var commandListPosition = new CommandListPosition(commands);
			var command = commands[0];

			// pre-requisite: Connection is non-null must be checked before calling this method
			var connection = command.Connection!;

			if (Log.IsTraceEnabled())
				Log.Trace("Session{0} ExecuteReader {1} CommandCount: {2}", connection.Session.Id, ioBehavior, commands.Count);

			Dictionary<string, CachedProcedure?>? cachedProcedures = null;
			foreach (var command2 in commands)
			{
				if (command2.CommandType == CommandType.StoredProcedure)
				{
					cachedProcedures ??= new();
					var commandText = command2.CommandText!;
					if (!cachedProcedures.ContainsKey(commandText))
					{
						cachedProcedures.Add(commandText, await connection.GetCachedProcedure(commandText, revalidateMissing: false, ioBehavior, cancellationToken).ConfigureAwait(false));

						// because the connection was used to execute a MySqlDataReader with the connection's DefaultCommandTimeout,
						// we need to reapply the command's CommandTimeout (even if some of the time has elapsed)
						command.CancellableCommand.ResetCommandTimeout();
					}
				}
			}

			var writer = new ByteBufferWriter();
			//// cachedProcedures will be non-null if there is a stored procedure, which is also the only time it will be read
			if (!payloadCreator.WriteQueryCommand(ref commandListPosition, cachedProcedures!, writer, false))
				throw new InvalidOperationException("ICommandPayloadCreator failed to write query payload");

			cancellationToken.ThrowIfCancellationRequested();

			using var payload = writer.ToPayloadData();
			connection.Session.StartQuerying(command.CancellableCommand);
			command.SetLastInsertedId(0);
			try
			{
				await connection.Session.SendAsync(payload, ioBehavior, CancellationToken.None).ConfigureAwait(false);
				return await MySqlDataReader.CreateAsync(commandListPosition, payloadCreator, cachedProcedures, command, behavior, activity, ioBehavior, cancellationToken).ConfigureAwait(false);
			}
			catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.QueryInterrupted && cancellationToken.IsCancellationRequested)
			{
				Log.Info("Session{0} query was interrupted", connection.Session.Id);
				throw new OperationCanceledException(ex.Message, ex, cancellationToken);
			}
			catch (Exception ex) when (payload.Span.Length > 4_194_304 && (ex is SocketException or IOException or MySqlProtocolException))
			{
				// the default MySQL Server value for max_allowed_packet (in MySQL 5.7) is 4MiB: https://dev.mysql.com/doc/refman/5.7/en/server-system-variables.html#sysvar_max_allowed_packet
				// use "decimal megabytes" (to round up) when creating the exception message
				int megabytes = payload.Span.Length / 1_000_000;
				throw new MySqlException($"Error submitting {megabytes}MB packet; ensure 'max_allowed_packet' is greater than {megabytes}MB.", ex);
			}
		}
		catch (Exception ex) when (activity is { IsAllDataRequested: true })
		{
			activity.SetException(ex);
			activity.Stop();
			throw;
		}
	}

	private static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(CommandExecutor));
}
