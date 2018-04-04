using System;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class Transaction : IClassFixture<TransactionFixture>
	{
		public Transaction(TransactionFixture database)
		{
			m_database = database;
			m_connection = m_database.Connection;
		}

		[Fact]
		public void NestedTransactions()
		{
			using (m_connection.BeginTransaction())
			{
				Assert.Throws<InvalidOperationException>(() => m_connection.BeginTransaction());
			}
		}

		[Fact]
		public void Commit()
		{
			m_connection.Execute("delete from transactions_test");
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Commit();
			}
			var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new[] { 1, 2 }, results);
		}

#if !BASELINE
		[Fact]
		public async Task CommitAsync()
		{
			await m_connection.ExecuteAsync("delete from transactions_test").ConfigureAwait(false);
			using (var trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false))
			{
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.CommitAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new[] { 1, 2 }, results);
		}
#endif

		[Fact]
		public void Rollback()
		{
			m_connection.Execute("delete from transactions_test");
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Rollback();
			}
			var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new int[0], results);
		}

#if !BASELINE
		[Fact]
		public async Task RollbackAsync()
		{
			await m_connection.ExecuteAsync("delete from transactions_test").ConfigureAwait(false);
			using (var trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false))
			{
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.RollbackAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new int[0], results);
		}
#endif

		[Fact]
		public void NoCommit()
		{
			m_connection.Execute("delete from transactions_test");
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
			}
			var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new int[0], results);
		}

		readonly TransactionFixture m_database;
		readonly MySqlConnection m_connection;
	}
}
