using System.Data;
using System.Data.Common;
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
			m_command.LastInsertedId = -1;
			var statementPreparerOptions = StatementPreparerOptions.None;
			if (m_command.Connection.AllowUserVariables || m_command.CommandType == CommandType.StoredProcedure)
				statementPreparerOptions |= StatementPreparerOptions.AllowUserVariables;
			if (m_command.Connection.OldGuids)
				statementPreparerOptions |= StatementPreparerOptions.OldGuids;
			var preparer = new MySqlStatementPreparer(commandText, parameterCollection, statementPreparerOptions);
			var payload = new PayloadData(preparer.ParseAndBindParameters());
			await m_command.Connection.Session.SendAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
			var reader = await MySqlDataReader.CreateAsync(m_command, behavior, ioBehavior, cancellationToken).ConfigureAwait(false);
			m_command.Connection.HasActiveReader = true;
			return reader;
		}

		readonly MySqlCommand m_command;
	}
}
