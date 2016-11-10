using System;
using System.Linq;
using MySql.Data.MySqlClient;
using Dapper;
using Xunit;

namespace SideBySide.New
{
	public class InsertTests : IClassFixture<DatabaseFixture>
	{
		public InsertTests(DatabaseFixture database)
		{
			m_database = database;
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

		class EnumValues
		{
			public Enum16? Enum16 { get; set; }
			public Enum32? Enum32 { get; set; }
			public Enum64? Enum64 { get; set; }
		}

		readonly DatabaseFixture m_database;
	}
}
