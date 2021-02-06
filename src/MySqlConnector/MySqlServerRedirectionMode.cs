namespace MySqlConnector
{
	/// <summary>
	/// Server redirection configuration.
	/// </summary>
	public enum MySqlServerRedirectionMode
	{
		/// <summary>
		/// Server redirection will not be performed.
		/// </summary>
		Disabled,

		/// <summary>
		/// Server redirection will occur if possible, otherwise the original connection will be used.
		/// </summary>
		Preferred,

		/// <summary>
		/// Server redirection must occur, otherwise connecting fails.
		/// </summary>
		Required,
	}
}
