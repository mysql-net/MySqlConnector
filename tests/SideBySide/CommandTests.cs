using System;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class CommandTests : IClassFixture<DatabaseFixture>
	{
		public CommandTests(DatabaseFixture database)
		{
			m_database = database;
		}

		[Fact]
		public void CreateCommandSetsConnection()
		{
			using (var command = m_database.Connection.CreateCommand())
			{
				Assert.Equal(m_database.Connection, command.Connection);
			}
		}

		[Fact]
		public void ExecuteReaderRequiresConnection()
		{
			using (var command = new MySqlCommand())
			{
				Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
			}
		}

		[Fact]
		public void ExecuteReaderRequiresOpenConnection()
		{
			using (var connection = new MySqlConnection())
			using (var command = connection.CreateCommand())
			{
				Assert.Throws<InvalidOperationException>(() => command.ExecuteReader());
			}
		}

		[Fact]
		public void PrepareRequiresConnection()
		{
			using (var command = new MySqlCommand())
			{
				Assert.Throws<InvalidOperationException>(() => command.Prepare());
			}
		}

		[Fact]
		public void PrepareRequiresOpenConnection()
		{
			using (var connection = new MySqlConnection())
			using (var command = connection.CreateCommand())
			{
				Assert.Throws<InvalidOperationException>(() => command.Prepare());
			}
		}

		[Fact]
		public void ExecuteNonQueryForSelectReturnsNegativeOne()
		{
			using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
			using (var command = connection.CreateCommand())
			{
				connection.Open();
				command.CommandText = "SELECT 1;";
				Assert.Equal(-1, command.ExecuteNonQuery());
			}
		}

		[Fact]
		public async Task ExecuteNonQueryReturnValue()
		{
			using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
			{
				await connection.OpenAsync();
				await connection.ExecuteAsync(@"drop table if exists execute_non_query;
create table execute_non_query(id integer not null primary key auto_increment, value text null);");
				Assert.Equal(4, await connection.ExecuteAsync("insert into execute_non_query(value) values(null), (null), ('one'), ('two');"));
				Assert.Equal(-1, await connection.ExecuteAsync("select value from execute_non_query;"));
				Assert.Equal(2, await connection.ExecuteAsync("delete from execute_non_query where value is null;"));
				Assert.Equal(1, await connection.ExecuteAsync("update execute_non_query set value = 'three' where value = 'one';"));
			}
		}

		readonly DatabaseFixture m_database;
	}
}
