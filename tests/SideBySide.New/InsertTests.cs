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
		    m_database.Connection.Execute(@"create schema if not exists test;
use test;
drop table if exists test;
create table test(rowid integer not null primary key auto_increment, text varchar(100) not null);");

		    var query = @"insert into test(text) values(@text);
select last_insert_id();";
		    var rowids = m_database.Connection.Query<long>(query, new { text = "Test" });
		    foreach (var rowid in rowids)
			    Assert.Equal(1L, rowid);
	    }

		readonly DatabaseFixture m_database;
	}
}
