using System.Data;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ConnectSync : IClassFixture<DatabaseFixture>
	{
		public ConnectSync(DatabaseFixture database)
		{
			m_database = database;
		}

		[Fact]
		public void ConnectBadHost()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = "invalid.example.com",
			};
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				Assert.Throws<MySqlException>(() => connection.Open());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public void ConnectBadPort()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = "localhost",
				Port = 65000,
			};
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				Assert.Throws<MySqlException>(() => connection.Open());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public void State()
		{
			using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				connection.Open();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

		[Fact]
		public void ServerVersion()
		{
			using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
			{
				connection.Open();
				Assert.Equal(Constants.ServerVersion, connection.ServerVersion);
			}
		}

		readonly DatabaseFixture m_database;
	}
}
