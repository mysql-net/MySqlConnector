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
		public async Task State()
		{
			using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
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
