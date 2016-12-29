using System;
using System.Data;
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
			var csb = AppConfig.CreateConnectionStringBuilder();
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
			var csb = AppConfig.CreateConnectionStringBuilder();
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
				cmd.CommandText = @"drop table if exists query_test;
create table query_test(id integer not null primary key auto_increment, value integer not null);
insert into query_test (value) VALUES (1);
";
				cmd.ExecuteNonQuery();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select id, value FROM query_test;";
				using (var reader = cmd.ExecuteReader())
					Assert.Equal(false, reader.NextResult());
			}
		}

		[Fact]
		public async Task InvalidSql()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop table if exists query_invalid_sql;
create table query_invalid_sql(id integer not null primary key auto_increment);";
				await cmd.ExecuteNonQueryAsync();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select id from query_invalid_sql limit 1 where id is not null";
				await Assert.ThrowsAsync<MySqlException>(() => cmd.ExecuteNonQueryAsync());
				await Assert.ThrowsAsync<MySqlException>(() => cmd.ExecuteReaderAsync());
				await Assert.ThrowsAsync<MySqlException>(() => cmd.ExecuteScalarAsync());
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select count(id) from query_invalid_sql";
				Assert.Equal(0L, await cmd.ExecuteScalarAsync());
			}
		}

		[Fact]
		public async Task MultipleReaders()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop table if exists query_multiple_readers;
					create table query_multiple_readers(id integer not null primary key auto_increment);
					insert into query_multiple_readers(id) values(1), (2), (3);";
				await cmd.ExecuteNonQueryAsync();
			}

			using (var cmd1 = m_database.Connection.CreateCommand())
			using (var cmd2 = m_database.Connection.CreateCommand())
			{
				cmd1.CommandText = @"select id from query_multiple_readers;";
				cmd2.CommandText = @"select id from query_multiple_readers order by id;";

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

#if NET451
					reader1.Close();
#else
					reader1.Dispose();
#endif
					using (cmd2.ExecuteReader())
					{
					}
					Assert.Equal(1, cmd2.ExecuteScalar());
				}
			}
		}

		[Fact]
		public async Task MultipleStatements()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop table if exists query_multiple_statements;
					create table query_multiple_statements(value1 int not null, value2 int not null, value3 int not null);
					insert into query_multiple_statements(value1, value2, value3) values(1, 2, 3), (4, 5, 6), (7, 8, 9);";
				await cmd.ExecuteNonQueryAsync();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select value1 from query_multiple_statements order by value1;
					select value2 from query_multiple_statements order by value2;
					select value3 from query_multiple_statements order by value3;";

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
				cmd.CommandText = @"drop table if exists query_get_name;
create table query_get_name(id integer not null primary key, value text not null);
insert into query_get_name (id, value) VALUES (1, 'one'), (2, 'two');
";
				cmd.ExecuteNonQuery();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select id, value FROM query_get_name order by id;";
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

				cmd.CommandText = "select id, value FROM query_get_name where id > 10 order by id;";
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
		public async Task ParameterIsNull()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop table if exists query_null_parameter;
create table query_null_parameter(id integer not null primary key, value text);
insert into query_null_parameter (id, value) VALUES (1, 'one'), (2, 'two'), (3, null);
";
				cmd.ExecuteNonQuery();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select id, value FROM query_null_parameter where @parameter is null or value = @parameter order by id;";
				cmd.Parameters.Add(new MySqlParameter { ParameterName = "@parameter", Value = "one" });
				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(await reader.ReadAsync());
					Assert.Equal(1L, reader.GetInt64(0));
					Assert.False(await reader.ReadAsync());
					Assert.False(await reader.NextResultAsync());
				}
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select id, value FROM query_null_parameter where @parameter is null or value = @parameter order by id;";
				cmd.Parameters.Add(new MySqlParameter { ParameterName = "@parameter", Value = null });
				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(await reader.ReadAsync());
					Assert.Equal(1L, reader.GetInt64(0));
					Assert.True(await reader.ReadAsync());
					Assert.Equal(2L, reader.GetInt64(0));
					Assert.True(await reader.ReadAsync());
					Assert.Equal(3L, reader.GetInt64(0));
					Assert.False(await reader.ReadAsync());
					Assert.False(await reader.NextResultAsync());
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
			m_database.Connection.Execute("drop table if exists query_bool_test;");
			m_database.Connection.Execute("create table query_bool_test (Id int not null, IsBold BOOLEAN null );");
			m_database.Connection.Execute("insert query_bool_test (Id, IsBold) values (1,1);");
			m_database.Connection.Execute("insert query_bool_test (Id, IsBold) values (2,null);");

			var rows = m_database.Connection.Query<BoolTest>("select * from query_bool_test").ToDictionary(x => x.Id);

			Assert.True(rows[1].IsBold);
			Assert.Null(rows[2].IsBold);
		}


		[Fact]
		public async Task GetEnumerator()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop table if exists query_enumerator;
					create table query_enumerator(value text);
					insert into query_enumerator(value) values('one'), ('two'), ('three'), ('four');";
				await cmd.ExecuteNonQueryAsync();
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select value from query_enumerator order by value asc;";
				using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
					Assert.Equal(new[] { "four", "one", "three", "two" }, reader.Cast<IDataRecord>().Select(x => x.GetString(0)));
			}
		}

#if BASELINE
		[Fact(Skip = "http://bugs.mysql.com/bug.php?id=82292")]
#else
		[Fact]
#endif
		public void DapperNullableBoolNullFirst()
		{
			// adapted from https://github.com/StackExchange/dapper-dot-net/issues/552
			m_database.Connection.Execute("drop table if exists query_dapper_bool_test;");
			m_database.Connection.Execute("create table query_dapper_bool_test (Id int not null, IsBold BOOLEAN null );");
			m_database.Connection.Execute("insert query_dapper_bool_test (Id, IsBold) values (2,null);");
			m_database.Connection.Execute("insert query_dapper_bool_test (Id, IsBold) values (1,1);");

			var rows = m_database.Connection.Query<BoolTest>("select * from query_dapper_bool_test").ToDictionary(x => x.Id);

			Assert.True(rows[1].IsBold);
			Assert.Null(rows[2].IsBold);
		}

#if BASELINE
		[Fact(Skip = "https://bugs.mysql.com/bug.php?id=78760")]
#else
		[Fact]
#endif
		public void TabsAndNewLines()
		{
			m_database.Connection.Execute(@"drop table if exists query_tabs;
			create table query_tabs(
				id bigint(20) not null primary key
			);
			insert into query_tabs(id) values(1);");

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select\ncount(*)\nfrom\nquery_tabs;";
				Assert.Equal(1L, (long) cmd.ExecuteScalar());
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select\tcount(*)\n\t\tfrom\tquery_tabs;";
				Assert.Equal(1L, (long) cmd.ExecuteScalar());
			}
		}

		class BoolTest
		{
			public int Id { get; set; }
			public bool? IsBold { get; set; }
		}

		readonly DatabaseFixture m_database;
	}
}
