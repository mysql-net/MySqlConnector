namespace MySqlConnector.Core;

/// <summary>
/// <see cref="CommandListPosition"/> encapsulates a list of <see cref="IMySqlCommand"/> and the current position within that list.
/// </summary>
internal struct CommandListPosition
{
	public CommandListPosition(object commands)
	{
		m_commands = commands;
		CommandCount = commands switch
		{
			MySqlCommand _ => 1,
			IReadOnlyList<MySqlBatchCommand> list => list.Count,
			_ => 0,
		};
		PreparedStatements = null;
		CommandIndex = 0;
		PreparedStatementIndex = 0;
	}

	public readonly IMySqlCommand CommandAt(int index) =>
		m_commands switch
		{
			MySqlCommand command when index is 0 => command,
			IReadOnlyList<MySqlBatchCommand> list => list[index],
			_ => throw new ArgumentOutOfRangeException(nameof(index)),
		};

	/// <summary>
	/// The commands in this list; either a singular <see cref="MySqlCommand"/> or a <see cref="IReadOnlyList{MySqlBatchCommand}"/>.
	/// </summary>
	private readonly object m_commands;

	/// <summary>
	/// The number of commands in the list.
	/// </summary>
	public readonly int CommandCount;

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
