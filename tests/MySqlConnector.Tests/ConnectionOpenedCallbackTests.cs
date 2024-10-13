using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlConnector.Tests;

public class ConnectionOpenedCallbackTests : IDisposable
{
	public ConnectionOpenedCallbackTests()
	{
		m_server = new();
		m_server.Start();

		m_csb = new MySqlConnectionStringBuilder()
		{
			Server = "localhost",
			Port = (uint) m_server.Port,
		};
		m_dataSource = new MySqlDataSourceBuilder(m_csb.ConnectionString)
			.UseConnectionOpenedCallback(OnConnectionOpenedAsync)
			.Build();
	}

	public void Dispose()
	{
		m_dataSource.Dispose();
		m_server.Stop();
	}

	[Fact]
	public void CallbackIsInvoked()
	{
		using (var connection = m_dataSource.CreateConnection())
		{
			Assert.Equal(0, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.None, m_connectionOpenedConditions);

			connection.Open();

			Assert.Equal(1, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.New, m_connectionOpenedConditions);
		}
	}

	[Fact]
	public void SyncCallbackIsInvoked()
	{
		var dataSource = new MySqlDataSourceBuilder(m_csb.ConnectionString)
			.UseConnectionOpenedCallback(data =>
			{
				m_connectionOpenedCount++;
				m_connectionOpenedConditions = data.Conditions;
			})
			.Build();
		using (var connection = dataSource.CreateConnection())
		{
			Assert.Equal(0, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.None, m_connectionOpenedConditions);

			connection.Open();

			Assert.Equal(1, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.New, m_connectionOpenedConditions);
		}
	}

	[Fact]
	public void CallbackIsInvokedForPooledConnection()
	{
		using (var connection = m_dataSource.CreateConnection())
		{
			Assert.Equal(0, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.None, m_connectionOpenedConditions);

			connection.Open();

			Assert.Equal(1, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.New, m_connectionOpenedConditions);
		}

		using (var connection = m_dataSource.OpenConnection())
		{
			Assert.Equal(2, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.Reset, m_connectionOpenedConditions);
		}

		using (var connection = m_dataSource.OpenConnection())
		{
			Assert.Equal(3, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.Reset, m_connectionOpenedConditions);
		}
	}

	[Fact]
	public void CallbackIsInvokedForNonPooledConnection()
	{
		var csb = new MySqlConnectionStringBuilder(m_csb.ConnectionString)
		{
			Pooling = false,
		};
		using var dataSource = new MySqlDataSourceBuilder(csb.ConnectionString)
			.UseConnectionOpenedCallback(OnConnectionOpenedAsync)
			.Build();

		using (var connection = dataSource.OpenConnection())
		{
			Assert.Equal(1, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.New, m_connectionOpenedConditions);
		}

		using (var connection = dataSource.OpenConnection())
		{
			Assert.Equal(2, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.New, m_connectionOpenedConditions);
		}

		using (var connection = dataSource.OpenConnection())
		{
			Assert.Equal(3, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.New, m_connectionOpenedConditions);
		}
	}

	[Fact]
	public void ConditionsForNonResetConnection()
	{
		var csb = new MySqlConnectionStringBuilder(m_csb.ConnectionString)
		{
			ConnectionReset = false,
		};
		using var dataSource = new MySqlDataSourceBuilder(csb.ConnectionString)
			.UseConnectionOpenedCallback(OnConnectionOpenedAsync)
			.Build();

		using (var connection = dataSource.OpenConnection())
		{
			Assert.Equal(1, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.New, m_connectionOpenedConditions);
		}
		using (var connection = dataSource.OpenConnection())
		{
			Assert.Equal(2, m_connectionOpenedCount);
			Assert.Equal(MySqlConnectionOpenedConditions.None, m_connectionOpenedConditions);
		}
	}

	private ValueTask OnConnectionOpenedAsync(MySqlConnectionOpenedData data)
	{
		m_connectionOpenedCount++;
		m_connectionOpenedConditions = data.Conditions;
		return default;
	}

	private readonly FakeMySqlServer m_server;
	private readonly MySqlConnectionStringBuilder m_csb;
	private readonly MySqlDataSource m_dataSource;

	private int m_connectionOpenedCount;
	private MySqlConnectionOpenedConditions m_connectionOpenedConditions;
}
