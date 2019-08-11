using System.Collections.Generic;

namespace MySqlConnector.Core
{
	/// <summary>
	/// <see cref="CommandListPosition"/> encapsulates a list of <see cref="IMySqlCommand"/> and the current position within that list.
	/// </summary>
	internal struct CommandListPosition
	{
		public CommandListPosition(IReadOnlyList<IMySqlCommand> commands)
		{
			Commands = commands;
			CommandIndex = 0;
			PreparedStatementIndex = 0;
		}

		/// <summary>
		/// The commands in the list.
		/// </summary>
		public IReadOnlyList<IMySqlCommand> Commands { get; }

		/// <summary>
		/// The index of the current command.
		/// </summary>
		public int CommandIndex;

		/// <summary>
		/// If the current command is a prepared statement, the index of the current prepared statement for that command.
		/// </summary>
		public int PreparedStatementIndex;
	}
}
