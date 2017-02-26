using System;
using System.Linq;
using MySql.Data.MySqlClient;
using Dapper;
using Xunit;
using System.Data;
using System.Threading.Tasks;

namespace SideBySide
{
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
				using (var command = new MySqlCommand("INSERT INTO insert_ai (text) VALUES (@text);", m_database.Connection))
				{
					command.Parameters.Add(new MySqlParameter { ParameterName = "@text", Value = "test" });
					await command.ExecuteNonQueryAsync();
					Assert.Equal(command.LastInsertedId, 1L);
				}
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

#if BASELINE
		[Theory(Skip = "http://bugs.mysql.com/bug.php?id=70686")]
#else
		[Theory]
#endif
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
					command.Parameters.Add(new MySqlParameter { ParameterName = "@value", Value = TimeSpan.FromMilliseconds(10) });
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


#if BASELINE
		[Fact(Skip = "https://bugs.mysql.com/bug.php?id=73788")]
#else
		[Fact]
#endif
		public void InsertDateTimeOffset()
		{
			m_database.Connection.Execute(@"drop table if exists insert_datetimeoffset;
create table insert_datetimeoffset(rowid integer not null primary key auto_increment, datetimeoffset1 datetime null);");
			var value = new DateTimeOffsetValues { datetimeoffset1 = new DateTimeOffset(2017, 1, 2, 3, 4, 5, TimeSpan.FromMinutes(678)) };

			m_database.Connection.Open();
			try
			{
				using (var cmd = m_database.Connection.CreateCommand())
				{
					cmd.CommandText = @"insert into insert_datetimeoffset(datetimeoffset1) values(@datetimeoffset1);";
					cmd.Parameters.Add(new MySqlParameter
					{
						ParameterName = "@datetimeoffset1",
						DbType = DbType.DateTimeOffset,
						Value = value.datetimeoffset1
					});
					cmd.ExecuteNonQuery();
				}
			}
			finally
			{
				m_database.Connection.Close();
			}

			var datetime = m_database.Connection.ExecuteScalar<DateTime>(@"select datetimeoffset1 from insert_datetimeoffset order by rowid;");

			DateTime.SpecifyKind(datetime, DateTimeKind.Utc);

			Assert.Equal(value.datetimeoffset1.Value.UtcDateTime, datetime);
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
			Assert.Equal(null, results[0].Enum16);
			Assert.Equal(null, results[0].Enum32);
			Assert.Equal(null, results[0].Enum64);
			Assert.Equal(Enum16.On, results[1].Enum16);
			Assert.Equal(Enum32.Off, results[1].Enum32);
			Assert.Equal(Enum64.On, results[1].Enum64);
		}

		enum Enum16 : short
		{
			Off,
			On,
		}

		enum Enum32 : int
		{
			Off,
			On,
		}

		enum Enum64 : long
		{
			Off,
			On,
		}

		class DateTimeOffsetValues
		{
			public DateTimeOffset? datetimeoffset1 { get; set; }
		}


		class EnumValues
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

		enum MySqlSize
		{
			None,
			XSmall,
			Small,
			Medium,
			Large,
			XLarge
		}

		enum MySqlColor
		{
			None,
			Red,
			Orange,
			Yellow,
			Green,
			Blue,
			Indigo,
			Violet
		}

		[Fact]
		public void InsertMySqSet()
		{
			m_database.Connection.Execute(@"drop table if exists insert_mysql_set;
create table insert_mysql_set(
	rowid integer not null primary key auto_increment,
	value set('one', 'two', 'four', 'eight') null
);");
			m_database.Connection.Execute(@"insert into insert_mysql_set(value) values('one'), ('two'), ('one,two'), ('four'), ('four,one'), ('four,two'), ('four,two,one'), ('eight');");
			Assert.Equal(new[] { "one", "one,two", "one,four", "one,two,four" }, m_database.Connection.Query<string>(@"select value from insert_mysql_set where find_in_set('one', value) order by rowid"));
		}

		readonly DatabaseFixture m_database;
	}
}
