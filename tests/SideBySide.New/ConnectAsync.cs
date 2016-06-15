using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ConnectAsync : IClassFixture<DatabaseFixture>
	{
		public ConnectAsync(DatabaseFixture database)
		{
			m_database = database;
		}

		[Fact]
		public async Task ConnectBadHost()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = "invalid.example.com",
			};
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				await Assert.ThrowsAsync<MySqlException>(() => connection.OpenAsync());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public async Task ConnectBadPort()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = "localhost",
				Port = 65000,
			};
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				await Assert.ThrowsAsync<MySqlException>(() => connection.OpenAsync());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public async Task ConnectBadPassword()
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.Password = "wrong";
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				await Assert.ThrowsAsync<MySqlException>(() => connection.OpenAsync());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public async Task State()
		{
			using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				await connection.OpenAsync();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

#if BASELINE
		[Fact(Skip = "Doesn't implement \"Multiple hosts can be specified separated by commas.\" behavior as documented.")]
#else
		[Fact]
#endif
		public async Task ConnectMultipleHostNames()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = "www.mysql.com,invalid.example.net,localhost",
				Port = 3306,
				UserID = Constants.UserName,
				Password = Constants.Password,
			};
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				await connection.OpenAsync();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

		[Fact]
		public async Task ServerVersion()
		{
			using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
			{
				await connection.OpenAsync();
				Assert.Equal(Constants.ServerVersion, connection.ServerVersion);
			}
		}

		readonly DatabaseFixture m_database;
	}
}
