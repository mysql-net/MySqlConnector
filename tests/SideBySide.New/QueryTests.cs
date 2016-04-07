using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class QueryTests : IClassFixture<DatabaseFixture>
	{
		public QueryTests(DatabaseFixture database)
		{
			m_database = database;
		}

		[Fact]
		public void WithoutUserVariables()
		{
			var csb = new MySqlConnectionStringBuilder(m_database.Connection.ConnectionString) { AllowUserVariables = false };
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				var cmd = connection.CreateCommand();
				cmd.CommandText = "set @var = 1; select @var + 1;";
				Assert.Throws<MySqlException>(() => cmd.ExecuteScalar());
			}
		}

		[Fact]
		public void WithUserVariables()
		{
			var csb = new MySqlConnectionStringBuilder(m_database.Connection.ConnectionString) { AllowUserVariables = true };
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				var cmd = connection.CreateCommand();
				cmd.CommandText = "set @var = 1; select @var + 1;";
				Assert.Equal(2L, cmd.ExecuteScalar());
			}
		}

		readonly DatabaseFixture m_database;
	}
}
