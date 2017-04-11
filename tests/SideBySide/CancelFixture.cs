using Dapper;

namespace SideBySide
{
	public class CancelFixture : DatabaseFixture
	{
		public CancelFixture()
		{
			Connection.Open();
			Connection.Execute(@"drop table if exists integers;
				create table integers(value int not null primary key);
				insert into integers(value) values (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20);");
		}
	}
}
