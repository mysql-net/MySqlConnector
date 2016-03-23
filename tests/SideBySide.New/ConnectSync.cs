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
