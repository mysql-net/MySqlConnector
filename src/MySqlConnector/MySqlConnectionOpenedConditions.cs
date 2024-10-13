namespace MySqlConnector;

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
