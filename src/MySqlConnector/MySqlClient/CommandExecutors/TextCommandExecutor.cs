using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Protocol.Serialization;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient.CommandExecutors
{
	internal class TextCommandExecutor : ICommandExecutor
	{
		internal TextCommandExecutor(MySqlCommand command)
		{
			m_command = command;
		}

		public virtual async Task<int> ExecuteNonQueryAsync(string commandText, MySqlParameterCollection parameterCollection,
			IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			using (var reader = (MySqlDataReader) await ExecuteReaderAsync(commandText, parameterCollection, CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false))
			{
				do
				{
					while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
					{
					}
				} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
				return reader.RecordsAffected;
			}
		}

		public virtual async Task<object> ExecuteScalarAsync(string commandText, MySqlParameterCollection parameterCollection,
			IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			object result = null;
			using (var reader = (MySqlDataReader) await ExecuteReaderAsync(commandText, parameterCollection, CommandBehavior.SingleResult | CommandBehavior.SingleRow, ioBehavior, cancellationToken).ConfigureAwait(false))
			{
				do
				{
					if (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
						result = reader.GetValue(0);
				} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
			}
			return result;
		}

		public virtual async Task<DbDataReader> ExecuteReaderAsync(string commandText, MySqlParameterCollection parameterCollection,
			CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			using (cancellationToken.Register(m_command.Cancel))
			{
				m_command.Connection.Session.StartQuerying(m_command);
				m_command.LastInsertedId = -1;
				var statementPreparerOptions = StatementPreparerOptions.None;
				if (m_command.Connection.AllowUserVariables || m_command.CommandType == CommandType.StoredProcedure)
					statementPreparerOptions |= StatementPreparerOptions.AllowUserVariables;
				if (m_command.Connection.OldGuids)
					statementPreparerOptions |= StatementPreparerOptions.OldGuids;
				var preparer = new MySqlStatementPreparer(commandText, parameterCollection, statementPreparerOptions);
				var payload = new PayloadData(preparer.ParseAndBindParameters());
				try
				{
					await m_command.Connection.Session.SendAsync(payload, ioBehavior, CancellationToken.None).ConfigureAwait(false);
					return await MySqlDataReader.CreateAsync(m_command, behavior, ioBehavior).ConfigureAwait(false);
				}
				catch (MySqlException ex) when (ex.Number == (int) MySqlErrorCode.QueryInterrupted && cancellationToken.IsCancellationRequested)
				{
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

		readonly MySqlCommand m_command;
	}
}
