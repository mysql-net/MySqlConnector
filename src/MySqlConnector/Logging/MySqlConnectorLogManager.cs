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
	[Obsolete("Use UseLoggerFactory or AddMySqlDataSource instead. See https://mysqlconnector.net/diagnostics/logging/.")]
	public static IMySqlConnectorLoggerProvider Provider
	{
		set
		{
			MySqlConnectorLoggingConfiguration.GlobalConfiguration = new(new MySqlConnectorLoggerFactory(value));
		}
	}

	// A helper class that adapts ILoggerFactory to the old-style IMySqlConnectorLoggerProvider interface.
	private sealed class MySqlConnectorLoggerFactory(IMySqlConnectorLoggerProvider loggerProvider) : ILoggerFactory
	{
		public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();

		public ILogger CreateLogger(string categoryName)
		{
			// assume all logger names start with "MySqlConnector." but the old API didn't expect that prefix
			return new MySqlConnectorLogger(loggerProvider.CreateLogger(categoryName[15..]));
		}

		public void Dispose()
		{
		}
	}

	// A helper class that adapts ILogger to the old-style IMySqlConnectorLogger interface.
	private sealed class MySqlConnectorLogger(IMySqlConnectorLogger logger) : ILogger
	{
		public IDisposable BeginScope<TState>(TState state)
			where TState : notnull
			=> throw new NotSupportedException();

		public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(ConvertLogLevel(logLevel));

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
			logger.Log(ConvertLogLevel(logLevel), formatter(state, exception), exception: exception);

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
	}
}
