namespace MySqlConnector.Core;

internal sealed class NonPooledConnectionPoolMetadata : IConnectionPoolMetadata
{
	public static IConnectionPoolMetadata Instance { get; } = new NonPooledConnectionPoolMetadata();

	public ConnectionPool? ConnectionPool => null;
	public int Id => 0;
	public int Generation => 0;
	public int GetNewSessionId() => Interlocked.Increment(ref m_lastId);

	private int m_lastId;
}
