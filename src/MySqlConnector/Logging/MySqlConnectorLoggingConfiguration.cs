using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MySqlConnector.Logging;

public sealed class MySqlConnectorLoggingConfiguration(ILoggerFactory loggerFactory)
{
	internal ILogger DataSourceLogger { get; } = loggerFactory.CreateLogger("MySqlConnector.MySqlDataSource");
	internal ILogger ConnectionLogger { get; } = loggerFactory.CreateLogger("MySqlConnector.MySqlConnection");
	internal ILogger CommandLogger { get; } = loggerFactory.CreateLogger("MySqlConnector.MySqlCommand");
	internal ILogger PoolLogger { get; } = loggerFactory.CreateLogger("MySqlConnector.ConnectionPool");
	internal ILogger BulkCopyLogger { get; } = loggerFactory.CreateLogger("MySqlConnector.MySqlBulkCopy");

	internal static ILoggerFactory GlobalLoggerFactory { get; set; } = NullLoggerFactory.Instance;
	internal static MySqlConnectorLoggingConfiguration NullConfiguration { get; } = new MySqlConnectorLoggingConfiguration(GlobalLoggerFactory);
	internal static MySqlConnectorLoggingConfiguration GlobalConfiguration { get; set; } = NullConfiguration;

	/// <summary>
	/// <para>
	/// Globally initializes MySqlConnector logging to use the provided <paramref name="loggerFactory" />.
	/// Must be called before any MySqlConnector APIs are used.
	/// </para>
	/// </summary>
	/// <param name="loggerFactory">The logging factory to use when logging from MySqlConnector.</param>
	public static void InitializeLogging(ILoggerFactory loggerFactory)
	{
		GlobalLoggerFactory = loggerFactory;
		GlobalConfiguration = new MySqlConnectorLoggingConfiguration(loggerFactory);
	}
}
