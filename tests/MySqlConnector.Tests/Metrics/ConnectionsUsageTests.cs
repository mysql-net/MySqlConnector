#nullable enable

namespace MySqlConnector.Tests.Metrics;

public class ConnectionsUsageTests : MetricsTestsBase
{
    [Theory(Skip = MetricsSkip)]
	[InlineData("DataSource|true||")]
	[InlineData("DataSource|true||app-name")]
    [InlineData("DataSource|true|pool-name|")]
    [InlineData("DataSource|true|pool-name|app-name")]
	[InlineData("Plain|true|")]
	[InlineData("Plain|true|app-name")]
	public void ConnectionsWithPoolsHaveMetrics(string connectionCreatorSpec)
    {
		using var connectionCreator = CreateConnectionCreator(connectionCreatorSpec, CreateConnectionStringBuilder());
		PoolName = connectionCreator.PoolName;

		// no connections at beginning of test
		AssertMeasurement("db.client.connections.usage", 0);
		AssertMeasurement("db.client.connections.usage|idle", 0);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(0, Server.ActiveConnections);

		// opening a connection creates a 'used' connection
		using (var connection = connectionCreator.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 1);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 1);
			Assert.Equal(1, Server.ActiveConnections);
		}

		// closing it creates an 'idle' connection
		AssertMeasurement("db.client.connections.usage", 1);
		AssertMeasurement("db.client.connections.usage|idle", 1);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(1, Server.ActiveConnections);

		// reopening the connection transitions it back to 'used'
		using (var connection = connectionCreator.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 1);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 1);
		}
		Assert.Equal(1, Server.ActiveConnections);

		// opening a second connection creates a net new 'used' connection
		using (var connection = connectionCreator.OpenConnection())
		using (var connection2 = connectionCreator.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 2);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 2);
			Assert.Equal(2, Server.ActiveConnections);
		}

		AssertMeasurement("db.client.connections.usage", 2);
		AssertMeasurement("db.client.connections.usage|idle", 2);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(2, Server.ActiveConnections);
	}

	[Theory(Skip = MetricsSkip)]
    [InlineData("DataSource|false||")]
    [InlineData("DataSource|false||app-name")]
    [InlineData("DataSource|false|pool-name|")]
    [InlineData("DataSource|false|pool-name|app-name")]
    [InlineData("Plain|false|")]
    [InlineData("Plain|false|app-name")]
    public void ConnectionsWithoutPoolsHaveNoMetrics(string connectionCreatorSpec)
	{
		using var connectionCreator = CreateConnectionCreator(connectionCreatorSpec, CreateConnectionStringBuilder());
		PoolName = connectionCreator.PoolName;

		// no connections at beginning of test
		AssertMeasurement("db.client.connections.usage", 0);
		AssertMeasurement("db.client.connections.usage|idle", 0);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(0, Server.ActiveConnections);

		// opening a connection doesn't change connection counts
		using (var connection = connectionCreator.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 0);
			AssertMeasurement("db.client.connections.usage|idle", 0);
			AssertMeasurement("db.client.connections.usage|used", 0);
			Assert.Equal(1, Server.ActiveConnections);
		}

		// closing it doesn't create an idle connection but closes it immediately
		AssertMeasurement("db.client.connections.usage", 0);
		AssertMeasurement("db.client.connections.usage|idle", 0);
		AssertMeasurement("db.client.connections.usage|used", 0);

		// disposing the connection sends a COM_QUIT packet and immediately returns; give the in-proc server a chance to process it
		for (var retry = 0; retry < 20; retry++)
		{
			if (Server.ActiveConnections != 0)
				Thread.Sleep(1);
		}
		Assert.Equal(0, Server.ActiveConnections);
	}

	[Fact(Skip = MetricsSkip)]
	public void NamedDataSourceWithMinPoolSize()
	{
		var csb = CreateConnectionStringBuilder();
		csb.MinimumPoolSize = 3;

		PoolName = "minimum-pool-size";
		using var dataSource = new MySqlDataSourceBuilder(csb.ConnectionString)
			.UseName(PoolName)
			.Build();

		// minimum pool size is created lazily when the first connection is opened
		AssertMeasurement("db.client.connections.usage", 0);
		AssertMeasurement("db.client.connections.usage|idle", 0);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(0, Server.ActiveConnections);

		// opening a connection creates the minimum connections then takes an idle one from the pool
		using (var connection = dataSource.OpenConnection())
		{
			AssertMeasurement("db.client.connections.usage", 3);
			AssertMeasurement("db.client.connections.usage|idle", 2);
			AssertMeasurement("db.client.connections.usage|used", 1);
			Assert.Equal(3, Server.ActiveConnections);
		}

		// closing puts it back to idle
		AssertMeasurement("db.client.connections.usage", 3);
		AssertMeasurement("db.client.connections.usage|idle", 3);
		AssertMeasurement("db.client.connections.usage|used", 0);
		Assert.Equal(3, Server.ActiveConnections);
	}

	[Fact(Skip = MetricsSkip)]
	public async Task PendingRequestForCreation()
	{
		var csb = CreateConnectionStringBuilder();
		PoolName = csb.GetConnectionString(includePassword: false);
		Server.ConnectDelay = TimeSpan.FromSeconds(0.5);

		AssertMeasurement("db.client.connections.pending_requests", 0);

		using var connection = new MySqlConnection(csb.ConnectionString);
		var openTask = connection.OpenAsync();
		AssertMeasurement("db.client.connections.pending_requests", 1);
		await openTask;

		AssertMeasurement("db.client.connections.pending_requests", 0);
	}

	[Fact(Skip = MetricsSkip)]
	public async Task PendingRequestForOpenFromPool()
	{
		var csb = CreateConnectionStringBuilder();
		PoolName = csb.GetConnectionString(includePassword: false);
		Server.ResetDelay = TimeSpan.FromSeconds(0.5);

		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();
		connection.Close();

		AssertMeasurement("db.client.connections.pending_requests", 0);

		var openTask = connection.OpenAsync();
		AssertMeasurement("db.client.connections.pending_requests", 1);
		await openTask;

		AssertMeasurement("db.client.connections.pending_requests", 0);
	}

	private IConnectionCreator CreateConnectionCreator(string spec, MySqlConnectionStringBuilder connectionStringBuilder)
	{
		var parts = spec.Split('|');
		return parts[0] switch
		{
			"DataSource" => new DataSourceConnectionCreator(bool.Parse(parts[1]), parts[2] == "" ? null : parts[2], parts[3] == "" ? null : parts[3], connectionStringBuilder),
			"Plain" => new PlainConnectionCreator(bool.Parse(parts[1]), parts[2] == "" ? null : parts[2], connectionStringBuilder),
			_ => throw new NotSupportedException(),
		};
	}
}
