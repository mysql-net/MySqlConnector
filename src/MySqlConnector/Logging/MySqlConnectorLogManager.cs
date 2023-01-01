using Microsoft.Extensions.Logging;

namespace MySqlConnector.Logging;

/// <summary>
/// Controls logging for MySqlConnector.
/// </summary>
public static class MySqlConnectorLogManager
{
	/// <summary>
	/// Allows the <see cref="IMySqlConnectorLoggerProvider"/> to be set for this library. <see cref="Provider"/> can
	/// be set once, and must be set before any other library methods are used.
	/// </summary>
#pragma warning disable CA1044 // Properties should not be write only
	public static IMySqlConnectorLoggerProvider Provider
	{
		set
		{
			MySqlConnectorLoggingConfiguration.GlobalConfiguration = new(new MySqlConnectorLoggerFactor(value));
		}
	}

	// A helper class that adapts ILoggerFactory to the old-style IMySqlConnectorLoggerProvider interface.
	private sealed class MySqlConnectorLoggerFactor : ILoggerFactory
	{
		public MySqlConnectorLoggerFactor(IMySqlConnectorLoggerProvider loggerProvider) =>
			m_loggerProvider = loggerProvider;

		public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();

		public ILogger CreateLogger(string categoryName)
		{
			// assume all logger names start with "MySqlConnector." but the old API didn't expect that prefix
			return new MySqlConnectorLogger(m_loggerProvider.CreateLogger(categoryName[15..]));
		}

		public void Dispose()
		{
		}

		private readonly IMySqlConnectorLoggerProvider m_loggerProvider;
	}

	// A helper class that adapts ILogger to the old-style IMySqlConnectorLogger interface.
	private sealed class MySqlConnectorLogger : ILogger
	{
		public MySqlConnectorLogger(IMySqlConnectorLogger logger) =>
			m_logger = logger;

		public IDisposable BeginScope<TState>(TState state)
			where TState : notnull
			=> throw new NotSupportedException();

		public bool IsEnabled(LogLevel logLevel) => m_logger.IsEnabled(ConvertLogLevel(logLevel));

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
			m_logger.Log(ConvertLogLevel(logLevel), formatter(state, exception), exception: exception);

		private static MySqlConnectorLogLevel ConvertLogLevel(LogLevel logLevel) =>
			logLevel switch
			{
				LogLevel.Trace => MySqlConnectorLogLevel.Trace,
				LogLevel.Debug => MySqlConnectorLogLevel.Debug,
				LogLevel.Information => MySqlConnectorLogLevel.Info,
				LogLevel.Warning => MySqlConnectorLogLevel.Warn,
				LogLevel.Error => MySqlConnectorLogLevel.Error,
				LogLevel.Critical => MySqlConnectorLogLevel.Fatal,
				_ => MySqlConnectorLogLevel.Info,
			};

		private readonly IMySqlConnectorLogger m_logger;
	}
}
