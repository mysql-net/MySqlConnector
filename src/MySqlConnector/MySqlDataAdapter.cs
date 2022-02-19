using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MySqlConnector.Core;

namespace MySqlConnector;

public sealed class MySqlDataAdapter : DbDataAdapter
{
	public MySqlDataAdapter()
	{
		GC.SuppressFinalize(this);
	}

	public MySqlDataAdapter(MySqlCommand selectCommand)
		: this()
	{
		SelectCommand = selectCommand;
	}

	public MySqlDataAdapter(string selectCommandText, MySqlConnection connection)
		: this(new MySqlCommand(selectCommandText, connection))
	{
	}

	public MySqlDataAdapter(string selectCommandText, string connectionString)
		: this(new MySqlCommand(selectCommandText, new MySqlConnection(connectionString)))
	{
	}

	public event MySqlRowUpdatingEventHandler? RowUpdating;

	public event MySqlRowUpdatedEventHandler? RowUpdated;

	public new MySqlCommand? DeleteCommand
	{
		get => (MySqlCommand?) base.DeleteCommand;
		set => base.DeleteCommand = value;
	}

	public new MySqlCommand? InsertCommand
	{
		get => (MySqlCommand?) base.InsertCommand;
		set => base.InsertCommand = value;
	}

	public new MySqlCommand? SelectCommand
	{
		get => (MySqlCommand?) base.SelectCommand;
		set => base.SelectCommand = value;
	}

	public new MySqlCommand? UpdateCommand
	{
		get => (MySqlCommand?) base.UpdateCommand;
		set => base.UpdateCommand = value;
	}

	protected override void OnRowUpdating(RowUpdatingEventArgs value) => RowUpdating?.Invoke(this, (MySqlRowUpdatingEventArgs) value);

	protected override void OnRowUpdated(RowUpdatedEventArgs value) => RowUpdated?.Invoke(this, (MySqlRowUpdatedEventArgs) value);

	protected override RowUpdatingEventArgs CreateRowUpdatingEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping) => new MySqlRowUpdatingEventArgs(dataRow, command, statementType, tableMapping);

	protected override RowUpdatedEventArgs CreateRowUpdatedEvent(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping) => new MySqlRowUpdatedEventArgs(dataRow, command, statementType, tableMapping);

	public override int UpdateBatchSize { get; set; }

	protected override void InitializeBatching() => m_batch = new();

	protected override void TerminateBatching()
	{
		m_batch?.Dispose();
		m_batch = null;
	}

	protected override int AddToBatch(IDbCommand command)
	{
		var mySqlCommand = (MySqlCommand) command;
		if (m_batch!.Connection is null)
		{
			m_batch.Connection = mySqlCommand.Connection;
			m_batch.Transaction = mySqlCommand.Transaction;
		}

		var count = m_batch.BatchCommands.Count;
		var batchCommand = new MySqlBatchCommand
		{
			CommandText = command.CommandText,
			CommandType = command.CommandType,
		};
		if (mySqlCommand.CloneRawParameters() is MySqlParameterCollection clonedParameters)
		{
			foreach (var clonedParameter in clonedParameters)
				batchCommand.Parameters.Add(clonedParameter!);
		}

		m_batch.BatchCommands.Add(batchCommand);
		return count;
	}

	protected override void ClearBatch() => m_batch!.BatchCommands.Clear();

	protected override int ExecuteBatch()
	{
		if (TryConvertToCommand(m_batch!) is MySqlCommand command)
		{
			command.Connection = m_batch!.Connection;
			command.Transaction = m_batch.Transaction;
			return command.ExecuteNonQuery();
		}
		else
		{
			return m_batch!.ExecuteNonQuery();
		}
	}

	// Detects if the commands in 'batch' are all "INSERT" commands that can be combined into one large value list;
	// returns a MySqlCommand with the combined SQL if so.
	internal static MySqlCommand? TryConvertToCommand(MySqlBatch batch)
	{
		// ensure there are at least two commands
		if (batch.BatchCommands.Count < 1)
			return null;

		// check for a parameterized command
		var firstCommand = batch.BatchCommands[0];
		if (firstCommand.Parameters.Count == 0)
			return null;
		firstCommand.Batch = batch;

		// check that all commands have the same SQL
		var sql = firstCommand.CommandText;
		for (var i = 1; i < batch.BatchCommands.Count; i++)
		{
			if (batch.BatchCommands[i].CommandText != sql)
				return null;
		}

		// check that it's an INSERT statement
		if (!sql.StartsWith("INSERT INTO ", StringComparison.OrdinalIgnoreCase))
			return null;

		// check for "VALUES(...)" clause
		var match = Regex.Match(sql, @"\bVALUES\s*\([^)]+\)\s*;?\s*$", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		if (!match.Success)
			return null;

		// extract the parameters
		var parser = new InsertSqlParser(firstCommand);
		parser.Parse(sql);

		// record the parameter indexes that were found
		foreach (var parameterIndex in parser.ParameterIndexes)
		{
			if (parameterIndex < 0 || parameterIndex >= firstCommand.Parameters.Count)
				return null;
		}

		// ensure that the VALUES(...) clause contained only parameters, and that all were consumed
		var remainingValues = parser.CommandText.Substring(match.Index + 6).Trim();
		remainingValues = remainingValues.TrimEnd(';').Trim().TrimStart('(').TrimEnd(')');
		remainingValues = remainingValues.Replace(",", "");
		if (!string.IsNullOrWhiteSpace(remainingValues))
			return null;

		// build one INSERT statement with concatenated VALUES
		var combinedCommand = new MySqlCommand();
		var sqlBuilder = new StringBuilder(sql.Substring(0, match.Index + 6));
		var combinedParameterIndex = 0;
		for (var i = 0; i < batch.BatchCommands.Count; i++)
		{
			var command = batch.BatchCommands[i];
			if (i != 0)
				sqlBuilder.Append(',');
			sqlBuilder.Append('(');

			for (var parameterIndex = 0; parameterIndex < parser.ParameterIndexes.Count; parameterIndex++)
			{
				if (parameterIndex != 0)
					sqlBuilder.Append(',');
				var parameterName = "@p" + combinedParameterIndex.ToString(CultureInfo.InvariantCulture);
				sqlBuilder.Append(parameterName);
				combinedParameterIndex++;
				var parameter = command.Parameters[parser.ParameterIndexes[parameterIndex]].Clone();
				parameter.ParameterName = parameterName;
				combinedCommand.Parameters.Add(parameter);
			}

			sqlBuilder.Append(')');
		}
		sqlBuilder.Append(';');

		combinedCommand.CommandText = sqlBuilder.ToString();
		return combinedCommand;
	}

	internal sealed class InsertSqlParser : SqlParser
	{
		public InsertSqlParser(IMySqlCommand command)
			: base(new StatementPreparer(command.CommandText!, null, command.CreateStatementPreparerOptions()))
		{
			CommandText = command.CommandText!;
			m_parameters = command.RawParameters;
			ParameterIndexes = new();
		}

		public List<int> ParameterIndexes { get; }

		public string CommandText { get; private set; }

		protected override void OnNamedParameter(int index, int length)
		{
			var name = CommandText.Substring(index, length);
			var parameterIndex = m_parameters?.NormalizedIndexOf(name) ?? -1;
			ParameterIndexes.Add(parameterIndex);

			// overwrite the parameter name with spaces
#if NETCOREAPP3_0_OR_GREATER
			CommandText = string.Concat(CommandText.AsSpan(0, index), new string(' ', length), CommandText.AsSpan(index + length));
#else
			CommandText = CommandText.Substring(0, index) + new string(' ', length) + CommandText.Substring(index + length);
#endif
		}

		protected override void OnPositionalParameter(int index)
		{
			ParameterIndexes.Add(ParameterIndexes.Count);

			// overwrite the parameter placeholder with a space
#if NETCOREAPP3_0_OR_GREATER
			CommandText = string.Concat(CommandText.AsSpan(0, index), " ", CommandText.AsSpan(index + 1));
#else
			CommandText = CommandText.Substring(0, index) + " " + CommandText.Substring(index + 1);
#endif
		}

		private readonly MySqlParameterCollection? m_parameters;
	}

	private MySqlBatch? m_batch;
}

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public delegate void MySqlRowUpdatingEventHandler(object sender, MySqlRowUpdatingEventArgs e);

public delegate void MySqlRowUpdatedEventHandler(object sender, MySqlRowUpdatedEventArgs e);
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix

public sealed class MySqlRowUpdatingEventArgs : RowUpdatingEventArgs
{
	public MySqlRowUpdatingEventArgs(DataRow row, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
		: base(row, command, statementType, tableMapping)
	{
	}

	public new MySqlCommand? Command => (MySqlCommand?) base.Command!;
}

public sealed class MySqlRowUpdatedEventArgs : RowUpdatedEventArgs
{
	public MySqlRowUpdatedEventArgs(DataRow row, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
		: base(row, command, statementType, tableMapping)
	{
	}

	public new MySqlCommand? Command => (MySqlCommand?) base.Command;
}
