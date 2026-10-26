namespace MySqlConnector.Core;

internal sealed class PreparedStatements(IReadOnlyList<PreparedStatement> preparedStatements, ParsedStatements parsedStatements, string? databaseOverride) : IDisposable
{
	public IReadOnlyList<PreparedStatement> Statements { get; } = preparedStatements;

	// The value of ServerSession.DatabaseOverride when these statements were prepared; the server executes them in that database.
	public string? DatabaseOverride { get; } = databaseOverride;

	public void Dispose()
	{
		parsedStatements?.Dispose();
		parsedStatements = null!;
	}
}
