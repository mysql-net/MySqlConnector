using Microsoft.Extensions.Logging;
using MySqlConnector.Logging;

namespace MySqlConnector;

/// <summary>
/// <see cref="MySqlDataSourceBuilder"/> provides an API for configuring and creating a <see cref="MySqlDataSource"/>,
/// from which <see cref="MySqlConnection"/> objects can be obtained.
/// </summary>
public sealed class MySqlDataSourceBuilder
{
	/// <summary>
	/// Initializes a new <see cref="MySqlDataSourceBuilder"/> with the specified connection string.
	/// </summary>
	/// <param name="connectionString">The optional connection string to use.</param>
	public MySqlDataSourceBuilder(string? connectionString = null)
	{
		ConnectionStringBuilder = new(connectionString ?? "");
	}

	/// <summary>
	/// Sets the <see cref="ILoggerFactory"/> that will be used for logging.
	/// </summary>
	/// <param name="loggerFactory">The logger factory.</param>
	/// <returns>This object, so that method calls can be chained.</returns>
	public MySqlDataSourceBuilder UseLoggerFactory(ILoggerFactory? loggerFactory)
	{
		m_loggerFactory = loggerFactory;
		return this;
	}

	/// <summary>
	/// Builds a <see cref="MySqlDataSource"/> which is ready for use.
	/// </summary>
	/// <returns>A new <see cref="MySqlDataSource"/> with the settings configured through this <see cref="MySqlDataSourceBuilder"/>.</returns>
	public MySqlDataSource Build()
	{
		var loggingConfiguration = m_loggerFactory is null ? MySqlConnectorLoggingConfiguration.NullConfiguration : new(m_loggerFactory);
		return new(ConnectionStringBuilder.ConnectionString,
			loggingConfiguration);
	}

	/// <summary>
	/// A <see cref="MySqlConnectionStringBuilder"/> that can be used to configure the connection string on this <see cref="MySqlDataSourceBuilder"/>.
	/// </summary>
	public MySqlConnectionStringBuilder ConnectionStringBuilder { get; }

	private ILoggerFactory? m_loggerFactory;
}
