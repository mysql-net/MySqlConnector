#if !MYSQL_DATA
#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SideBySide;

public class MySqlDataSourceTests : IClassFixture<DatabaseFixture>
{
	public MySqlDataSourceTests(DatabaseFixture _)
	{
	}

	[Fact]
	public void CreateConnectionConnectionString()
	{
		var connectionString = AppConfig.ConnectionString;
		using var dbSource = new MySqlDataSource(connectionString);
		using var connection = dbSource.CreateConnection();
		Assert.Equal(ConnectionState.Closed, connection.State);
		Assert.Equal(connectionString, connection.ConnectionString);
	}

	[Fact]
	public void OpenConnection()
	{
		using var dbSource = new MySqlDataSource(AppConfig.ConnectionString);
		using var connection = dbSource.OpenConnection();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[Fact]
	public async Task OpenConnectionAsync()
	{
		using var dbSource = new MySqlDataSource(AppConfig.ConnectionString);
		using var connection = await dbSource.OpenConnectionAsync();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[Fact]
	public void OpenConnectionReusesConnection()
	{
		using var dbSource = new MySqlDataSource(AppConfig.ConnectionString);

		int serverThread;
		using (var connection = dbSource.OpenConnection())
		{
			serverThread = connection.ServerThread;
		}

		using (var connection = dbSource.OpenConnection())
		{
			Assert.Equal(serverThread, connection.ServerThread);
		}
	}

	[Fact]
	public void MultipleDataSourcesHaveDifferentPools()
	{
		using var dbSource1 = new MySqlDataSource(AppConfig.ConnectionString);
		using var dbSource2 = new MySqlDataSource(AppConfig.ConnectionString);

		int serverThread;
		using (var connection = dbSource1.OpenConnection())
		{
			serverThread = connection.ServerThread;
		}

		using (var connection = dbSource2.OpenConnection())
		{
			Assert.NotEqual(serverThread, connection.ServerThread);
		}
	}

	[Fact]
	public void NonPoolingDataSourceDoesNotReuseConnections()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Pooling = false;
		using var dbSource = new MySqlDataSource(csb.ConnectionString);

		int serverThread;
		using (var connection = dbSource.OpenConnection())
		{
			serverThread = connection.ServerThread;
		}

		using (var connection = dbSource.OpenConnection())
		{
			Assert.NotEqual(serverThread, connection.ServerThread);
		}
	}

	[Fact]
	public void CreateFromDbFactory()
	{
		using var dbSource = MySqlConnectorFactory.Instance.CreateDataSource(AppConfig.ConnectionString);
		Assert.IsType<MySqlDataSource>(dbSource);
		Assert.Equal(AppConfig.ConnectionString, dbSource.ConnectionString);
	}
}
#endif
#endif
