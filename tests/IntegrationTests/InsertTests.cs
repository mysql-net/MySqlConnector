using System.Globalization;
using System.Numerics;
#if MYSQL_DATA
using MySql.Data.Types;
#endif

namespace IntegrationTests;

public class InsertTests : IClassFixture<DatabaseFixture>
{
	public InsertTests(DatabaseFixture database)
	{
		m_database = database;
	}

	[Fact]
	public async Task LastInsertedId()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_ai;
create table insert_ai(rowid integer not null primary key auto_increment, text varchar(100) not null);");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand("INSERT INTO insert_ai (text) VALUES (@text);", m_database.Connection);
			command.Parameters.Add(new() { ParameterName = "@text", Value = "test" });
			Assert.Equal(1, await command.ExecuteNonQueryAsync());
			Assert.Equal(1L, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task LastInsertedIdNegative()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_ai;
create table insert_ai(rowid integer not null primary key auto_increment);");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand("INSERT INTO insert_ai(rowid) VALUES (@rowid);", m_database.Connection);
			command.Parameters.AddWithValue("@rowid", -1);
			Assert.Equal(1, await command.ExecuteNonQueryAsync());
			Assert.Equal(-1L, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task LastInsertedIdUlong()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_ai;
create table insert_ai(rowid bigint unsigned not null primary key auto_increment);");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand("INSERT INTO insert_ai(rowid) VALUES (@rowid);", m_database.Connection);
			command.Parameters.AddWithValue("@rowid", ((ulong) long.MaxValue) + 1);
			Assert.Equal(1, await command.ExecuteNonQueryAsync());
			Assert.Equal(long.MinValue, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task LastInsertedIdAfterSelect()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_ai;
create table insert_ai(rowid integer not null primary key auto_increment, text varchar(100) not null);
insert into insert_ai(text) values('test');
");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand(@"SELECT * FROM insert_ai;
INSERT INTO insert_ai (text) VALUES (@text);", m_database.Connection);
			command.Parameters.Add(new() { ParameterName = "@text", Value = "test" });
			Assert.Equal(1, await command.ExecuteNonQueryAsync());
			Assert.Equal(2L, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task LastInsertedIdBeforeSelect()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_ai;
create table insert_ai(rowid integer not null primary key auto_increment, text varchar(100) not null);
insert into insert_ai(text) values('test');
");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand(@"INSERT INTO insert_ai (text) VALUES (@text);
SELECT * FROM insert_ai;", m_database.Connection);
			command.Parameters.Add(new() { ParameterName = "@text", Value = "test" });
			Assert.Equal(1, await command.ExecuteNonQueryAsync());
			Assert.Equal(2L, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task LastInsertedIdTwoInserts()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_ai;
create table insert_ai(rowid integer not null primary key auto_increment, text varchar(100) not null);
");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand(@"INSERT INTO insert_ai (text) VALUES ('test1');
INSERT INTO insert_ai (text) VALUES ('test2');", m_database.Connection);
			Assert.Equal(2, await command.ExecuteNonQueryAsync());
			Assert.Equal(2L, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task LastInsertedIdLockTables()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_ai;
create table insert_ai(rowid integer not null primary key auto_increment, text varchar(100) not null);
");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand(@"LOCK TABLES insert_ai WRITE;
INSERT INTO insert_ai (text) VALUES ('test');
UNLOCK TABLES;", m_database.Connection);
			Assert.Equal(1, await command.ExecuteNonQueryAsync());
			Assert.Equal(1L, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task LastInsertedIdInsertForeignKey()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists TestTableWithForeignKey;
drop table if exists TestTable;

Create Table TestTable(
    id BIGINT NOT NULL AUTO_INCREMENT,
    column1 CHAR(100),
    Primary Key(id)
);

Create Table TestTableWithForeignKey(
    foreign_id BIGINT NOT NULL,
    column2 CHAR(100),
    Foreign Key(foreign_id) REFERENCES TestTable(id)
);");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand(@"INSERT INTO TestTable(column1) VALUES('hello');
INSERT INTO TestTableWithForeignKey(foreign_id, column2) VALUES(LAST_INSERT_ID(), 'test');", m_database.Connection);
			Assert.Equal(2, await command.ExecuteNonQueryAsync());
			Assert.Equal(1L, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task LastInsertedIdInsertIgnore()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_ai;
create table insert_ai(rowid integer not null primary key auto_increment, text varchar(100) not null);
");
		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand(@"INSERT IGNORE INTO insert_ai (rowid, text) VALUES (2, 'test');", m_database.Connection);
			Assert.Equal(1, await command.ExecuteNonQueryAsync());
			Assert.Equal(2L, command.LastInsertedId);
			Assert.Equal(0, await command.ExecuteNonQueryAsync());
			Assert.Equal(0L, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public async Task RowsAffected()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_rows_affected;
create table insert_rows_affected(id integer not null primary key auto_increment, value text null);");

		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand(@"
INSERT INTO insert_rows_affected (value) VALUES (null);
INSERT INTO insert_rows_affected (value) VALUES (null);", m_database.Connection);
			var rowsAffected = await command.ExecuteNonQueryAsync();
			Assert.Equal(2, rowsAffected);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public void LastInsertedIdExplicitStart()
	{
		m_database.Connection.Execute(@"drop table if exists insert_ai_2;
create table insert_ai_2(rowid integer not null primary key auto_increment, text varchar(100) not null) auto_increment = 1234;");
		try
		{
			m_database.Connection.Open();
			using var command = new MySqlCommand("insert into insert_ai_2(text) values('test');", m_database.Connection);
			command.ExecuteNonQuery();
			Assert.Equal(1234L, command.LastInsertedId);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[Fact]
	public void InsertWithDapper()
	{
		m_database.Connection.Execute(@"drop table if exists insert_dapper;
create table insert_dapper(rowid integer not null primary key auto_increment, text varchar(100) not null);");

		var query = @"insert into insert_dapper(text) values(@text);
select last_insert_id();";
		var rowids = m_database.Connection.Query<long>(query, new { text = "Test" });
		foreach (var rowid in rowids)
			Assert.Equal(1L, rowid);
	}

	[Theory]
	[InlineData(3)]
	[InlineData(6)]
	public void InsertTime(int precision)
	{
		m_database.Connection.Execute($@"drop table if exists insert_time;
create table insert_time(value TIME({precision}));");

		try
		{
			m_database.Connection.Open();
			using (var command = new MySqlCommand("INSERT INTO insert_time (value) VALUES (@Value);", m_database.Connection))
			{
				command.Parameters.Add(new() { ParameterName = "@value", Value = TimeSpan.FromMilliseconds(10) });
				command.ExecuteNonQuery();
			}

			using (var command = new MySqlCommand("SELECT value FROM insert_time;", m_database.Connection))
			using (var reader = command.ExecuteReader())
			{
				Assert.True(reader.Read());
				Assert.Equal(TimeSpan.FromMilliseconds(10), reader.GetValue(0));
				Assert.False(reader.Read());
			}
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	[SkippableFact(MySqlData = "https://bugs.mysql.com/bug.php?id=73788")]
	public void InsertDateTimeOffset()
	{
		m_database.Connection.Execute(@"drop table if exists insert_datetimeoffset;
create table insert_datetimeoffset(rowid integer not null primary key auto_increment, datetimeoffset1 datetime null);");
		var value = new DateTimeOffsetValues { DateTimeOffset1 = new DateTimeOffset(2017, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(678)) };

		m_database.Connection.Open();
		try
		{
			using var cmd = m_database.Connection.CreateCommand();
			cmd.CommandText = @"insert into insert_datetimeoffset(datetimeoffset1) values(@datetimeoffset1);";
			cmd.Parameters.Add(new()
			{
				ParameterName = "@datetimeoffset1",
				DbType = DbType.DateTimeOffset,
				Value = value.DateTimeOffset1,
			});
			Assert.Equal(1, cmd.ExecuteNonQuery());
		}
		finally
		{
			m_database.Connection.Close();
		}

		var datetime = m_database.Connection.ExecuteScalar<DateTime>(@"select datetimeoffset1 from insert_datetimeoffset order by rowid;");

		DateTime.SpecifyKind(datetime, DateTimeKind.Utc);

		Assert.Equal(value.DateTimeOffset1.Value.UtcDateTime, datetime);
	}

	[SkippableFact(MySqlData = "https://bugs.mysql.com/bug.php?id=91199")]
	public void InsertMySqlDateTime()
	{
		m_database.Connection.Execute(@"drop table if exists insert_mysqldatetime;
create table insert_mysqldatetime(rowid integer not null primary key auto_increment, ts timestamp(6) null);");
		var value = new DateTimeOffsetValues { DateTimeOffset1 = new DateTimeOffset(2017, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(678)) };

		m_database.Connection.Open();
		try
		{
			using var cmd = m_database.Connection.CreateCommand();
			cmd.CommandText = @"insert into insert_mysqldatetime(ts) values(@ts);";
			cmd.Parameters.AddWithValue("@ts", new MySqlDateTime(2018, 6, 9, 12, 34, 56, 123456));
			Assert.Equal(1, cmd.ExecuteNonQuery());
		}
		finally
		{
			m_database.Connection.Close();
		}

		var datetime = m_database.Connection.ExecuteScalar<DateTime>(@"select ts from insert_mysqldatetime order by rowid;");
		Assert.Equal(new DateTime(2018, 6, 9, 12, 34, 56, 123).AddTicks(4560), datetime);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertMemoryStream(bool prepare)
	{
		m_database.Connection.Execute(@"drop table if exists insert_stream;
create table insert_stream(rowid integer not null primary key auto_increment, str text, blb blob);");

		m_database.Connection.Open();
		try
		{
			using var cmd = m_database.Connection.CreateCommand();
			cmd.CommandText = @"insert into insert_stream(str, blb) values(@str, @blb);";
			cmd.Parameters.AddWithValue("@str", new MemoryStream(new byte[] { 97, 98, 99, 100 }));
			cmd.Parameters.AddWithValue("@blb", new MemoryStream(new byte[] { 97, 98, 99, 100 }, 0, 4, false, true));
			if (prepare)
				cmd.Prepare();
			Assert.Equal(1, cmd.ExecuteNonQuery());
		}
		finally
		{
			m_database.Connection.Close();
		}

		using var reader = m_database.Connection.ExecuteReader(@"select str, blb from insert_stream order by rowid;");
		Assert.True(reader.Read());
		Assert.Equal("abcd", reader.GetValue(0));
		Assert.Equal(new byte[] { 97, 98, 99, 100 }, reader.GetValue(1));
	}

	[SkippableTheory(MySqlData = "https://bugs.mysql.com/bug.php?id=103819")]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertStringBuilder(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_string_builder;
create table insert_string_builder(rowid integer not null primary key auto_increment, str text collate utf8mb4_bin);");

		var value = new StringBuilder("\aAB\\12'ab\\'\\'");
		for (var i = 0; i < 100; i++)
			value.Append("\U0001F600\uD800\'\U0001F601\uD800");

		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_string_builder(str) values(@str);";
		cmd.Parameters.AddWithValue("@str", value);
		if (prepare)
			cmd.Prepare();
		Assert.Equal(1, cmd.ExecuteNonQuery());

		using var reader = connection.ExecuteReader(@"select str from insert_string_builder order by rowid;");
		Assert.True(reader.Read());

		// all unpaired high-surrogates will be converted to the Unicode Replacement Character when converted to UTF-8 to be transmitted to the server
		var expected = value.ToString().Replace('\uD800', '\uFFFD');
		Assert.Equal(expected, reader.GetValue(0));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertBigInteger(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_big_integer;
create table insert_big_integer(rowid integer not null primary key auto_increment, value bigint);");

		var value = 1_000_000_000_000_000L;
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_big_integer(value) values(@value);";
		cmd.Parameters.AddWithValue("@value", new BigInteger(value));
		if (prepare)
			cmd.Prepare();
		Assert.Equal(1, cmd.ExecuteNonQuery());

		using var reader = connection.ExecuteReader(@"select value from insert_big_integer order by rowid;");
		Assert.True(reader.Read());
		Assert.Equal(value, reader.GetValue(0));
	}

#if !MYSQL_DATA
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertMySqlDecimal(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_mysql_decimal;
			create table insert_mysql_decimal(rowid integer not null primary key auto_increment, value decimal(65,0));");

		string value = "22";
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_mysql_decimal(value) values(@value);";
		cmd.Parameters.AddWithValue("@value", new MySqlDecimal(value));
		if (prepare)
			cmd.Prepare();
		cmd.ExecuteNonQuery();

		using var reader = connection.ExecuteReader(@"select value from insert_mysql_decimal order by rowid;");
		Assert.True(reader.Read());
		Assert.Equal(value, reader.GetValue(0).ToString());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertMySqlDecimalAsDecimal(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_mysql_decimal;
			create table insert_mysql_decimal(rowid integer not null primary key auto_increment, value decimal(65, 30));");

		string value = "-123456789012345678901234.01234";
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_mysql_decimal(value) values(@value);";
		cmd.Parameters.AddWithValue("@value", new MySqlDecimal(value));
		if (prepare)
			cmd.Prepare();
		Assert.Equal(1, cmd.ExecuteNonQuery());

		using var reader = connection.ExecuteReader(@"select value from insert_mysql_decimal order by rowid;");
		Assert.True(reader.Read());
		var val = ((decimal) reader.GetValue(0)).ToString(CultureInfo.InvariantCulture);
		Assert.Equal(value, val);
	}

	[Theory]
	[InlineData(1_000_000, 1024, true)]
	[InlineData(1_000_000, 1024, false)]
	[InlineData(1_000_000, int.MaxValue, true)]
	[InlineData(1_000_000, int.MaxValue, false)]
	[InlineData(0xff_fff8, 299593, true)]
	[InlineData(0xff_fff8, 299593, false)]
	[InlineData(0xff_fff8, 300000, true)]
	[InlineData(0xff_fff8, 300000, false)]
	[InlineData(0xff_fff8, int.MaxValue, true)]
	[InlineData(0xff_fff8, int.MaxValue, false)]
	[InlineData(0xff_fff9, int.MaxValue, true)]
	[InlineData(0xff_fff9, int.MaxValue, false)]
	[InlineData(0x1ff_fff0, 299593, true)]
	[InlineData(0x1ff_fff0, 299593, false)]
	[InlineData(0x1ff_fff0, 300000, true)]
	[InlineData(0x1ff_fff0, 300000, false)]
	[InlineData(15_999_999, int.MaxValue, true)]
	[InlineData(15_999_999, int.MaxValue, false)]
	[InlineData(16_000_000, int.MaxValue, true)]
	[InlineData(16_000_000, int.MaxValue, false)]
	[InlineData(16_000_001, int.MaxValue, true)]
	[InlineData(16_000_001, int.MaxValue, false)]
	[InlineData(31_999_999, 999_999, true)]
	[InlineData(31_999_999, 1_000_000, false)]
	[InlineData(32_000_000, 1_000_001, true)]
	[InlineData(32_000_000, 1_000_002, false)]
	[InlineData(32_000_001, 1_000_003, true)]
	[InlineData(32_000_001, 1_000_004, false)]
	public async Task SendLongData(int dataLength, int chunkLength, bool isAsync)
	{
		using MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute("""
			drop table if exists insert_mysql_long_data;
			create table insert_mysql_long_data(rowid integer not null primary key auto_increment, value longblob);
			""");

		var random = new Random(dataLength);
		var data = new byte[dataLength];
		random.NextBytes(data);

		using var chunkStream = new ChunkStream(data, chunkLength);

		using var writeCommand = new MySqlCommand("insert into insert_mysql_long_data(value) values(@value);", connection);
		writeCommand.Parameters.AddWithValue("@value", chunkStream);
		writeCommand.Prepare();
		if (isAsync)
			await writeCommand.ExecuteNonQueryAsync().ConfigureAwait(true);
		else
			writeCommand.ExecuteNonQuery();

		using var readCommand = new MySqlCommand("select length(value) from insert_mysql_long_data order by rowid;", connection);
		using (var reader = readCommand.ExecuteReader())
		{
			Assert.True(reader.Read());
			Assert.Equal(chunkStream.Length, reader.GetInt32(0));
		}

		readCommand.CommandText = "select value from insert_mysql_long_data order by rowid;";
		using (var reader = readCommand.ExecuteReader())
		{
			Assert.True(reader.Read());
			var readData = (byte[]) reader.GetValue(0);
			Assert.True(data.AsSpan().SequenceEqual(readData)); // much faster than Assert.Equal
		}
	}
#endif

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ReadMySqlDecimalUsingReader(bool prepare)
	{
		using MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_mysql_decimal;
			create table insert_mysql_decimal(rowid integer not null primary key auto_increment, value decimal(65, 30));");

		string value = "-12345678901234567890123456789012345.012345678901234567890123456789";
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_mysql_decimal(value) values(@value);";
		cmd.Parameters.AddWithValue("@value", value);
		Assert.Equal(1, cmd.ExecuteNonQuery());

		cmd.CommandText = @"select value from insert_mysql_decimal order by rowid;";
		if (prepare)
			cmd.Prepare();
		using var reader = cmd.ExecuteReader();
		Assert.True(reader.Read());
		var val = reader.GetMySqlDecimal("value");
		Assert.Equal(value, val.ToString());

#if !MYSQL_DATA
		val = reader.GetFieldValue<MySqlDecimal>(0);
		Assert.Equal(value, val.ToString());
#endif

		// value is too large to read as a regular decimal
#if MYSQL_DATA
		Assert.Throws<OverflowException>(() => reader.GetValue(0));
#else
		Assert.Throws<FormatException>(() => reader.GetValue(0));
#endif
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ReadMySqlDecimalZeroFill(bool prepare)
	{
		using MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute("""
			drop table if exists mysql_decimal_zerofill;
			create table mysql_decimal_zerofill(rowid integer not null primary key auto_increment, value decimal(20, 10) zerofill);
			insert into mysql_decimal_zerofill(value) values(0),(1),(0.1);
			""");

		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"select value from mysql_decimal_zerofill order by rowid;";
		if (prepare)
			cmd.Prepare();
		using var reader = cmd.ExecuteReader();

		Assert.True(reader.Read());
		Assert.Equal("0000000000.0000000000", reader.GetMySqlDecimal("value").ToString());
		Assert.Equal(0m, reader.GetDecimal(0));

		Assert.True(reader.Read());
		Assert.Equal("0000000001.0000000000", reader.GetMySqlDecimal("value").ToString());
		Assert.Equal(1m, reader.GetDecimal(0));

		Assert.True(reader.Read());
		Assert.Equal("0000000000.1000000000", reader.GetMySqlDecimal("value").ToString());
		Assert.Equal(0.1m, reader.GetDecimal(0));

		Assert.False(reader.Read());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void InsertBigIntegerAsDecimal(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_big_integer;
create table insert_big_integer(rowid integer not null primary key auto_increment, value decimal(40, 2));");

		var value = long.MaxValue * 1000m;
		using var cmd = connection.CreateCommand();
		cmd.CommandText = @"insert into insert_big_integer(value) values(@value);";
		cmd.Parameters.AddWithValue("@value", new BigInteger(value));
		if (prepare)
			cmd.Prepare();
		cmd.ExecuteNonQuery();

		using var reader = connection.ExecuteReader(@"select value from insert_big_integer order by rowid;");
		Assert.True(reader.Read());
		Assert.Equal(value, reader.GetValue(0));
	}

	[Fact]
	public void InsertOldGuid()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.OldGuids = true;
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists old_guids;
create table old_guids(id integer not null primary key auto_increment, guid binary(16) null);");

		var guid = new Guid(1, 2, 3, 0x27, 0x5C, 0x7B, 0x7D, 0x22, 0x25, 0x26, 0x2C);

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = @"insert into old_guids(guid) values(@guid)";
			var parameter = cmd.CreateParameter();
			parameter.ParameterName = "@guid";
			parameter.Value = guid;
			cmd.Parameters.Add(parameter);
			cmd.ExecuteNonQuery();
		}

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = @"select guid from old_guids;";
			var selected = (Guid) cmd.ExecuteScalar();
			Assert.Equal(guid, selected);
		}
	}

	[Fact]
	public void InsertEnumValue()
	{
		m_database.Connection.Execute(@"drop table if exists insert_enum_value;
create table insert_enum_value(rowid integer not null primary key auto_increment, Enum16 int null, Enum32 int null, Enum64 bigint null);");
		m_database.Connection.Execute(@"insert into insert_enum_value(Enum16, Enum32, Enum64) values(@e16a, @e32a, @e64a), (@e16b, @e32b, @e64b);",
			new { e16a = default(Enum16?), e32a = default(Enum32?), e64a = default(Enum64?), e16b = Enum16.On, e32b = Enum32.Off, e64b = Enum64.On });
		var results = m_database.Connection.Query<EnumValues>(@"select Enum16, Enum32, Enum64 from insert_enum_value order by rowid;").ToList();
		Assert.Equal(2, results.Count);
		Assert.Null(results[0].Enum16);
		Assert.Null(results[0].Enum32);
		Assert.Null(results[0].Enum64);
		Assert.Equal(Enum16.On, results[1].Enum16);
		Assert.Equal(Enum32.Off, results[1].Enum32);
		Assert.Equal(Enum64.On, results[1].Enum64);
	}

	[Fact]
	public async Task EnumParametersAreParsedCorrectly()
	{
		await m_database.Connection.ExecuteAsync(@"drop table if exists insert_enum_value2;
create table insert_enum_value2(rowid integer not null primary key auto_increment, `Varchar` varchar(10), `String` varchar(10), `Int` int null);");

		try
		{
			await m_database.Connection.OpenAsync();
			using var command = new MySqlCommand("INSERT INTO insert_enum_value2 (`Varchar`, `String`, `Int`) VALUES (@Varchar, @String, @Int);", m_database.Connection);
			command.Parameters.Add(new("@String", MySqlColor.Orange)).MySqlDbType = MySqlDbType.String;
			command.Parameters.Add(new("@Varchar", MySqlColor.Green)).MySqlDbType = MySqlDbType.VarChar;
			command.Parameters.Add(new("@Int", MySqlColor.None));

			await command.ExecuteNonQueryAsync();
			var result = (await m_database.Connection.QueryAsync<ColorEnumValues>(@"select `Varchar`, `String`, `Int` from insert_enum_value2;")).ToArray();
			Assert.Single(result);
			Assert.Equal(MySqlColor.Orange.ToString("G"), result[0].String);
			Assert.Equal(MySqlColor.Green.ToString("G"), result[0].Varchar);
			Assert.Equal((int) MySqlColor.None, result[0].Int);
		}
		finally
		{
			m_database.Connection.Close();
		}
	}

	private enum Enum16 : short
	{
		Off,
		On,
	}

	private enum Enum32 : int
	{
		Off,
		On,
	}

	private enum Enum64 : long
	{
		Off,
		On,
	}

	private class DateTimeOffsetValues
	{
		public DateTimeOffset? DateTimeOffset1 { get; set; }
	}

	private class ColorEnumValues
	{
		public string Varchar { get; set; }
		public string String { get; set; }
		public int Int { get; set; }
	}

	private class EnumValues
	{
		public Enum16? Enum16 { get; set; }
		public Enum32? Enum32 { get; set; }
		public Enum64? Enum64 { get; set; }
	}

	[Fact]
	public void InsertMySqlEnum()
	{
		m_database.Connection.Execute(@"drop table if exists insert_mysql_enums;
create table insert_mysql_enums(
	rowid integer not null primary key auto_increment,
	size enum('x-small', 'small', 'medium', 'large', 'x-large'),
	color enum('red', 'orange', 'yellow', 'green', 'blue', 'indigo', 'violet') not null
);");
		m_database.Connection.Execute(@"insert into insert_mysql_enums(size, color) values(@size, @color);", new { size = MySqlSize.Large, color = MySqlColor.Blue });
		Assert.Equal(new[] { "large" }, m_database.Connection.Query<string>(@"select size from insert_mysql_enums"));
		Assert.Equal(new[] { "blue" }, m_database.Connection.Query<string>(@"select color from insert_mysql_enums"));
	}

	private enum MySqlSize
	{
		None,
		XSmall,
		Small,
		Medium,
		Large,
		XLarge,
	}

	private enum MySqlColor
	{
		None,
		Red,
		Orange,
		Yellow,
		Green,
		Blue,
		Indigo,
		Violet,
	}

	[Fact]
	public void InsertMySqlSet()
	{
		m_database.Connection.Execute(@"drop table if exists insert_mysql_set;
create table insert_mysql_set(
	rowid integer not null primary key auto_increment,
	value set('one', 'two', 'four', 'eight') null
);");
		m_database.Connection.Execute(@"insert into insert_mysql_set(value) values('one'), ('two'), ('one,two'), ('four'), ('four,one'), ('four,two'), ('four,two,one'), ('eight');");
		Assert.Equal(new[] { "one", "one,two", "one,four", "one,two,four" }, m_database.Connection.Query<string>(@"select value from insert_mysql_set where find_in_set('one', value) order by rowid"));
	}

	[Theory]
	[MemberData(nameof(GetChars))]
	public void InsertChar(char ch, bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_char;
create table insert_char(
rowid integer not null primary key auto_increment,
value tinytext null
);");

		using (var cmd = new MySqlCommand("insert into insert_char(value) values(@data);", connection))
		{
			cmd.Parameters.AddWithValue("@data", ch);
			if (prepare)
				cmd.Prepare();
			cmd.ExecuteNonQuery();
		}
		Assert.Equal(ch, connection.Query<char>(@"select value from insert_char;").Single());
	}

	public static IEnumerable<object[]> GetChars() =>
		new[] { '\0', 'a', '\'', '\"', '\\', 'A', '\b', '\n', '\r', '\t', '\x1A' }
			.SelectMany(x => new[] { false, true }.Select(y => new object[] { x, y }));

#if !MYSQL_DATA
	[Theory]
	[MemberData(nameof(GetBlobs))]
	public void InsertBlob(object data, bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		connection.Execute(@"drop table if exists insert_mysql_blob;
create table insert_mysql_blob(
rowid integer not null primary key auto_increment,
value mediumblob null
);");

		using (var cmd = new MySqlCommand("insert into insert_mysql_blob(value) values(@data);", connection))
		{
			cmd.Parameters.AddWithValue("@data", data);
			if (prepare)
				cmd.Prepare();
			cmd.ExecuteNonQuery();
		}
		Assert.Equal(new byte[] { 1, 0, 2, 39, 3, 92, 4, 34, 5, 6 }, connection.Query<byte[]>(@"select value from insert_mysql_blob;").Single());
	}

	public static IEnumerable<object[]> GetBlobs()
	{
		foreach (var blob in new object[]
		{
			new byte[] { 1, 0, 2, 39, 3, 92, 4, 34, 5, 6 },
			new ReadOnlyMemory<byte>(new byte[] { 0, 1, 0, 2, 39, 3, 92, 4, 34, 5, 6, 7, 8 }, 1, 10),
			new Memory<byte>(new byte[] { 0, 1, 0, 2, 39, 3, 92, 4, 34, 5, 6, 7, 8 }, 1, 10),
			new ArraySegment<byte>(new byte[] { 0, 1, 0, 2, 39, 3, 92, 4, 34, 5, 6, 7, 8 }, 1, 10),
		})
		{
			yield return new[] { blob, false };
			yield return new[] { blob, true };
		}
	}
#endif

	private readonly DatabaseFixture m_database;
}
