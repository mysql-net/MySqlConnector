using Microsoft.Extensions.Logging;

namespace MySqlConnector.Core;

/// <summary>
/// <see cref="IMySqlCommand"/> provides an internal abstraction over <see cref="MySqlCommand"/> and <see cref="MySqlBatchCommand"/>.
/// </summary>
internal interface IMySqlCommand
{
	string? CommandText { get; }
	CommandType CommandType { get; }
	bool AllowUserVariables { get; }
	CommandBehavior CommandBehavior { get; }
	MySqlParameterCollection? RawParameters { get; }
	MySqlAttributeCollection? RawAttributes { get; }
	CachedProcedure? CachedProc { get; set; }
	PreparedStatements? PreparedStmts { get; set; }
	PreparedStatements? TryGetPreparedStatements();
	MySqlConnection? Connection { get; }
	long LastInsertedId { get; }
	void SetLastInsertedId(long lastInsertedId);
	MySqlParameterCollection? OutParameters { get; set; }
	MySqlParameter? ReturnParameter { get; set; }
	ICancellableCommand CancellableCommand { get; }
	ILogger Logger { get; }
}

internal static class IMySqlCommandExtensions
{
	public static StatementPreparerOptions CreateStatementPreparerOptions(this IMySqlCommand command)
	{
		var connection = command.Connection!;
		var statementPreparerOptions = StatementPreparerOptions.None;
		if (connection.AllowUserVariables || command.CommandType == CommandType.StoredProcedure || command.AllowUserVariables)
			statementPreparerOptions |= StatementPreparerOptions.AllowUserVariables;
		if (connection.DateTimeKind == DateTimeKind.Utc)
			statementPreparerOptions |= StatementPreparerOptions.DateTimeUtc;
		else if (connection.DateTimeKind == DateTimeKind.Local)
			statementPreparerOptions |= StatementPreparerOptions.DateTimeLocal;
		if (command.CommandType == CommandType.StoredProcedure)
			statementPreparerOptions |= StatementPreparerOptions.AllowOutputParameters;
		if (connection.NoBackslashEscapes)
			statementPreparerOptions |= StatementPreparerOptions.NoBackslashEscapes;

		statementPreparerOptions |= connection.GuidFormat switch
		{
			MySqlGuidFormat.Char36 => StatementPreparerOptions.GuidFormatChar36,
			MySqlGuidFormat.Char32 => StatementPreparerOptions.GuidFormatChar32,
			MySqlGuidFormat.Binary16 => StatementPreparerOptions.GuidFormatBinary16,
			MySqlGuidFormat.TimeSwapBinary16 => StatementPreparerOptions.GuidFormatTimeSwapBinary16,
			MySqlGuidFormat.LittleEndianBinary16 => StatementPreparerOptions.GuidFormatLittleEndianBinary16,
			_ => StatementPreparerOptions.None,
		};

		return statementPreparerOptions;
	}
}
