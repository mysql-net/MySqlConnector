namespace MySqlConnector;

/// <summary>
/// A callback that is invoked when a new <see cref="MySqlConnection"/> is opened.
/// </summary>
/// <param name="data">A <see cref="MySqlConnectionOpenedData"/> giving information about the connection being opened.</param>
/// <returns>A <see cref="ValueTask"/> representing the result of the possibly-asynchronous operation.</returns>
public delegate ValueTask MySqlConnectionOpenedCallback(MySqlConnectionOpenedData data);

public sealed class MySqlConnectionOpenedData
{
	/// <summary>
	/// The <see cref="MySqlConnection"/> that was opened.
	/// </summary>
	public MySqlConnection Connection { get; }

	/// <summary>
	/// Bitflags giving the conditions under which a connection was opened.
	/// </summary>
	public MySqlConnectionOpenedConditions Conditions { get; }

	internal MySqlConnectionOpenedData(MySqlConnection connection, MySqlConnectionOpenedConditions conditions)
	{
		Connection = connection;
		Conditions = conditions;
	}
}

/// <summary>
/// Bitflags giving the conditions under which a connection was opened.
/// </summary>
[Flags]
public enum MySqlConnectionOpenedConditions
{
	/// <summary>
	/// No specific conditions apply. This value may be used when an existing pooled connection is reused without being reset.
	/// </summary>
	None = 0,

	/// <summary>
	/// A new physical connection to a MySQL Server was opened. This value is mutually exclusive with <see cref="Reset"/>.
	/// </summary>
	New = 1,

	/// <summary>
	/// An existing pooled connection to a MySQL Server was reset. This value is mutually exclusive with <see cref="New"/>.
	/// </summary>
	Reset = 2,
}
