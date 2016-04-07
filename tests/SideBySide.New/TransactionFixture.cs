#if DAPPER
using Dapper;
#endif

namespace SideBySide
{
	public class TransactionFixture : DatabaseFixture
	{
		public TransactionFixture()
		{
			Connection.Open();
			Connection.Execute(@"
drop schema if exists transactions;

create schema transactions;

create table transactions.test(value integer null);
			");
		}
	}
}
