using System.Data;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ConnectSync
	{
		[Fact]
		public void State()
		{
			using (var connection = new MySqlConnection(GetConnectionStringBuilder().ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				connection.Open();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

		[Fact]
		public void ServerVersion()
		{
			using (var connection = new MySqlConnection(GetConnectionStringBuilder().ConnectionString))
			{
				connection.Open();
				Assert.Equal(Constants.ServerVersion	, connection.ServerVersion);
			}
		}

		private MySqlConnectionStringBuilder GetConnectionStringBuilder()
		{
			return new MySqlConnectionStringBuilder
			{
				Server = Constants.Server,
				UserID = Constants.UserName,
				Password = Constants.Password,
			};
		}
	}
}
