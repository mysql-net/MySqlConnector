namespace MySql.Data.MySqlClient
{
	public enum MySqlLoadBalance
	{
		/// <summary>
		/// Each new connection opened for a connection pool uses the next host name (sequentially with wraparound).
		/// </summary>
		RoundRobin,

		/// <summary>
		/// Each new connection tries to connect to the first host; subsequent hosts are used only if connecting to the first one fails.
		/// </summary>
		FailOver,

		/// <summary>
		/// Servers are tried in random order.
		/// </summary>
		Random,

		/// <summary>
		/// Servers are tried in ascending order of number of currently-open connections.
		/// </summary>
		LeastConnections,
	}
}
