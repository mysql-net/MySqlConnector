#nullable enable

namespace MySqlConnector.Tests.Metrics;

public interface IConnectionCreator : IDisposable
{
	string PoolName { get; }
	MySqlConnection OpenConnection();
}

internal sealed class DataSourceConnectionCreator : IConnectionCreator
{
	public DataSourceConnectionCreator(bool usePooling, string? poolName, string? applicationName, MySqlConnectionStringBuilder connectionStringBuilder)
	{
		connectionStringBuilder.Pooling = usePooling;
		connectionStringBuilder.ApplicationName = applicationName;
		m_dataSource = new MySqlDataSourceBuilder(connectionStringBuilder.ConnectionString)
			.UseName(poolName)
			.Build();
		PoolName = poolName ?? applicationName ?? connectionStringBuilder.GetConnectionString(includePassword: false);
	}

	public MySqlConnection OpenConnection() => m_dataSource.OpenConnection();
	public string PoolName { get; }
	public void Dispose() => m_dataSource.Dispose();

	private readonly MySqlDataSource m_dataSource;
}

internal sealed class PlainConnectionCreator : IConnectionCreator
{
	public PlainConnectionCreator(bool usePooling, string? applicationName, MySqlConnectionStringBuilder connectionStringBuilder)
	{
		connectionStringBuilder.Pooling = usePooling;
		connectionStringBuilder.ApplicationName = applicationName;
		m_connectionString = connectionStringBuilder.ConnectionString;
		PoolName = applicationName ?? connectionStringBuilder.GetConnectionString(includePassword: false);
	}

	public MySqlConnection OpenConnection()
	{
		var connection = new MySqlConnection(m_connectionString);
		connection.Open();
		return connection;
	}

	public string PoolName { get; }

	public void Dispose()
	{
	}

	private readonly string m_connectionString;
}
