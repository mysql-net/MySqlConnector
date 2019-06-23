using System;
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;

namespace MySqlConnector.Core
{
	/// <summary>
	/// <see cref="IMySqlCommand"/> provides an internal abstraction over <see cref="MySqlCommand"/> and <see cref="MySqlBatchCommand"/>.
	/// </summary>
	internal interface IMySqlCommand
	{
		string CommandText { get; }
		CommandType CommandType { get; }
		MySqlParameterCollection Parameters { get; }
		PreparedStatements TryGetPreparedStatements();
		MySqlConnection Connection { get; set; }
		long LastInsertedId { get; }
		void SetLastInsertedId(long lastInsertedId);
		MySqlParameterCollection OutParameters { get; set; }
		MySqlParameter ReturnParameter { get; set; }
	}

	internal static class IMySqlCommandExtensions
	{
		public static void ResetCommandTimeout(this IMySqlCommand command)
		{
			// TODO:
			(command as MySqlCommand)?.ResetCommandTimeout();
		}

		public static IDisposable RegisterCancel(this IMySqlCommand command, CancellationToken cancellationToken)
		{
			// TODO:
			return (command as MySqlCommand)?.RegisterCancel(cancellationToken);
		}

		public static StatementPreparerOptions CreateStatementPreparerOptions(this IMySqlCommand command)
		{
			var connection = command.Connection;
			var statementPreparerOptions = StatementPreparerOptions.None;
			if (connection.AllowUserVariables || command.CommandType == CommandType.StoredProcedure)
				statementPreparerOptions |= StatementPreparerOptions.AllowUserVariables;
			if (connection.DateTimeKind == DateTimeKind.Utc)
				statementPreparerOptions |= StatementPreparerOptions.DateTimeUtc;
			else if (connection.DateTimeKind == DateTimeKind.Local)
				statementPreparerOptions |= StatementPreparerOptions.DateTimeLocal;
			if (command.CommandType == CommandType.StoredProcedure)
				statementPreparerOptions |= StatementPreparerOptions.AllowOutputParameters;

			switch (connection.GuidFormat)
			{
			case MySqlGuidFormat.Char36:
				statementPreparerOptions |= StatementPreparerOptions.GuidFormatChar36;
				break;
			case MySqlGuidFormat.Char32:
				statementPreparerOptions |= StatementPreparerOptions.GuidFormatChar32;
				break;
			case MySqlGuidFormat.Binary16:
				statementPreparerOptions |= StatementPreparerOptions.GuidFormatBinary16;
				break;
			case MySqlGuidFormat.TimeSwapBinary16:
				statementPreparerOptions |= StatementPreparerOptions.GuidFormatTimeSwapBinary16;
				break;
			case MySqlGuidFormat.LittleEndianBinary16:
				statementPreparerOptions |= StatementPreparerOptions.GuidFormatLittleEndianBinary16;
				break;
			}

			return statementPreparerOptions;
		}
	}
}
