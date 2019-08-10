namespace MySqlConnector.Logging
{
	/// <summary>
	/// Creates loggers that do nothing.
	/// </summary>
	public sealed class NoOpLoggerProvider : IMySqlConnectorLoggerProvider
	{
		/// <summary>
		/// Returns a <see cref="NoOpLogger"/>.
		/// </summary>
		public IMySqlConnectorLogger CreateLogger(string name) => NoOpLogger.Instance;
	}
}
