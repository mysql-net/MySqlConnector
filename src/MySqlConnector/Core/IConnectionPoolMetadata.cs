namespace MySqlConnector.Core;

internal interface IConnectionPoolMetadata
{
	/// <summary>
	/// Returns the <see cref="ConnectionPool"/> this <see cref="IConnectionPoolMetadata"/> is associated with,
	/// or <c>null</c> if it represents a non-pooled connection.
	/// </summary>
	ConnectionPool? ConnectionPool { get; }

	/// <summary>
	/// Returns the ID of the connection pool, or 0 if this is a non-pooled connection.
	/// </summary>
	int Id { get; }

	/// <summary>
	/// Returns the generation of the connection pool, or 0 if this is a non-pooled connection.
	/// </summary>
	int Generation { get; }

	/// <summary>
	/// Returns a new session ID.
	/// </summary>
	/// <returns>A new session ID.</returns>
	int GetNewSessionId();
}
