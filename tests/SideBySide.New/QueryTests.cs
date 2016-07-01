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
				await Assert.ThrowsAsync<MySqlException>(() => cmd.ExecuteReaderAsync());
				await Assert.ThrowsAsync<MySqlException>(() => cmd.ExecuteScalarAsync());
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select count(id) from invalid_sql.test";
				Assert.Equal(0L, await cmd.ExecuteScalarAsync());
			}
		}


		[Fact]
		public async Task MultipleReaders()
		{
			var csb = Constants.CreateConnectionStringBuilder();
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				await connection.OpenAsync();

				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = @"drop schema if exists multiple_readers;
						create schema multiple_readers;
						create table multiple_readers.test(id integer not null primary key auto_increment);
						insert into multiple_readers.test(id) values(1), (2), (3);";
					await cmd.ExecuteNonQueryAsync();
				}

				using (var cmd1 = connection.CreateCommand())
				using (var cmd2 = connection.CreateCommand())
				{
					cmd1.CommandText = @"select id from multiple_readers.test;";
					cmd2.CommandText = @"select id from multiple_readers.test order by id;";

					using (var reader1 = await cmd1.ExecuteReaderAsync())
					{
						Assert.Throws<MySqlException>(() => cmd2.ExecuteReader());
						Assert.Throws<MySqlException>(() => cmd2.ExecuteScalar());
						do
						{
							while (await reader1.ReadAsync())
							{
								Assert.Throws<MySqlException>(() => cmd2.ExecuteReader());
								Assert.Throws<MySqlException>(() => cmd2.ExecuteScalar());
							}
							Assert.Throws<MySqlException>(() => cmd2.ExecuteReader());
							Assert.Throws<MySqlException>(() => cmd2.ExecuteScalar());
						} while (await reader1.NextResultAsync());

						Assert.Throws<MySqlException>(() => cmd2.ExecuteReader());
						Assert.Throws<MySqlException>(() => cmd2.ExecuteScalar());
#if NET45
					reader1.Close();
					using (var reader2 = cmd2.ExecuteReader())
					{
						// TODO: Remove this loop when disposing an incomplete reader cleans it up properly
						while (reader2.Read())
						{
						}
					}
					Assert.Equal(1, cmd2.ExecuteScalar());
#endif
					}

					using (var reader2 = cmd2.ExecuteReader())
					{
						// TODO: Remove this loop when disposing an incomplete reader cleans it up properly
						while (reader2.Read())
						{
						}
					}
					Assert.Equal(1, cmd2.ExecuteScalar());
				}
			}
		}

		readonly DatabaseFixture m_database;
	}
}
