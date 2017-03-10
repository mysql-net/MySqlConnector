using Dapper;

namespace SideBySide
{
	public class TransactionFixture : DatabaseFixture
	{
		public TransactionFixture()
		{
			Connection.Open();
			Connection.Execute(@"
drop table if exists transactions_test;
create table transactions_test(value integer null);
			");
		}
	}
}
