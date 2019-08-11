using MySqlConnector.Protocol.Payloads;

namespace MySqlConnector.Core
{
	/// <summary>
	/// <see cref="PreparedStatement"/> is a statement that has been prepared on the MySQL Server.
	/// </summary>
	internal sealed class PreparedStatement
	{
		public PreparedStatement(int statementId, ParsedStatement statement, ColumnDefinitionPayload[] columns, ColumnDefinitionPayload[] parameters)
		{
			StatementId = statementId;
			Statement = statement;
			Columns = columns;
			Parameters = parameters;
		}

		public int StatementId { get; }
		public ParsedStatement Statement { get; }
		public ColumnDefinitionPayload[] Columns { get; }
		public ColumnDefinitionPayload[] Parameters { get; }
	}
}
