namespace MySqlConnector.Core;

/// <summary>
/// <see cref="CommandListPosition"/> encapsulates a list of <see cref="IMySqlCommand"/> and the current position within that list.
/// </summary>
internal struct CommandListPosition
{
	public CommandListPosition(IReadOnlyList<IMySqlCommand> commands)
	{
		Commands = commands;
		PreparedStatements = null;
		CommandIndex = 0;
		PreparedStatementIndex = 0;
	}

	/// <summary>
	/// The commands in the list.
	/// </summary>
	public IReadOnlyList<IMySqlCommand> Commands { get; }

	/// <summary>
	/// Associated prepared statements of commands
	/// </summary>
	public PreparedStatements? PreparedStatements;

	/// <summary>
	/// The index of the current command.
	/// </summary>
	public int CommandIndex;

	/// <summary>
	/// If the current command is a prepared statement, the index of the current prepared statement for that command.
	/// </summary>
	public int PreparedStatementIndex;

	/// <summary>
	/// Retrieve the last used prepared statement
	/// </summary>
	public PreparedStatement? LastUsedPreparedStatement;
}
