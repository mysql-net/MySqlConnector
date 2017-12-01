namespace MySqlConnector.Logging
{
	/// <summary>
	/// Implementations of <see cref="IMySqlConnectorLoggerProvider"/> create logger instances.
	/// </summary>
	public interface IMySqlConnectorLoggerProvider
	{
		/// <summary>
		/// Creates a logger with the specified name. This method may be called from multiple threads and must be thread-safe.
		/// </summary>
		IMySqlConnectorLogger CreateLogger(string name);
	}
}
