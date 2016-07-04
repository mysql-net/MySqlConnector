using System;
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
			m_connection.Execute("delete from transactions.test");
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions.test values(1), (2)", transaction: trans);
				trans.Commit();
			}
			var results = m_connection.Query<int>(@"select value from transactions.test order by value;");
			Assert.Equal(new[] { 1, 2 }, results);
		}

		[Fact]
		public void Rollback()
		{
			m_connection.Execute("delete from transactions.test");
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions.test values(1), (2)", transaction: trans);
				trans.Rollback();
			}
			var results = m_connection.Query<int>(@"select value from transactions.test order by value;");
			Assert.Equal(new int[0], results);
		}

		[Fact]
		public void NoCommit()
		{
			m_connection.Execute("delete from transactions.test");
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions.test values(1), (2)", transaction: trans);
			}
			var results = m_connection.Query<int>(@"select value from transactions.test order by value;");
			Assert.Equal(new int[0], results);
		}

		readonly TransactionFixture m_database;
		readonly MySqlConnection m_connection;
	}
}
