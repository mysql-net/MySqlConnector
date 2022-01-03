namespace SideBySide;

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

	[Theory]
	[InlineData(true, false, false)]
	[InlineData(false, true, false)]
	[InlineData(false, false, true)]
	public void Bug1096(bool sprocBeforeInsert, bool sprocAfterInsert, bool sprocTwiceBeforeInsert)
	{
		var csb = new MySqlConnectionStringBuilder(AppConfig.ConnectionString);
		csb.UseAffectedRows = true;
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		connection.Execute(@"
DROP TABLE IF EXISTS bug_1096;
CREATE TABLE bug_1096 (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR (50) NOT NULL,
  PRIMARY KEY (`Id`)
);

DROP PROCEDURE IF EXISTS sp_bug_1096;
CREATE PROCEDURE sp_bug_1096 (in pId INT, IN pName VARCHAR(50))
BEGIN
UPDATE bug_1096 SET `Name` = pName WHERE (`Id` = pId);
SELECT 0 AS ResultCode;
END;

INSERT INTO bug_1096 (`Name`) VALUES ('Demo-Name');");

		var sproc = "CALL sp_bug_1096 (1, 'Demo-Name-Updated');";
		var insert = "INSERT INTO bug_1096 (`Name`) VALUES ('Demo-Name-Updated-Batch');";
		var expectedRowsAffected = connection.ServerVersion.IndexOf("MariaDB") == -1 ? 1 : 2;

		if (sprocBeforeInsert)
		{
			var rowsAffected = connection.Execute(sproc + insert);
			Assert.Equal(expectedRowsAffected, rowsAffected);
		}
		else if (sprocAfterInsert)
		{
			var rowsAffected = connection.Execute(insert + sproc);
			Assert.Equal(expectedRowsAffected, rowsAffected);
		}
		else if (sprocTwiceBeforeInsert)
		{
			var rowsAffected = connection.Execute(sproc + sproc + insert);
			Assert.Equal(expectedRowsAffected, rowsAffected);
		}
	}

	[Fact]
	public void GetOrdinal()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 0 as zero, 1 as one;";

		using var reader = cmd.ExecuteReader();
		Assert.Equal(0, reader.GetOrdinal("zero"));
		Assert.Equal(1, reader.GetOrdinal("one"));
	}

	[Fact]
	public void GetOrdinalIgnoreCase()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 0 as zero, 1 as one;";

		using var reader = cmd.ExecuteReader();
		Assert.Equal(0, reader.GetOrdinal("Zero"));
		Assert.Equal(1, reader.GetOrdinal("ONE"));
	}

	[Fact]
	public void GetOrdinalExceptionForNoColumn()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 0 as zero, 1 as one;";

		using var reader = cmd.ExecuteReader();
		Assert.Throws<IndexOutOfRangeException>(() => reader.GetOrdinal("three"));
	}

	[Fact]
	public void GetOrdinalExceptionForNull()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 0 as zero, 1 as one;";

		using var reader = cmd.ExecuteReader();
		Assert.Throws<ArgumentNullException>(() => reader.GetOrdinal(null));
	}

	[Fact]
	public void GetOrdinalAfterNextResult()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 0 as zero, 1 as one;";
		using var reader = cmd.ExecuteReader();
		Assert.False(reader.NextResult());
#if BASELINE
		Assert.Throws<Exception>(() => reader.GetOrdinal("zero"));
#else
		Assert.Throws<InvalidOperationException>(() => reader.GetOrdinal("zero"));
#endif
	}

	[Fact]
	public void GetOrdinalBeforeAndAfterRead()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 0 as zero, 1 as one;";

		using var reader = cmd.ExecuteReader();
		Assert.Equal(1, reader.GetOrdinal("one"));
		Assert.True(reader.Read());
		Assert.Equal(1, reader.GetOrdinal("one"));
		Assert.False(reader.Read());
		Assert.Equal(1, reader.GetOrdinal("one"));
	}

	[Fact]
	public void WithoutUserVariables()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.AllowUserVariables = false;
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		using var cmd = connection.CreateCommand();
		cmd.CommandText = "set @var = 1; select @var + 1;";
		Assert.Throws<MySqlException>(() => cmd.ExecuteScalar());
	}

	[Fact]
	public void WithUserVariables()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.AllowUserVariables = true;
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		using var cmd = connection.CreateCommand();
		cmd.CommandText = "set @var = 1; select @var + 1;";
		Assert.Equal(2L, cmd.ExecuteScalar());
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
			using var reader = cmd.ExecuteReader();
			Assert.False(reader.NextResult());
		}
	}

	[Fact]
	public void ReadAfterDispose()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "SELECT 1;";
		using var reader = cmd.ExecuteReader();
		reader.Dispose();
#if BASELINE
		Assert.Throws<MySqlException>(() => reader.Read());
#else
		Assert.Throws<InvalidOperationException>(() => reader.Read());
#endif
	}

	[Fact]
	public async Task ReadAsyncAfterDispose()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "SELECT 1;";
		using var reader = cmd.ExecuteReader();
		reader.Dispose();
#if BASELINE
		await Assert.ThrowsAsync<MySqlException>(() => reader.ReadAsync());
#else
		await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadAsync());
#endif
	}

	[Fact]
	public void NextResultAfterDispose()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "SELECT 1;";
		using var reader = cmd.ExecuteReader();
		reader.Dispose();
#if BASELINE
		Assert.Throws<MySqlException>(() => reader.NextResult());
#else
		Assert.Throws<InvalidOperationException>(() => reader.NextResult());
#endif
	}

	[Fact]
	public async Task NextResultAsyncAfterDispose()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "SELECT 1;";
		using var reader = cmd.ExecuteReader();
		reader.Dispose();
#if BASELINE
		await Assert.ThrowsAsync<MySqlException>(() => reader.NextResultAsync());
#else
		await Assert.ThrowsAsync<InvalidOperationException>(() => reader.NextResultAsync());
#endif
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
			var ex = await Assert.ThrowsAsync<MySqlException>(cmd.ExecuteNonQueryAsync);
			Assert.Equal((int) MySqlErrorCode.ParseError, ex.Number);

			ex = await Assert.ThrowsAsync<MySqlException>(cmd.ExecuteReaderAsync);
			Assert.Equal((int) MySqlErrorCode.ParseError, ex.Number);

			ex = await Assert.ThrowsAsync<MySqlException>(cmd.ExecuteScalarAsync);
			Assert.Equal((int) MySqlErrorCode.ParseError, ex.Number);
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
#if BASELINE
		var exceptionType = typeof(MySqlException);
#else
		var exceptionType = typeof(InvalidOperationException);
#endif
		using (var cmd = m_database.Connection.CreateCommand())
		{
			cmd.CommandText = @"drop table if exists query_multiple_readers;
				create table query_multiple_readers(id integer not null primary key auto_increment);
				insert into query_multiple_readers(id) values(1), (2), (3);";
			await cmd.ExecuteNonQueryAsync();
		}

		using var cmd1 = m_database.Connection.CreateCommand();
		cmd1.CommandText = @"select id from query_multiple_readers;";
		using var cmd2 = m_database.Connection.CreateCommand();
		cmd2.CommandText = @"select id from query_multiple_readers order by id;";

		using var reader1 = await cmd1.ExecuteReaderAsync();
		Assert.Throws(exceptionType, () => cmd2.ExecuteReader());
		Assert.Throws(exceptionType, () => cmd2.ExecuteScalar());
		do
		{
			while (await reader1.ReadAsync())
			{
				Assert.Throws(exceptionType, () => cmd2.ExecuteReader());
				Assert.Throws(exceptionType, () => cmd2.ExecuteScalar());
			}
			Assert.Throws(exceptionType, () => cmd2.ExecuteReader());
			Assert.Throws(exceptionType, () => cmd2.ExecuteScalar());
		} while (await reader1.NextResultAsync());

		Assert.Throws(exceptionType, () => cmd2.ExecuteReader());
		Assert.Throws(exceptionType, () => cmd2.ExecuteScalar());

		reader1.Dispose();
		using (cmd2.ExecuteReader())
		{
		}
		Assert.Equal(1, cmd2.ExecuteScalar());
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

		using var cmd1 = m_database.Connection.CreateCommand();
		using var cmd2 = m_database.Connection.CreateCommand();
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

			using var reader = await cmd.ExecuteReaderAsync();
			Assert.True(await reader.NextResultAsync());
			Assert.True(await reader.NextResultAsync());
			Assert.True(await reader.ReadAsync());
			Assert.Equal(3, reader.GetInt32(0));
			Assert.False(await reader.NextResultAsync());
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
#if BASELINE
				Assert.Throws<IndexOutOfRangeException>(() => reader.GetName(0));
#else
				Assert.Throws<InvalidOperationException>(() => reader.GetName(0));
#endif
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
#if BASELINE
				Assert.Throws<IndexOutOfRangeException>(() => reader.GetName(0));
#else
				Assert.Throws<InvalidOperationException>(() => reader.GetName(0));
#endif
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
			cmd.Parameters.Add(new() { ParameterName = "@parameter", Value = "one" });
			using var reader = cmd.ExecuteReader();
			Assert.True(await reader.ReadAsync());
			Assert.Equal(1L, reader.GetInt64(0));
			Assert.False(await reader.ReadAsync());
			Assert.False(await reader.NextResultAsync());
		}

		using (var cmd = m_database.Connection.CreateCommand())
		{
			cmd.CommandText = "select id, value FROM query_null_parameter where @parameter is null or value = @parameter order by id;";
			cmd.Parameters.Add(new() { ParameterName = "@parameter", Value = null });
			using var reader = cmd.ExecuteReader();
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

	[Fact]
	public async Task DoubleDispose()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = @"select 1;";
		using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
		Assert.True(await reader.ReadAsync().ConfigureAwait(false));
		reader.Dispose();
		reader.Dispose();
	}

	[Fact]
	public async Task MultipleStatementsWithInvalidSql()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = @"select 1; select 1 from mysql.abc; select 2;";

		using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
		Assert.True(await reader.ReadAsync().ConfigureAwait(false));
		Assert.Equal(1, reader.GetInt32(0));
		Assert.False(await reader.ReadAsync().ConfigureAwait(false));

		await Assert.ThrowsAsync<MySqlException>(() => reader.NextResultAsync());
		Assert.False(await reader.ReadAsync().ConfigureAwait(false));

		Assert.False(await reader.NextResultAsync().ConfigureAwait(false));
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
			using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
			Assert.Equal(new[] { "four", "one", "three", "two" }, reader.Cast<IDataRecord>().Select(x => x.GetString(0)));
		}
	}

	[SkippableFact(Baseline = "http://bugs.mysql.com/bug.php?id=82292")]
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

	[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=78760")]
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
	public void ExecuteScalarReturnsFirstValue()
	{
		var result = m_database.Connection.ExecuteScalar("select 1; select 2;");
		TestUtilities.AssertIsOne(result);
	}

	[Fact]
	public async Task ExecuteScalarAsyncReturnsFirstValue()
	{
		var result = await m_database.Connection.ExecuteScalarAsync("select 1; select 2;");
		TestUtilities.AssertIsOne(result);
	}

	[Fact]
	public void ExecuteScalarReturnsNull()
	{
		m_database.Connection.Execute(@"drop table if exists empty_table;
		create table empty_table(id bigint not null primary key);");
		var result = m_database.Connection.ExecuteScalar("select * from empty_table; select 2;");
		Assert.Null(result);
	}

	[Fact]
	public void ExecuteScalarReturnsDBNull()
	{
		using var command = m_database.Connection.CreateCommand();
		command.CommandText = "select null; select 2;";
		var result = command.ExecuteScalar();
		Assert.Equal(DBNull.Value, result);
	}

	[Fact]
	public void TrailingCommentIsNotAResultSet()
	{
		using var command = m_database.Connection.CreateCommand();
		command.CommandText = "select 0; -- trailing comment";

		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(0, reader.GetInt32(0));
		Assert.False(reader.NextResult());
		Assert.False(reader.HasRows);
		Assert.False(reader.Read());
	}

	[Fact]
	public void SumBytes()
	{
		m_database.Connection.Execute(@"drop table if exists sum_bytes;
		create table sum_bytes(value tinyint unsigned not null);
		insert into sum_bytes(value) values(0), (1), (2), (254), (255);");

		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select sum(value) from sum_bytes";
		Assert.Equal(512m, cmd.ExecuteScalar());

		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(512m, reader.GetValue(0));
		Assert.Equal(512, reader.GetInt32(0));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SumShorts(bool prepareCommand)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		connection.Execute(@"drop table if exists sum_shorts;
		create table sum_shorts(value smallint unsigned not null);
		insert into sum_shorts(value) values(0), (1), (2), (32766), (32767);");

		using var cmd = connection.CreateCommand();
		cmd.CommandText = "select sum(value) from sum_shorts";
		if (prepareCommand)
			cmd.Prepare();
		Assert.Equal(65536m, cmd.ExecuteScalar());

		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(65536m, reader.GetValue(0));
		Assert.Throws<OverflowException>(() => reader.GetInt16(0));
		Assert.Equal(65536, reader.GetInt32(0));
		Assert.Equal(65536L, reader.GetInt64(0));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void SumInts(bool prepareCommand)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		connection.Execute(@"drop table if exists sum_ints;
		create table sum_ints(value int unsigned not null);
		insert into sum_ints(value) values(0), (1), (2), (2147483646), (2147483647);");

		using var cmd = connection.CreateCommand();
		cmd.CommandText = "select sum(value) from sum_ints";
		if (prepareCommand)
			cmd.Prepare();
		Assert.Equal(4294967296m, cmd.ExecuteScalar());

		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(4294967296m, reader.GetValue(0));
		Assert.Throws<OverflowException>(() => reader.GetInt32(0));
		Assert.Equal(4294967296L, reader.GetInt64(0));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void DivideInts(bool prepareCommand)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		using var cmd = new MySqlCommand("select 2 / 1;", connection);
		if (prepareCommand)
			cmd.Prepare();
		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(2.000m, reader.GetValue(0));
		Assert.Equal(2.000m, reader.GetDecimal(0));
#if !BASELINE
		Assert.Equal((byte) 2, reader.GetByte(0));
		Assert.Equal((sbyte) 2, reader.GetSByte(0));
#endif
		Assert.Equal((short) 2, reader.GetInt16(0));
		Assert.Equal((ushort) 2, reader.GetUInt16(0));
		Assert.Equal(2, reader.GetInt32(0));
		Assert.Equal(2u, reader.GetUInt32(0));
		Assert.Equal(2L, reader.GetInt64(0));
		Assert.Equal(2ul, reader.GetUInt64(0));
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
	[InlineData("1000000000000", typeof(long))]
	[InlineData("cast(1000000000000 as unsigned)", typeof(ulong))]
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
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select " + value + ";";

		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(expectedType, reader.GetFieldType(0));
	}

	[Fact]
	public void GetFieldTypeInvalidIndex()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 1;";
		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Throws<IndexOutOfRangeException>(() => reader.GetFieldType(1));
	}

	[Fact]
	public void GetFieldTypeAfterNextResult()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 1;";
		using var reader = cmd.ExecuteReader();
		Assert.False(reader.NextResult());
#if BASELINE
		Assert.Throws<IndexOutOfRangeException>(() => reader.GetFieldType(0));
#else
		Assert.Throws<InvalidOperationException>(() => reader.GetFieldType(0));
#endif
	}

	[Theory]
#if BASELINE
	[InlineData("null", "VARCHAR")]
#else
	[InlineData("null", "NULL")]
#endif
	[InlineData("cast(null as char)", "VARCHAR")]
	[InlineData("1000000000000", "BIGINT")]
	[InlineData("cast(1000000000000 as unsigned)", "BIGINT")]
	[InlineData("1.0", "DECIMAL")]
	[InlineData("'text'", "VARCHAR")]
	[InlineData("cast('2000-01-02' as date)", "DATE")]
	[InlineData("cast('2000-01-02 13:45:56' as datetime)", "DATETIME")]
	[InlineData("cast('13:45:56' as time)", "TIME")]
	[InlineData("_binary'00112233'", "BLOB")]
	[InlineData("sqrt(2)", "DOUBLE")]
	public void GetDataTypeName(string value, string expectedDataType)
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select " + value + ";";

		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(expectedDataType, reader.GetDataTypeName(0));
	}

	[Fact]
	public void GetDataTypeNameInvalidIndex()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 1;";
		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Throws<IndexOutOfRangeException>(() => reader.GetDataTypeName(1));
	}

	[Fact]
	public void GetDataTypeNameAfterNextResult()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 1;";
		using var reader = cmd.ExecuteReader();
		Assert.False(reader.NextResult());
#if BASELINE
		Assert.Throws<IndexOutOfRangeException>(() => reader.GetDataTypeName(0));
#else
		Assert.Throws<InvalidOperationException>(() => reader.GetDataTypeName(0));
#endif
	}

#if !BASELINE
	[Fact]
	public void GetColumnSchemaAfterNextResult()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select 1;";
		using var reader = cmd.ExecuteReader();
		Assert.False(reader.NextResult());
		Assert.Empty(reader.GetColumnSchema());
	}
#endif

	private void UseReaderWithoutDisposingThread(object obj)
	{
		var data = (UseReaderWithoutDisposingThreadData) obj;

		try
		{
			for (int i = 0; i < 100; i++)
			{
				using var connection = new MySqlConnection(data.ConnectionStringBuilder.ConnectionString);
				connection.Open();

				using var cmd = connection.CreateCommand();
				cmd.CommandText = @"select * from dispose_reader;";

				var reader = cmd.ExecuteReader();
				reader.Read();
			}
		}
		catch (Exception ex)
		{
			lock (data)
				data.Exceptions.Add(ex);
		}
	}

	[Fact]
	public void InputOutputParameter()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "set @param = 1234";

		cmd.Parameters.Add(new()
		{
			ParameterName = "@param",
			Direction = ParameterDirection.InputOutput,
			Value = 123,
		});

		Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());

		// Issue #231: Assert.Equal(1234, cmd.Parameters["@param"].Value);
	}

	[Fact]
	public void OutputParameter()
	{
		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "set @param = 1234";

		cmd.Parameters.Add(new()
		{
			ParameterName = "@param",
			Direction = ParameterDirection.Output,
		});

		Assert.Throws<MySqlException>(() => cmd.ExecuteNonQuery());

		// Issue #231: Assert.Equal(1234, cmd.Parameters["@param"].Value);
	}

	[Fact]
	public void CharParameter()
	{
		m_database.Connection.Execute(@"drop table if exists char_test;
create table char_test(id integer not null primary key, char1 char(1) not null, char4 char(4) not null, varchar1 varchar(1) not null, varchar4 varchar(4) not null) collate utf8mb4_bin;
insert into char_test (id, char1, char4, varchar1, varchar4) VALUES (1, '\'', 'b', 'c', 'Σ'), (2, 'e', '\\', '""', 'h');
");

		using (var command = new MySqlCommand("select id from char_test where char1 = @ch;", m_database.Connection))
		{
			command.Parameters.AddWithValue("@ch", '\'');
			using var reader = command.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.False(reader.Read());
		}

		using (var command = new MySqlCommand("select id from char_test where char4 = @ch;", m_database.Connection))
		{
			command.Parameters.AddWithValue("@ch", '\\');
			using var reader = command.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(2, reader.GetInt32(0));
			Assert.False(reader.Read());
		}

		using (var command = new MySqlCommand("select id from char_test where varchar1 = @ch;", m_database.Connection))
		{
			command.Parameters.AddWithValue("@ch", '"');
			using var reader = command.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(2, reader.GetInt32(0));
			Assert.False(reader.Read());
		}

#if !BASELINE
		// can't repro test failure locally, but it fails on Appveyor
		using (var command = new MySqlCommand("select id from char_test where varchar4 = @ch;", m_database.Connection))
		{
			command.Parameters.AddWithValue("@ch", 'Σ');
			using var reader = command.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.False(reader.Read());
		}
#endif
	}

	[Fact]
	public void EnumParameter()
	{
		m_database.Connection.Execute(@"drop table if exists enum_test;
create table enum_test(id integer not null primary key, value text not null);
insert into enum_test (id, value) VALUES (1002, 'no'), (1003, 'yes');
");

		using var command = new MySqlCommand("select * from enum_test where id = @ID;", m_database.Connection);
		command.Parameters.AddWithValue("@ID", MySqlErrorCode.No);

		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal((int) MySqlErrorCode.No, reader.GetInt32(0));
		Assert.Equal("no", reader.GetString(1));
		Assert.False(reader.Read());
	}

	[Fact]
	public void Int64EnumParameter()
	{
		m_database.Connection.Execute(@"drop table if exists long_enum_test;
create table long_enum_test(id bigint not null primary key, value integer not null);
insert into long_enum_test (id, value) VALUES (0x7FFFFFFFFFFFFFFF, 1);
");

		using var command = new MySqlCommand("select * from long_enum_test where id = @ID;", m_database.Connection);
		command.Parameters.AddWithValue("@ID", TestLongEnum.Value);

		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(long.MaxValue, reader.GetInt64(0));
		Assert.Equal(1, reader.GetInt32(1));
		Assert.False(reader.Read());
	}

	[Fact]
	public void ReturnDerivedTypes()
	{
		using MySqlTransaction transaction = m_database.Connection.BeginTransaction();
		using MySqlCommand command = m_database.Connection.CreateCommand();
		command.Transaction = transaction;
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
			Assert.Equal(3L, Convert.ToInt64(reader.GetValue(0)));
			Assert.False(reader.Read());
		}

		transaction.Rollback();
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

		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = sql;

		using var reader = cmd.ExecuteReader();
		for (int i = 0; i < expecteds.Length; i++)
		{
			Assert.Equal(expecteds[i], reader.HasRows);
			Assert.Equal(expecteds[i], reader.Read());
			Assert.False(reader.Read());
			Assert.Equal(expecteds[i], reader.HasRows);

			Assert.Equal(i != expecteds.Length - 1, reader.NextResult());
		}
	}

	[Fact]
	public void HasRowsRepeated()
	{
		m_database.Connection.Execute(@"drop table if exists has_rows;
create table has_rows(value int not null);
insert into has_rows(value) values(1),(2),(3);");

		using var cmd = m_database.Connection.CreateCommand();
		cmd.CommandText = "select * from has_rows where value = 1;";

		using var reader = cmd.ExecuteReader();
		Assert.True(reader.HasRows);
		Assert.True(reader.HasRows);
		Assert.True(reader.HasRows);
		Assert.True(reader.HasRows);

		Assert.True(reader.Read());

		Assert.True(reader.HasRows);
		Assert.True(reader.HasRows);
		Assert.True(reader.HasRows);

		Assert.False(reader.Read());

		Assert.True(reader.HasRows);
		Assert.True(reader.HasRows);
		Assert.True(reader.HasRows);
	}

	[Fact]
	public void CommandBehaviorCloseConnection()
	{
		using (var connection = new MySqlConnection(AppConfig.ConnectionString))
		{
			Assert.Equal(ConnectionState.Closed, connection.State);
			connection.Open();
			Assert.Equal(ConnectionState.Open, connection.State);

			using (var cmd = new MySqlCommand("SELECT 1;", connection))
			using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
			{
				Assert.True(reader.Read());
				Assert.Equal(1, reader.GetInt32(0));
				Assert.False(reader.Read());
			}

			Assert.Equal(ConnectionState.Closed, connection.State);
			connection.Open();
			Assert.Equal(ConnectionState.Open, connection.State);
			connection.Close();
		}
	}

	[Fact]
	public void CommandBehaviorSingleRow()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists command_behavior_single_row;
create table command_behavior_single_row(id integer not null primary key);
insert into command_behavior_single_row(id) values(1),(2),(3),(4),(5),(6),(7),(8),(9),(10);");

		using var cmd = new MySqlCommand("SELECT id FROM command_behavior_single_row ORDER BY id;", connection);
		using var reader = cmd.ExecuteReader(CommandBehavior.SingleRow);
		Assert.True(reader.Read());
		Assert.Equal(1, reader.GetInt32(0));
		Assert.False(reader.Read());
	}

	[Fact]
	public async Task CommandBehaviorSingleRowMultipleResultSets()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists command_behavior_single_row;
create table command_behavior_single_row(id integer not null primary key);");

		using (var cmd = new MySqlCommand("SELECT 1; insert into command_behavior_single_row(id) values(1); SELECT 2;", connection))
		{
			using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
			Assert.True(await reader.ReadAsync());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.False(await reader.ReadAsync());
#if !BASELINE
			Assert.False(await reader.NextResultAsync());
#endif
		}

		// subsequent commands were still executed (and had effects) even though they didn't return results
		using (var cmd = new MySqlCommand("select count(*) from command_behavior_single_row;", connection))
		{
			Assert.Equal(1L, await cmd.ExecuteScalarAsync());
		}
	}

	[Fact]
	public async Task CommandBehaviorSingleResultMultipleResultSets()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists command_behavior_single_result;
create table command_behavior_single_result(id integer not null primary key);");

		using (var cmd = new MySqlCommand("SELECT 1; insert into command_behavior_single_result(id) values(1); SELECT 2;", connection))
		{
			using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SingleResult);
			Assert.True(await reader.ReadAsync());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.False(await reader.ReadAsync());
			Assert.False(await reader.NextResultAsync());
		}

		// subsequent commands were still executed (and had effects) even though they didn't return results
		using (var cmd = new MySqlCommand("select count(*) from command_behavior_single_result;", connection))
		{
			Assert.Equal(1L, await cmd.ExecuteScalarAsync());
		}
	}

#if !BASELINE
	[Fact]
	public void NoBackslashEscapes()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.NoBackslashEscapes = true;
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();
		connection.Execute("SET @@sql_mode = CONCAT(@@sql_mode, ',NO_BACKSLASH_ESCAPES');");
		var value = "\\'\"";
		using var cmd = new MySqlCommand("SELECT @param;", connection)
		{
			Parameters =
			{
				new("@param", value),
			},
		};
		var result = cmd.ExecuteScalar();
		Assert.Equal(value, result);
	}
#endif

	[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=97067")]
	public async Task QueryBit()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		await connection.OpenAsync();
		await connection.ExecuteAsync(@"
DROP TABLE IF EXISTS query_bit;
CREATE TABLE query_bit(name TEXT, value BIT(1));
INSERT INTO query_bit VALUES('a', 1);
");

		using var command = new MySqlCommand(@"
SELECT value FROM query_bit;

SELECT MAX(value) FROM query_bit;

SELECT COALESCE(value, 0) FROM query_bit;

SELECT CASE
    WHEN name IS NOT NULL THEN value
    ELSE NULL
END AS value
FROM query_bit;", connection);
		using var reader = await command.ExecuteReaderAsync();

		Assert.True(await reader.ReadAsync());
		Assert.Equal(1ul, reader.GetValue(0));
		Assert.False(await reader.ReadAsync());

		Assert.True(await reader.NextResultAsync());

		Assert.True(await reader.ReadAsync());
		Assert.Equal(1ul, reader.GetValue(0));
		Assert.False(await reader.ReadAsync());

		Assert.True(await reader.NextResultAsync());

		Assert.True(await reader.ReadAsync());
		var value = reader.GetValue(0);
		if (value is byte[] byteArray)
			Assert.Equal(new byte[] { 49 }, byteArray); // server bug returns '1' as a blob
		else
			Assert.Equal(1m, value);
		Assert.False(await reader.ReadAsync());

		Assert.True(await reader.NextResultAsync());

		Assert.True(await reader.ReadAsync());
		// MySQL returns ulong, MariaDB returns decimal; GetBoolean will coerce both
		Assert.True(reader.GetBoolean(0));
		Assert.False(await reader.ReadAsync());

		Assert.False(await reader.NextResultAsync());
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public void GetIntForTinyInt1(bool treatTinyAsBoolean, bool prepare)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.TreatTinyAsBoolean = treatTinyAsBoolean;
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists datatypes_tinyint1;
create table datatypes_tinyint1(value tinyint(1));
insert into datatypes_tinyint1(value) values(0), (1), (2), (-1), (-128), (127);");

		using var command = new MySqlCommand("select value from datatypes_tinyint1;", connection);
		if (prepare)
			command.Prepare();
		using var reader = command.ExecuteReader();

		int[] expected = { 0, 1, 2, -1, -128, 127 };
		if (treatTinyAsBoolean)
			expected = expected.Select(x => x == 0 ? 0 : 1).ToArray();

		for (int i = 0; i < expected.Length; i++)
		{
			Assert.True(reader.Read());
#if !BASELINE
			// https://bugs.mysql.com/bug.php?id=99091
			Assert.Equal((sbyte) expected[i], reader.GetSByte(0));
			if (treatTinyAsBoolean)
				Assert.Equal((byte) expected[i], reader.GetByte(0));
#endif
			Assert.Equal((short) expected[i], reader.GetInt16(0));
			Assert.Equal(expected[i], reader.GetInt32(0));
			Assert.Equal((long) expected[i], reader.GetInt64(0));
#if !BASELINE
			// https://bugs.mysql.com/bug.php?id=99091
			Assert.Equal(expected[i], reader.GetFieldValue<int>(0));
#endif
		}

		Assert.False(reader.Read());
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public async Task Delimiter(bool prepareCommand, bool isAsync)
	{
		using var command = m_database.Connection.CreateCommand();
		command.CommandText = @"DELIMITER $$
CREATE FUNCTION echo(
  name VARCHAR(63)
) RETURNS VARCHAR(63)
BEGIN
    RETURN name;
END
$$";
		MySqlException exception;
		if (prepareCommand)
		{
			exception = isAsync ? (await Assert.ThrowsAsync<MySqlException>(async () => await command.PrepareAsync())) :
				Assert.Throws<MySqlException>(() => command.Prepare());
		}
		else
		{
			exception = isAsync ? (await Assert.ThrowsAsync<MySqlException>(async () => await command.ExecuteNonQueryAsync())) :
				Assert.Throws<MySqlException>(() => command.ExecuteNonQuery());
		}

#if !BASELINE
		Assert.Equal(MySqlErrorCode.DelimiterNotSupported, exception.ErrorCode);
#else
		Assert.Equal((int) MySqlErrorCode.ParseError, exception.Number);
#endif
	}

	[Fact]
	public void QueryDateTimeLiteral()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		string sql = "SELECT ?p0 as datevalue";
		using var command = new MySqlCommand(sql, connection);
		command.Parameters.AddWithValue("?p0", DateTime.UtcNow);

		using var reader = command.ExecuteReader();
		using var schema = reader.GetSchemaTable();
		var providerType = (MySqlDbType) (int) schema.Rows[0]["ProviderType"];
		Assert.Equal(MySqlDbType.DateTime, providerType);
	}

	[SkippableTheory(Baseline = "https://bugs.mysql.com/bug.php?id=105209")]
	[InlineData(false)]
	[InlineData(true)]
	public void QueryTimeSpan(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		connection.Execute(@"drop table if exists test_time;
create table test_time(id int auto_increment not null primary key, tm time not null);
insert into test_time(tm) values('00:00:00'),('01:01:01'),('00:00:00');");

		string sql = "select tm from test_time order by id";
		using var command = new MySqlCommand(sql, connection);
		if (prepare)
			command.Prepare();

		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(TimeSpan.Zero, reader.GetTimeSpan(0));
		Assert.True(reader.Read());
		Assert.Equal(new(1, 1, 1), reader.GetTimeSpan(0));
		Assert.True(reader.Read());
		Assert.Equal(TimeSpan.Zero, reader.GetTimeSpan(0));
		Assert.False(reader.Read());
	}

	[SkippableTheory(ServerFeatures.QueryAttributes)]
	[InlineData(false)]
	[InlineData(true)]
	public void QueryAttributes(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		using var cmd = new MySqlCommand("select mysql_query_attribute_string('name') as attribute;", connection);
		cmd.Attributes.SetAttribute("name", "attr_value");

		if (prepare)
			cmd.Prepare();

		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			Assert.Equal(cmd.Attributes[0].Value, reader.GetValue(reader.GetOrdinal("attribute")));
		}
	}

	[SkippableTheory(ServerFeatures.QueryAttributes)]
	[InlineData(false)]
#if !BASELINE
	[InlineData(true)]
#endif
	public void QueryAttributesMultipleStatements(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		using var cmd = new MySqlCommand(@"select mysql_query_attribute_string('attr1') as attribute, @param1 as parameter;
select mysql_query_attribute_string('attr2') as attribute, @param2 as parameter;", connection);
		cmd.Attributes.SetAttribute("attr1", "attr1_value");
		cmd.Attributes.SetAttribute("attr2", "attr2_value");
		cmd.Parameters.AddWithValue("@param1", "param1_value");
		cmd.Parameters.AddWithValue("@param2", "param2_value");

		if (prepare)
			cmd.Prepare();

		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		Assert.Equal(cmd.Attributes[0].Value, reader.GetValue(reader.GetOrdinal("attribute")));
		Assert.Equal(cmd.Parameters[0].Value, reader.GetValue(reader.GetOrdinal("parameter")));
		Assert.False(reader.Read());
		Assert.True(reader.NextResult());
		Assert.True(reader.Read());
		Assert.Equal(cmd.Attributes[1].Value, reader.GetValue(reader.GetOrdinal("attribute")));
		Assert.Equal(cmd.Parameters[1].Value, reader.GetValue(reader.GetOrdinal("parameter")));
		Assert.False(reader.Read());
	}

	[SkippableFact(ServerFeatures.QueryAttributes)]
	public void QueryAttributeWithEmptyName()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		using var cmd = new MySqlCommand("select mysql_query_attribute_string('name') as attribute;", connection);
		Assert.Throws<ArgumentException>(() => cmd.Attributes.SetAttribute("", "attr_value"));
	}

	[SkippableTheory(ServerFeatures.QueryAttributes, Baseline = "https://bugs.mysql.com/bug.php?id=105728")]
	[InlineData(false)]
	[InlineData(true)]
	public void QueryAttributeAndParameter(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		using var cmd = new MySqlCommand("select mysql_query_attribute_string('name') as attribute, @name as parameter;", connection);
		cmd.Attributes.SetAttribute("name", "attr_value");
		cmd.Parameters.AddWithValue("name", "param_value");

		if (prepare)
			cmd.Prepare();

		using var reader = cmd.ExecuteReader();
		while (reader.Read())
		{
			Assert.Equal(cmd.Attributes[0].Value, reader.GetValue(reader.GetOrdinal("attribute")));
			Assert.Equal(cmd.Parameters[0].Value, reader.GetValue(reader.GetOrdinal("parameter")));
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
