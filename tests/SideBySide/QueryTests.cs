using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
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
					Assert.False(reader.NextResult());
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
				try
				{
					await cmd.ExecuteNonQueryAsync();
					Assert.True(false, "Exception should have been thrown.");
				}
				catch (MySqlException ex)
				{
					Assert.Equal((int) MySqlErrorCode.ParseError, ex.Number);
				}

				try
				{
					using (var reader = await cmd.ExecuteReaderAsync())
					{
					}
					Assert.True(false, "Exception should have been thrown.");
				}
				catch (MySqlException ex)
				{
					Assert.Equal((int) MySqlErrorCode.ParseError, ex.Number);
				}

				try
				{
					await cmd.ExecuteScalarAsync();
					Assert.True(false, "Exception should have been thrown.");
				}
				catch (MySqlException ex)
				{
					Assert.Equal((int) MySqlErrorCode.ParseError, ex.Number);
				}
			}

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"select count(id) from query_invalid_sql";
				Assert.Equal(0L, await cmd.ExecuteScalarAsync());
			}
		}

		[UnbufferedResultSetsFact]
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

					reader1.Dispose();
					using (cmd2.ExecuteReader())
					{
					}
					Assert.Equal(1, cmd2.ExecuteScalar());
				}
			}
		}

#if BASELINE
		[Fact(Skip = "Does not support BufferResultSets")]
#else
		[Fact]
#endif
		public async Task MultipleBufferedReaders()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
#if !BASELINE
			csb.BufferResultSets = true;
#endif

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				await connection.OpenAsync();
				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = @"drop table if exists query_multiple_buffered_readers;
						create table query_multiple_buffered_readers(id integer not null primary key auto_increment);
						insert into query_multiple_buffered_readers(id) values(1), (2), (3);";
					await cmd.ExecuteNonQueryAsync();
				}

				using (var cmd1 = connection.CreateCommand())
				using (var cmd2 = connection.CreateCommand())
				{
					var commandText = @"select id from query_multiple_buffered_readers order by id ASC;
						select id from query_multiple_buffered_readers order by id DESC;";
					cmd1.CommandText = commandText;
					cmd2.CommandText = commandText;

					var readers = new[]{ await cmd1.ExecuteReaderAsync(), await cmd2.ExecuteReaderAsync() };
					foreach (var reader in readers){
						Assert.True(await reader.ReadAsync());
						Assert.Equal(1, reader.GetInt32(0));
						Assert.True(await reader.ReadAsync());
						Assert.Equal(2, reader.GetInt32(0));
						Assert.True(await reader.ReadAsync());
						Assert.Equal(3, reader.GetInt32(0));
						Assert.False(await reader.ReadAsync());
						Assert.True(await reader.NextResultAsync());

						Assert.True(await reader.ReadAsync());
						Assert.Equal(3, reader.GetInt32(0));
						Assert.True(await reader.ReadAsync());
						Assert.Equal(2, reader.GetInt32(0));
						Assert.True(await reader.ReadAsync());
						Assert.Equal(1, reader.GetInt32(0));
						Assert.False(await reader.ReadAsync());
						Assert.False(await reader.NextResultAsync());
					}
				}
			}
		}

		[Fact]
		public async Task UndisposedReader()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = @"drop table if exists query_undisposed_reader;
					create table query_undisposed_reader(id integer not null primary key auto_increment);
					insert into query_undisposed_reader(id) values(1), (2), (3);";
				await cmd.ExecuteNonQueryAsync();
			}

			using (var cmd1 = m_database.Connection.CreateCommand())
			using (var cmd2 = m_database.Connection.CreateCommand())
			{
				var commandText = @"select id from query_undisposed_reader order by id;";
				cmd1.CommandText = commandText;
				cmd2.CommandText = commandText;

				var reader1 = await cmd1.ExecuteReaderAsync();
				Assert.True(reader1.Read());
				Assert.Equal(1, reader1.GetInt32(0));

				m_database.Connection.Close();
				m_database.Connection.Open();

				var reader2 = await cmd1.ExecuteReaderAsync();
				Assert.True(reader2.Read());
				Assert.Equal(1, reader2.GetInt32(0));
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
				cmd.CommandText = @"DROP TABLE IF EXISTS `userauth`;
CREATE TABLE `userauth` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserName` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Email` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `PrimaryEmail` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `FirstName` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `LastName` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `DisplayName` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `BirthDate` datetime DEFAULT NULL,
  `BirthDateRaw` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Country` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Culture` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `FullName` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Gender` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Language` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `MailAddress` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Nickname` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `PostalCode` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `TimeZone` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Salt` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `PasswordHash` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `DigestHA1Hash` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Roles` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Permissions` text COLLATE latin1_general_ci,
  `CreatedDate` datetime NOT NULL,
  `ModifiedDate` datetime NOT NULL,
  `RefId` int(11) DEFAULT NULL,
  `RefIdStr` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Meta` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `PhoneNumber` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Company` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Address` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `Address2` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `City` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `State` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  `InvalidLoginAttempts` int(11) NOT NULL,
  `LastLoginAttempt` datetime DEFAULT NULL,
  `LockedDate` datetime DEFAULT NULL,
  `RecoveryToken` varchar(255) COLLATE latin1_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB AUTO_INCREMENT=1 DEFAULT CHARSET=latin1 COLLATE=latin1_general_ci;

INSERT INTO `userauth` (`Id`, `UserName`, `Email`, `PrimaryEmail`, `FirstName`, `LastName`, `DisplayName`, `BirthDate`, `BirthDateRaw`, `Country`, `Culture`, `FullName`, `Gender`, `Language`, `MailAddress`, `Nickname`, `PostalCode`, `TimeZone`, `Salt`, `PasswordHash`, `DigestHA1Hash`, `Roles`, `Permissions`, `CreatedDate`, `ModifiedDate`, `RefId`, `RefIdStr`, `Meta`, `PhoneNumber`, `Company`, `Address`, `Address2`, `City`, `State`, `InvalidLoginAttempts`, `LastLoginAttempt`, `LockedDate`, `RecoveryToken`) VALUES (1,'12345',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'12345678','A1b816iuY267N2MnoD23lxLW0GSO0SxRtzDWM0s8pnVg','Dtse5agePM9Z0NGgUpXqkTmtuKwfId8W','[12345]','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','2017-06-22 10:34:56','2017-06-22 10:34:56',NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,0,NULL,NULL,NULL);
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

				//cmd.CommandText = "select " + string.Join(",", Enumerable.Range(0,50).Select(_ => $"'{Guid.NewGuid()}'").ToArray());
				cmd.CommandText =
					"SELECT `Id`, `UserName`, `Email`, `PrimaryEmail`, `PhoneNumber`, `FirstName`, `LastName`, `DisplayName`, `Company`, `BirthDate`, `BirthDateRaw`, `Address`, `Address2`, `City`, `State`, `Country`, `Culture`, `FullName`, `Gender`, `Language`, `MailAddress`, `Nickname`, `PostalCode`, `TimeZone`, `Salt`, `PasswordHash`, `DigestHa1Hash`, `Roles`, `Permissions`, `CreatedDate`, `ModifiedDate`, `InvalidLoginAttempts`, `LastLoginAttempt`, `LockedDate`, `RecoveryToken`, `RefId`, `RefIdStr`, `Meta` FROM `UserAuth` WHERE `Id` = @Id";
				cmd.Parameters.Add("Id", DbType.Int32).Value = 1;
				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read());
					var ex = Record.Exception(() => reader.GetName(0));
					Assert.Null(ex);
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
					Assert.True(await reader.ReadAsync().ConfigureAwait(false));
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

		[Fact]
		public void SumBytes()
		{
			m_database.Connection.Execute(@"drop table if exists sum_bytes;
			create table sum_bytes(value tinyint unsigned not null);
			insert into sum_bytes(value) values(0), (1), (2), (254), (255);");

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select sum(value) from sum_bytes";
				Assert.Equal(512m, cmd.ExecuteScalar());

				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal(512m, reader.GetValue(0));
					Assert.Equal(512, reader.GetInt32(0));
				}
			}
		}

		[Fact]
		public void SumShorts()
		{
			m_database.Connection.Execute(@"drop table if exists sum_shorts;
			create table sum_shorts(value smallint unsigned not null);
			insert into sum_shorts(value) values(0), (1), (2), (32766), (32767);");

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select sum(value) from sum_shorts";
				Assert.Equal(65536m, cmd.ExecuteScalar());

				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal(65536m, reader.GetValue(0));
					Assert.Throws<OverflowException>(() => reader.GetInt16(0));
					Assert.Equal(65536, reader.GetInt32(0));
					Assert.Equal(65536L, reader.GetInt64(0));
				}
			}
		}

		[Fact]
		public void SumInts()
		{
			m_database.Connection.Execute(@"drop table if exists sum_ints;
			create table sum_ints(value int unsigned not null);
			insert into sum_ints(value) values(0), (1), (2), (2147483646), (2147483647);");

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select sum(value) from sum_ints";
				Assert.Equal(4294967296m, cmd.ExecuteScalar());

				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal(4294967296m, reader.GetValue(0));
					Assert.Throws<OverflowException>(() => reader.GetInt32(0));
					Assert.Equal(4294967296L, reader.GetInt64(0));
				}
			}
		}

		[Fact]
		public void UseReaderWithoutDisposing()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.MaximumPoolSize = 8;

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Execute(@"drop table if exists dispose_reader;
					create table dispose_reader(value int not null);
					insert into dispose_reader(value) values(0), (1), (2), (3), (4), (5), (6), (7), (8), (9), (10);");
			}

			var threads = new List<Thread>();
			var threadData = new UseReaderWithoutDisposingThreadData(new List<Exception>(), csb);
			for (int i = 0; i < csb.MaximumPoolSize + 4; i++)
			{
				var thread = new Thread(UseReaderWithoutDisposingThread);
				threads.Add(thread);
				thread.Start(threadData);
			}
			foreach (var thread in threads)
				thread.Join();
			foreach (var ex in threadData.Exceptions)
				throw ex;
		}

		[Theory]
#if BASELINE
		[InlineData("null", typeof(string))]
#else
		[InlineData("null", typeof(object))]
#endif
		[InlineData("cast(null as char)", typeof(string))]
		[InlineData("1", typeof(long))]
		[InlineData("cast(1 as unsigned)", typeof(ulong))]
		[InlineData("1.0", typeof(decimal))]
		[InlineData("'text'", typeof(string))]
		[InlineData("cast('text' as char(4))", typeof(string))]
		[InlineData("cast('2000-01-02' as date)", typeof(DateTime))]
		[InlineData("cast('2000-01-02 13:45:56' as datetime)", typeof(DateTime))]
		[InlineData("cast('13:45:56' as time)", typeof(TimeSpan))]
		[InlineData("_binary'00112233'", typeof(byte[]))]
		[InlineData("sqrt(2)", typeof(double))]
		public void GetFieldType(string value, Type expectedType)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select " + value + ";";
				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal(expectedType, reader.GetFieldType(0));
				}
			}
		}

		[Theory]
#if BASELINE
		[InlineData("null", "VARCHAR")]
#else
		[InlineData("null", "NULL")]
#endif
		[InlineData("cast(null as char)", "VARCHAR")]
		[InlineData("1", "BIGINT")]
		[InlineData("cast(1 as unsigned)", "BIGINT")]
		[InlineData("1.0", "DECIMAL")]
		[InlineData("'text'", "VARCHAR")]
		[InlineData("cast('2000-01-02' as date)", "DATE")]
		[InlineData("cast('2000-01-02 13:45:56' as datetime)", "DATETIME")]
		[InlineData("cast('13:45:56' as time)", "TIME")]
		[InlineData("_binary'00112233'", "BLOB")]
		[InlineData("sqrt(2)", "DOUBLE")]
		public void GetDataTypeName(string value, string expectedDataType)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select " + value + ";";
				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal(expectedDataType, reader.GetDataTypeName(0));
				}
			}
		}

		private void UseReaderWithoutDisposingThread(object obj)
		{
			var data = (UseReaderWithoutDisposingThreadData) obj;

			try
			{
				for (int i = 0; i < 100; i++)
				{
					using (var connection = new MySqlConnection(data.ConnectionStringBuilder.ConnectionString))
					{
						connection.Open();
						using (var cmd = connection.CreateCommand())
						{
							cmd.CommandText = @"select * from dispose_reader;";
							var reader = cmd.ExecuteReader();
							reader.Read();
						}
					}
				}
			}
			catch (Exception ex)
			{
				lock (data)
					data.Exceptions.Add(ex);
			}
		}

		[Fact]
		public void ParameterDefaults()
		{
			var parameter = new MySqlParameter();
			Assert.Equal(DbType.AnsiString, parameter.DbType);
			Assert.Equal(ParameterDirection.Input, parameter.Direction);
			Assert.False(parameter.IsNullable);
			Assert.Null(parameter.ParameterName);
			Assert.Equal(0, parameter.Precision);
			Assert.Equal(0, parameter.Scale);
			Assert.Equal(0, parameter.Size);
			Assert.Null(parameter.Value);
		}

		[Fact]
		public void InputOutputParameter()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "set @param = 1234";

				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@param",
					Direction = ParameterDirection.InputOutput,
					Value = 123,
				});

				Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());

				// Issue #231: Assert.Equal(1234, cmd.Parameters["@param"].Value);
			}
		}

		[Fact]
		public void OutputParameter()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "set @param = 1234";

				cmd.Parameters.Add(new MySqlParameter
				{
					ParameterName = "@param",
					Direction = ParameterDirection.Output,
				});

				Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());

				// Issue #231: Assert.Equal(1234, cmd.Parameters["@param"].Value);
			}
		}

		[Fact]
		public void EnumParameter()
		{
			m_database.Connection.Execute(@"drop table if exists enum_test;
create table enum_test(id integer not null primary key, value text not null);
insert into enum_test (id, value) VALUES (1002, 'no'), (1003, 'yes');
");

			using (var command = new MySqlCommand("select * from enum_test where id = @ID;", m_database.Connection))
			{
				command.Parameters.AddWithValue("@ID", MySqlErrorCode.No);
				using (var reader = command.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal((int) MySqlErrorCode.No, reader.GetInt32(0));
					Assert.Equal("no", reader.GetString(1));
					Assert.False(reader.Read());
				}
			}
		}

		[Fact
#if BASELINE
			(Skip = "https://bugs.mysql.com/bug.php?id=84701")
#endif
		]
		public void Int64EnumParameter()
		{
			m_database.Connection.Execute(@"drop table if exists long_enum_test;
create table long_enum_test(id bigint not null primary key, value integer not null);
insert into long_enum_test (id, value) VALUES (0x7FFFFFFFFFFFFFFF, 1);
");

			using (var command = new MySqlCommand("select * from long_enum_test where id = @ID;", m_database.Connection))
			{
				command.Parameters.AddWithValue("@ID", TestLongEnum.Value);
				using (var reader = command.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal(long.MaxValue, reader.GetInt64(0));
					Assert.Equal(1, reader.GetInt32(1));
					Assert.False(reader.Read());
				}
			}
		}

		[Fact]
		public void ReturnDerivedTypes()
		{
			using (MySqlTransaction transaction = m_database.Connection.BeginTransaction())
			using (MySqlCommand command = m_database.Connection.CreateCommand())
			{
				command.CommandText = "select @param + @param2";

				MySqlParameter parameter = command.CreateParameter();
				parameter.ParameterName = "param";
				parameter.Value = 1;
				MySqlParameterCollection parameterCollection = command.Parameters;
				parameterCollection.Add(parameter);

				MySqlParameter parameter2 = parameterCollection.AddWithValue("param2", 2);

				MySqlParameter parameterB = parameterCollection[0];
				Assert.Same(parameter, parameterB);
				MySqlParameter parameter2B = parameterCollection["param2"];
				Assert.Same(parameter2, parameter2B);

				using (MySqlDataReader reader = command.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal(3L, reader.GetValue(0));
					Assert.False(reader.Read());
				}

				transaction.Rollback();
			}
		}

		[Theory]
		[InlineData(new[] { 1 }, new[] { true })]
		[InlineData(new[] { 4 }, new[] { false })]
		[InlineData(new[] { 1, 2 }, new[] { true, true })]
		[InlineData(new[] { 1, 4 }, new[] { true, false })]
		[InlineData(new[] { 4, 1 }, new[] { false, true })]
		[InlineData(new[] { 4, 5 }, new[] { false, false })]
		public void HasRows(int[] values, bool[] expecteds)
		{
			m_database.Connection.Execute(@"drop table if exists has_rows;
create table has_rows(value int not null);
insert into has_rows(value) values(1),(2),(3);");

			var sql = "";
			foreach (var value in values)
				sql += $"select * from has_rows where value = {value};";

			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = sql;
				using (var reader = cmd.ExecuteReader())
				{
					for (int i = 0; i < expecteds.Length; i++)
					{
						Assert.Equal(expecteds[i], reader.HasRows);
						Assert.Equal(expecteds[i], reader.Read());
						Assert.False(reader.Read());
						Assert.Equal(expecteds[i], reader.HasRows);

						Assert.Equal(i != expecteds.Length - 1, reader.NextResult());
					}
				}
			}
		}

		class BoolTest
		{
			public int Id { get; set; }
			public bool? IsBold { get; set; }
		}

		class UseReaderWithoutDisposingThreadData
		{
			public UseReaderWithoutDisposingThreadData(List<Exception> exceptions, MySqlConnectionStringBuilder csb)
			{
				Exceptions = exceptions;
				ConnectionStringBuilder = csb;
			}

			public List<Exception> Exceptions { get; }

			public MySqlConnectionStringBuilder ConnectionStringBuilder { get; }
		}

		enum TestLongEnum : long
		{
			Value = long.MaxValue,
		}

		readonly DatabaseFixture m_database;
	}
}
