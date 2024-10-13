namespace MySqlConnector;

/// <summary>
/// Contains information passed to <see cref="MySqlConnectionOpenedCallback"/> when a new <see cref="MySqlConnection"/> is opened.
/// </summary>
public sealed class MySqlConnectionOpenedContext
{
	/// <summary>
	/// The <see cref="MySqlConnection"/> that was opened.
	/// </summary>
	public MySqlConnection Connection { get; }

	/// <summary>
	/// Bitflags giving the conditions under which a connection was opened.
	/// </summary>
	public MySqlConnectionOpenedConditions Conditions { get; }

	internal MySqlConnectionOpenedContext(MySqlConnection connection, MySqlConnectionOpenedConditions conditions)
	{
		Connection = connection;
		Conditions = conditions;
	}
}
