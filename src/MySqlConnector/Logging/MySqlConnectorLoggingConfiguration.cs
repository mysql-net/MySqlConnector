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
	}

	public ILogger DataSourceLogger { get; }
	public ILogger ConnectionLogger { get; }
	public ILogger CommandLogger { get; }

	public static MySqlConnectorLoggingConfiguration NullConfiguration { get; } = new MySqlConnectorLoggingConfiguration(NullLoggerFactory.Instance);
	public static MySqlConnectorLoggingConfiguration GlobalConfiguration { get; set; } = NullConfiguration;
}
