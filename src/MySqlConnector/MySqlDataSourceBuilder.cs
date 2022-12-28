using Microsoft.Extensions.Logging;
using MySqlConnector.Logging;

namespace MySqlConnector;

#if NET7_0_OR_GREATER
public sealed class MySqlDataSourceBuilder
{
	public MySqlDataSourceBuilder(string? connectionString = null)
	{
		ConnectionStringBuilder = new(connectionString ?? "");
	}

	public MySqlDataSourceBuilder UseLoggerFactory(ILoggerFactory? loggerFactory)
	{
		m_loggerFactory = loggerFactory;
		return this;
	}

	public MySqlDataSource Build()
	{
		var loggingConfiguration = m_loggerFactory is null ? MySqlConnectorLoggingConfiguration.NullConfiguration : new(m_loggerFactory);
		return new(ConnectionStringBuilder.ConnectionString,
			loggingConfiguration);
	}

	public MySqlConnectionStringBuilder ConnectionStringBuilder { get; }

	private ILoggerFactory? m_loggerFactory;
}
#endif
