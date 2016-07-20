using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class QueryTests : IClassFixture<DatabaseFixture>, IDisposable
	{
		public QueryTests(DatabaseFixture database)
		{
			m_database = database;
			m_database.Connection.Open();
		}

		public void Dispose()
		{
			m_database.Connection.Close();
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
		public void NextResultBeforeRead()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop schema if exists query_test;
drop table if exists query_test.test;
create schema query_test;
create table query_test.test(id integer not null primary key auto_increment, value integer not null);
insert into query_test.test (value) VALUES (1);
";
				cmd.ExecuteNonQuery();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select id, value FROM query_test.test;";
				using (var reader = cmd.ExecuteReader())
					Assert.Equal(false, reader.NextResult());
			}
		}

		[Fact]
		public async Task InvalidSql()
		{
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
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop schema if exists multiple_readers;
					create schema multiple_readers;
					create table multiple_readers.test(id integer not null primary key auto_increment);
					insert into multiple_readers.test(id) values(1), (2), (3);";
				await cmd.ExecuteNonQueryAsync();
			}

			using (var cmd1 = m_database.Connection.CreateCommand())
			using (var cmd2 = m_database.Connection.CreateCommand())
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
					}
					Assert.Equal(1, cmd2.ExecuteScalar());
#endif
				}

				using (var reader2 = cmd2.ExecuteReader())
				{
				}
				Assert.Equal(1, cmd2.ExecuteScalar());
			}
		}

		[Fact]
		public async Task MultipleStatements()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop schema if exists multiple_statements;
					create schema multiple_statements;
					create table multiple_statements.test(value1 int not null, value2 int not null, value3 int not null);
					insert into multiple_statements.test(value1, value2, value3) values(1, 2, 3), (4, 5, 6), (7, 8, 9);";
				await cmd.ExecuteNonQueryAsync();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select value1 from multiple_statements.test order by value1;
					select value2 from multiple_statements.test order by value2;
					select value3 from multiple_statements.test order by value3;";

				using (var reader = await cmd.ExecuteReaderAsync())
				{
					Assert.True(await reader.NextResultAsync());
					Assert.True(await reader.NextResultAsync());
					Assert.True(await reader.ReadAsync());
					Assert.Equal(3, reader.GetInt32(0));
					Assert.False(await reader.NextResultAsync());
				}
			}
		}

		[Fact]
		public async Task GetName()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop schema if exists get_name;
create schema get_name;
create table get_name.test(id integer not null primary key, value text not null);
insert into get_name.test (id, value) VALUES (1, 'one'), (2, 'two');
";
				cmd.ExecuteNonQuery();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select id, value FROM get_name.test order by id;";
				using (var reader = cmd.ExecuteReader())
				{
					Assert.Equal("id", reader.GetName(0));
					Assert.Equal("value", reader.GetName(1));
					Assert.Throws<IndexOutOfRangeException>(() => reader.GetName(2));
					Assert.True(await reader.ReadAsync());
					Assert.Equal("id", reader.GetName(0));
					Assert.True(await reader.ReadAsync());
					Assert.Equal("id", reader.GetName(0));
					Assert.False(await reader.ReadAsync());
					Assert.Equal("id", reader.GetName(0));

					Assert.False(await reader.NextResultAsync());
					Assert.Throws<IndexOutOfRangeException>(() => reader.GetName(0));
				}

				cmd.CommandText = "select id, value FROM get_name.test where id > 10 order by id;";
				using (var reader = cmd.ExecuteReader())
				{
					Assert.Equal("id", reader.GetName(0));
					Assert.Equal("value", reader.GetName(1));
					Assert.Throws<IndexOutOfRangeException>(() => reader.GetName(2));
					Assert.False(await reader.ReadAsync());
					Assert.Equal("id", reader.GetName(0));

					Assert.False(await reader.NextResultAsync());
					Assert.Throws<IndexOutOfRangeException>(() => reader.GetName(0));
				}
			}
		}


		[Fact]
		public async Task DoubleDispose()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select 1;";
				using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
				{
					Assert.Equal(true, await reader.ReadAsync().ConfigureAwait(false));
					reader.Dispose();
					reader.Dispose();
				}
			}
		}

		[Fact]
		public async Task MultipleStatementsWithInvalidSql()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select 1; select 1 from mysql.abc; select 2;";
				using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
				{
					Assert.True(await reader.ReadAsync().ConfigureAwait(false));
					Assert.Equal(1, reader.GetInt32(0));
					Assert.False(await reader.ReadAsync().ConfigureAwait(false));

					await Assert.ThrowsAsync<MySqlException>(() => reader.NextResultAsync());
					Assert.False(await reader.ReadAsync().ConfigureAwait(false));

					Assert.False(await reader.NextResultAsync().ConfigureAwait(false));
				}
			}
		}

		[Fact]
		public void DapperNullableBoolNullLast()
		{
			// adapted from https://github.com/StackExchange/dapper-dot-net/issues/552
			m_database.Connection.Execute("drop schema if exists boolTest; create schema boolTest;");
			m_database.Connection.Execute("create table boolTest.Test (Id int not null, IsBold BOOLEAN null );");
			m_database.Connection.Execute("insert boolTest.Test (Id, IsBold) values (1,1);");
			m_database.Connection.Execute("insert boolTest.Test (Id, IsBold) values (2,null);");

			var rows = m_database.Connection.Query<BoolTest>("select * from boolTest.Test").ToDictionary(x => x.Id);

			Assert.True(rows[1].IsBold);
			Assert.Null(rows[2].IsBold);
		}

#if BASELINE
		[Fact(Skip = "Broken by http://bugs.mysql.com/bug.php?id=82292")]
#else
		[Fact]
#endif
		public void DapperNullableBoolNullFirst()
		{
			// adapted from https://github.com/StackExchange/dapper-dot-net/issues/552
			m_database.Connection.Execute("drop schema if exists boolTest; create schema boolTest;");
			m_database.Connection.Execute("create table boolTest.Test (Id int not null, IsBold BOOLEAN null );");
			m_database.Connection.Execute("insert boolTest.Test (Id, IsBold) values (2,null);");
			m_database.Connection.Execute("insert boolTest.Test (Id, IsBold) values (1,1);");

			var rows = m_database.Connection.Query<BoolTest>("select * from boolTest.Test").ToDictionary(x => x.Id);

			Assert.True(rows[1].IsBold);
			Assert.Null(rows[2].IsBold);
		}

		class BoolTest
		{
			public int Id { get; set; }
			public bool? IsBold { get; set; }
		}

		readonly DatabaseFixture m_database;
	}
}
