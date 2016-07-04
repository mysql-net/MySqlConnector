using System;
using System.Linq;
using Dapper;
using Xunit;

namespace SideBySide.New
{
	public class InsertTests : IClassFixture<DatabaseFixture>
	{
		public InsertTests(DatabaseFixture database)
		{
			m_database = database;
			m_database.Connection.Execute(@"create schema if not exists test; drop table if exists test.test;");
		}

		[Fact]
		public void InsertWithDapper()
		{
			m_database.Connection.Execute(@"create table test.test(rowid integer not null primary key auto_increment, text varchar(100) not null);");

			var query = @"insert into test.test(text) values(@text);
select last_insert_id();";
			var rowids = m_database.Connection.Query<long>(query, new { text = "Test" });
			foreach (var rowid in rowids)
				Assert.Equal(1L, rowid);
		}

		[Fact]
		public void InsertEnumValue()
		{
			m_database.Connection.Execute(@"create table test.test(rowid integer not null primary key auto_increment, Enum16 int null, Enum32 int null, Enum64 bigint null);");
			m_database.Connection.Execute(@"insert into test.test(Enum16, Enum32, Enum64) values(@e16a, @e32a, @e64a), (@e16b, @e32b, @e64b);",
				new { e16a = default(Enum16?), e32a = default(Enum32?), e64a = default(Enum64?), e16b = Enum16.On, e32b = Enum32.Off, e64b = Enum64.On });
			var results = m_database.Connection.Query<EnumValues>(@"select Enum16, Enum32, Enum64 from test.test order by rowid;").ToList();
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
