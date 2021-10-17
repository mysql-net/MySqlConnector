namespace MySqlConnector;

/// <summary>
/// Provides context for the <see cref="MySqlConnection.ProvidePasswordCallback"/> delegate.
/// </summary>
public sealed class MySqlProvidePasswordContext
{
	/// <summary>
	/// The server to which MySqlConnector is connecting. This is a host name from the <see cref="MySqlConnectionStringBuilder.Server"/> option.
	/// </summary>
	public string Server { get; }

	/// <summary>
	/// The server port. This corresponds to <see cref="MySqlConnectionStringBuilder.Port"/>.
	/// </summary>
	public int Port { get; }

	/// <summary>
	/// The user ID being used for authentication. This corresponds to <see cref="MySqlConnectionStringBuilder.UserID"/>.
	/// </summary>
	public string UserId { get; }

	/// <summary>
	/// The optional initial database; this value may be the empty string. This corresponds to <see cref="MySqlConnectionStringBuilder.Database"/>.
	/// </summary>
	public string Database { get; }

	internal MySqlProvidePasswordContext(string server, int port, string userId, string database)
	{
		Server = server;
		Port = port;
		UserId = userId;
		Database = database;
	}
}
