#nullable enable

namespace MySqlConnector.Tests.Metrics;

public class ConnectionsUsageTests : MetricsTestsBase
{
    [Theory(Skip = MetricsSkip)]
	[MemberData(nameof(GetPoolCreators))]
	public void PoolCreators(IConnectionCreator connectionCreator)
    {
		connectionCreator.SetConnectionStringBuilder(CreateConnectionStringBuilder());
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

		connectionCreator.Dispose();
	}

	public static IEnumerable<object[]> GetPoolCreators()
	{
		yield return new object[] { new DataSourceConnectionCreator("metrics-test") };
		yield return new object[] { new DataSourceConnectionCreator(null) };
		yield return new object[] { new PlainConnectionCreator() };
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
}
