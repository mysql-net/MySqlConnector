using System.Threading.Tasks;
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
			var csb = Constants.CreateConnectionStringBuilder();
			csb.AllowUserVariables = false;
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
			var csb = Constants.CreateConnectionStringBuilder();
			csb.AllowUserVariables = true;
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				var cmd = connection.CreateCommand();
				cmd.CommandText = "set @var = 1; select @var + 1;";
				Assert.Equal(2L, cmd.ExecuteScalar());
			}
		}

		[Fact]
		public async Task InvalidSql()
		{
			await m_database.Connection.OpenAsync();
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop schema if exists invalid_sql;
create schema invalid_sql;
create table invalid_sql.test(id integer not null primary key auto_increment);";
				await cmd.ExecuteNonQueryAsync();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select id from invalid_sql.test limit 1 where id is not null";
				await Assert.ThrowsAsync<MySqlException>(() => cmd.ExecuteNonQueryAsync());
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select count(id) from invalid_sql.test";
				Assert.Equal(0L, await cmd.ExecuteScalarAsync());
			}
		}

		readonly DatabaseFixture m_database;
	}
}
