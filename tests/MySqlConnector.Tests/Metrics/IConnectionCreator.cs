#nullable enable

namespace MySqlConnector.Tests.Metrics;

public interface IConnectionCreator : IDisposable
{
	void SetConnectionStringBuilder(MySqlConnectionStringBuilder connectionStringBuilder);
	string PoolName { get; }
	MySqlConnection OpenConnection();
}

internal sealed class DataSourceConnectionCreator(string? poolName) : IConnectionCreator
{
	public void SetConnectionStringBuilder(MySqlConnectionStringBuilder connectionStringBuilder)
	{
		m_connectionStringBuilder = connectionStringBuilder;
		m_dataSource = new MySqlDataSourceBuilder(connectionStringBuilder.ConnectionString)
			.UseName(m_poolName)
			.Build();
	}

	public MySqlConnection OpenConnection() => m_dataSource!.OpenConnection();
	public string PoolName => m_poolName ?? m_connectionStringBuilder!.GetConnectionString(includePassword: false);
	public override string ToString() => $"DataSource: {m_poolName ?? "(unnamed)"}";
	public void Dispose() => m_dataSource!.Dispose();

	private readonly string? m_poolName = poolName;
	private MySqlConnectionStringBuilder? m_connectionStringBuilder;
	private MySqlDataSource? m_dataSource;
}

internal sealed class PlainConnectionCreator : IConnectionCreator
{
	public MySqlConnection OpenConnection()
	{
		var connection = new MySqlConnection(m_connectionStringBuilder!.ConnectionString);
		connection.Open();
		return connection;
	}

	public void SetConnectionStringBuilder(MySqlConnectionStringBuilder connectionStringBuilder) => m_connectionStringBuilder = connectionStringBuilder;
	public string PoolName => m_connectionStringBuilder!.GetConnectionString(includePassword: false);
	public override string ToString() => "Plain";
	public void Dispose() { }

	private MySqlConnectionStringBuilder? m_connectionStringBuilder;
}
