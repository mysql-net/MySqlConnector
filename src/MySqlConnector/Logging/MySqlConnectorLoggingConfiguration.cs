using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MySqlConnector.Logging;

internal sealed class MySqlConnectorLoggingConfiguration
{
	public MySqlConnectorLoggingConfiguration(ILoggerFactory loggerFactory)
	{
		DataSourceLogger = loggerFactory.CreateLogger("MySqlConnector.MySqlDataSource");
		ConnectionLogger = loggerFactory.CreateLogger("MySqlConnector.MySqlConnection");
		CommandLogger = loggerFactory.CreateLogger("MySqlConnector.MySqlCommand");
		PoolLogger = loggerFactory.CreateLogger("MySqlConnector.ConnectionPool");
		BulkCopyLogger = loggerFactory.CreateLogger("MySqlConnector.MySqlBulkCopy");
	}

	public ILogger DataSourceLogger { get; }
	public ILogger ConnectionLogger { get; }
	public ILogger CommandLogger { get; }
	public ILogger PoolLogger { get; }
	public ILogger BulkCopyLogger { get; }

	public static MySqlConnectorLoggingConfiguration NullConfiguration { get; } = new MySqlConnectorLoggingConfiguration(NullLoggerFactory.Instance);
	public static MySqlConnectorLoggingConfiguration GlobalConfiguration { get; set; } = NullConfiguration;
}
