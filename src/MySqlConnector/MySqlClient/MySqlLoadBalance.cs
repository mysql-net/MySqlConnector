namespace MySql.Data.MySqlClient
{
	public enum MySqlLoadBalance
	{
		/// <summary>
		/// Servers are tried sequentially, across multiple calls to <see cref="MySqlConnection.Open"/>.
		/// </summary>
		RoundRobin,

		/// <summary>
		/// Servers are tried in order, starting with the first one, for each call to <see cref="MySqlConnection.Open"/>.
		/// </summary>
		InOrder,

		/// <summary>
		/// Servers are tried in random order.
		/// </summary>
		Random,

		/// <summary>
		/// Servers are tried in ascending order of number of currently-open connections.
		/// </summary>
		FewestConnections,
	}
}
