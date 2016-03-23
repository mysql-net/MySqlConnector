using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ConnectAsync
	{
		[Fact]
		public async Task State()
		{
			using (var connection = new MySqlConnection(GetConnectionStringBuilder().ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				await connection.OpenAsync();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

		[Fact]
		public async Task ServerVersion()
		{
			using (var connection = new MySqlConnection(GetConnectionStringBuilder().ConnectionString))
			{
				await connection.OpenAsync();
				Assert.Equal(Constants.ServerVersion, connection.ServerVersion);
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
