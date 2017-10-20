using System;
using System.Linq;
using System.Transactions;
using Dapper;
using MySql.Data.MySqlClient;
using Xunit;
using SysTransaction = System.Transactions.Transaction;

namespace SideBySide
{
	public class TransactionScopeTests : IClassFixture<DatabaseFixture>
	{
		public TransactionScopeTests(DatabaseFixture database)
		{
			m_database = database;
		}

		[Fact]
		public void EnlistTwoTransactions()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();

				using (var transaction1 = new CommittableTransaction())
				using (var transaction2 = new CommittableTransaction())
				{
					connection.EnlistTransaction(transaction1);
					Assert.Throws<MySqlException>(() => connection.EnlistTransaction(transaction2));
				}
			}
		}

		[Fact]
		public void BeginTransactionInScope()
		{
			using (var transactionScope = new TransactionScope())
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				Assert.Throws<InvalidOperationException>(() => connection.BeginTransaction());
			}
		}

		[Fact]
		public void BeginTransactionThenEnlist()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using (var dbTransaction = connection.BeginTransaction())
				using (var transaction = new CommittableTransaction())
				{
					Assert.Throws<InvalidOperationException>(() => connection.EnlistTransaction(transaction));
				}
			}
		}

		[Fact]
		public void CommitOneTransactionWithTransactionScope()
		{
			m_database.Connection.Execute(@"drop table if exists transaction_scope_test;
				create table transaction_scope_test(value integer not null);");

			using (var transactionScope = new TransactionScope())
			{
				using (var conn = new MySqlConnection(AppConfig.ConnectionString))
				{
					conn.Open();
					conn.Execute("insert into transaction_scope_test(value) values(1), (2);");

					transactionScope.Complete();
				}
			}

			var values = m_database.Connection.Query<int>(@"select value from transaction_scope_test order by value;").ToList();
			Assert.Equal(new[] { 1, 2 }, values);
		}

		[Fact]
		public void CommitOneTransactionWithEnlistTransaction()
		{
			m_database.Connection.Execute(@"drop table if exists transaction_scope_test;
				create table transaction_scope_test(value integer not null);");

			using (var conn = new MySqlConnection(AppConfig.ConnectionString))
			{
				conn.Open();
				using (var transaction = new CommittableTransaction())
				{
					conn.EnlistTransaction(transaction);
					conn.Execute("insert into transaction_scope_test(value) values(1), (2);");
					transaction.Commit();
				}
			}

			var values = m_database.Connection.Query<int>(@"select value from transaction_scope_test order by value;").ToList();
			Assert.Equal(new[] { 1, 2 }, values);
		}

		[Fact]
		public void RollBackOneTransactionWithTransactionScope()
		{
			m_database.Connection.Execute(@"drop table if exists transaction_scope_test;
				create table transaction_scope_test(value integer not null);");

			using (var transactionScope = new TransactionScope())
			{
				using (var conn = new MySqlConnection(AppConfig.ConnectionString))
				{
					conn.Open();
					conn.Execute("insert into transaction_scope_test(value) values(1), (2);");
				}
			}

			var values = m_database.Connection.Query<int>(@"select value from transaction_scope_test order by value;").ToList();
			Assert.Equal(new int[0], values);
		}

		[Fact]
		public void RollBackOneTransactionWithEnlistTransaction()
		{
			m_database.Connection.Execute(@"drop table if exists transaction_scope_test;
				create table transaction_scope_test(value integer not null);");

			using (var conn = new MySqlConnection(AppConfig.ConnectionString))
			{
				conn.Open();
				using (var transaction = new CommittableTransaction())
				{
					conn.EnlistTransaction(transaction);
					conn.Execute("insert into transaction_scope_test(value) values(1), (2);");
				}
			}

			var values = m_database.Connection.Query<int>(@"select value from transaction_scope_test order by value;").ToList();
			Assert.Equal(new int[0], values);
		}

		[Fact]
		public void ThrowExceptionInTransaction()
		{
			m_database.Connection.Execute(@"drop table if exists transaction_scope_test;
				create table transaction_scope_test(value integer not null);");

			try
			{
				using (var transactionScope = new TransactionScope())
				{
					using (var conn = new MySqlConnection(AppConfig.ConnectionString))
					{
						conn.Open();
						conn.Execute("insert into transaction_scope_test(value) values(1), (2);");

						throw new ApplicationException();
					}
				}
			}
			catch (ApplicationException)
			{
			}

			var values = m_database.Connection.Query<int>(@"select value from transaction_scope_test order by value;").ToList();
			Assert.Equal(new int[0], values);
		}


		[Fact]
		public void ThrowExceptionAfterCompleteInTransaction()
		{
			m_database.Connection.Execute(@"drop table if exists transaction_scope_test;
				create table transaction_scope_test(value integer not null);");

			try
			{
				using (var transactionScope = new TransactionScope())
				{
					using (var conn = new MySqlConnection(AppConfig.ConnectionString))
					{
						conn.Open();
						conn.Execute("insert into transaction_scope_test(value) values(1), (2);");

						transactionScope.Complete();

						throw new ApplicationException();
					}
				}
			}
			catch (ApplicationException)
			{
			}

			var values = m_database.Connection.Query<int>(@"select value from transaction_scope_test order by value;").ToList();
			Assert.Equal(new[] { 1, 2 }, values);
		}

		[Fact]
		public void AutoEnlistFalseWithCommit()
		{
			m_database.Connection.Execute(@"drop table if exists transaction_scope_test;
				create table transaction_scope_test(value integer not null);");

			using (var transactionScope = new TransactionScope())
			using (var conn = new MySqlConnection(AppConfig.ConnectionString + ";auto enlist=false"))
			{
				conn.Open();
				using (var dbTransaction = conn.BeginTransaction())
				{
					conn.Execute("insert into transaction_scope_test(value) values(1), (2);");

					dbTransaction.Commit();
					transactionScope.Complete();
				}
			}

			var values = m_database.Connection.Query<int>(@"select value from transaction_scope_test order by value;").ToList();
			Assert.Equal(new[] { 1, 2 }, values);
		}

		[Fact]
		public void AutoEnlistFalseWithoutCommit()
		{
			m_database.Connection.Execute(@"drop table if exists transaction_scope_test;
				create table transaction_scope_test(value integer not null);");

			using (var transactionScope = new TransactionScope())
			using (var conn = new MySqlConnection(AppConfig.ConnectionString + ";auto enlist=false"))
			{
				conn.Open();
				using (var dbTransaction = conn.BeginTransaction())
				{
					conn.Execute("insert into transaction_scope_test(value) values(1), (2);");

#if BASELINE
					// With Connector/NET a MySqlTransaction can't roll back after TransactionScope has been completed;
					// workaround is to explicitly dispose it first. In MySqlConnector (with AutoEnlist=false) they have
					// independent lifetimes.
					dbTransaction.Dispose();
#endif
					transactionScope.Complete();
				}
			}

			var values = m_database.Connection.Query<int>(@"select value from transaction_scope_test order by value;").ToList();
			Assert.Equal(new int[0], values);
		}

		[SkippableFact(Baseline = "Multiple simultaneous connections or connections with different connection strings inside the same transaction are not currently supported.")]
		public void CommitTwoTransactions()
		{
			m_database.Connection.Execute(@"drop table if exists transaction_scope_test_1;
				drop table if exists transaction_scope_test_2;
				create table transaction_scope_test_1(value integer not null);
				create table transaction_scope_test_2(value integer not null);");

			using (var transactionScope = new TransactionScope())
			{
				using (var conn1 = new MySqlConnection(AppConfig.ConnectionString))
				{
					conn1.Open();
					conn1.Execute("insert into transaction_scope_test_1(value) values(1), (2);");

					using (var conn2 = new MySqlConnection(AppConfig.ConnectionString))
					{
						conn2.Open();
						conn2.Execute("insert into transaction_scope_test_2(value) values(3), (4);");

						transactionScope.Complete();
					}
				}
			}

			var values1 = m_database.Connection.Query<int>(@"select value from transaction_scope_test_1 order by value;").ToList();
			var values2 = m_database.Connection.Query<int>(@"select value from transaction_scope_test_2 order by value;").ToList();
			Assert.Equal(new[] { 1, 2 }, values1);
			Assert.Equal(new[] { 3, 4 }, values2);
		}

		[SkippableFact(Baseline = "Multiple simultaneous connections or connections with different connection strings inside the same transaction are not currently supported.")]
		public void RollBackTwoTransactions()
		{
			m_database.Connection.Execute(@"drop table if exists transaction_scope_test_1;
				drop table if exists transaction_scope_test_2;
				create table transaction_scope_test_1(value integer not null);
				create table transaction_scope_test_2(value integer not null);");

			using (var transactionScope = new TransactionScope())
			{
				using (var conn1 = new MySqlConnection(AppConfig.ConnectionString))
				{
					conn1.Open();
					conn1.Execute("insert into transaction_scope_test_1(value) values(1), (2);");

					using (var conn2 = new MySqlConnection(AppConfig.ConnectionString))
					{
						conn2.Open();
						conn2.Execute("insert into transaction_scope_test_2(value) values(3), (4);");
					}
				}
			}

			var values1 = m_database.Connection.Query<int>(@"select value from transaction_scope_test_1 order by value;").ToList();
			var values2 = m_database.Connection.Query<int>(@"select value from transaction_scope_test_2 order by value;").ToList();
			Assert.Equal(new int[0], values1);
			Assert.Equal(new int[0], values2);
		}
		DatabaseFixture m_database;
	}
}
