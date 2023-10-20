using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MySqlConnector.Logging;

internal sealed class MySqlConnectorLoggingConfiguration(ILoggerFactory loggerFactory)
{
	public ILogger DataSourceLogger { get; } = loggerFactory.CreateLogger("MySqlConnector.MySqlDataSource");
	public ILogger ConnectionLogger { get; } = loggerFactory.CreateLogger("MySqlConnector.MySqlConnection");
	public ILogger CommandLogger { get; } = loggerFactory.CreateLogger("MySqlConnector.MySqlCommand");
	public ILogger PoolLogger { get; } = loggerFactory.CreateLogger("MySqlConnector.ConnectionPool");
	public ILogger BulkCopyLogger { get; } = loggerFactory.CreateLogger("MySqlConnector.MySqlBulkCopy");

	public static MySqlConnectorLoggingConfiguration NullConfiguration { get; } = new MySqlConnectorLoggingConfiguration(NullLoggerFactory.Instance);
	public static MySqlConnectorLoggingConfiguration GlobalConfiguration { get; set; } = NullConfiguration;
}
