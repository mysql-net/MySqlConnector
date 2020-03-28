using System;
using System.Data;
using System.Data.Common;
using System.Linq;
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
			m_connection.Execute("delete from transactions_test");
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
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Commit();
			}
			var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new[] { 1, 2 }, results);
		}

		[Fact]
		public void DbConnectionCommit()
		{
			DbConnection connection = m_connection;
			using (var trans = connection.BeginTransaction())
			{
				connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Commit();
			}
			var results = connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new[] { 1, 2 }, results);
		}

		[Theory]
		[InlineData(IsolationLevel.ReadUncommitted, "read uncommitted")]
		[InlineData(IsolationLevel.ReadCommitted, "read committed")]
		[InlineData(IsolationLevel.RepeatableRead, "repeatable read")]
		[InlineData(IsolationLevel.Serializable, "serializable")]
		[InlineData(IsolationLevel.Unspecified, "repeatable read")]
#if !BASELINE
		[InlineData(IsolationLevel.Snapshot, "repeatable read")]
#endif
		public void DbConnectionIsolationLevel(IsolationLevel inputIsolationLevel, string expectedTransactionIsolationLevel)
		{
			DbConnection connection = m_connection;
			m_connection.Execute(@"set global log_output = 'table';");
			m_connection.Execute(@"set global general_log = 1;");
			using (var trans = connection.BeginTransaction(inputIsolationLevel))
			{
				trans.Commit();
			}

			m_connection.Execute(@"set global general_log = 0;");
			var results = connection.Query<string>($"select convert(argument USING utf8) from mysql.general_log where thread_id = {m_connection.ServerThread} order by event_time desc limit 10;");
			var lastIsolationLevelQuery = results.First(x => x.ToLower().Contains("isolation"));

			if (IsMySqlAndVersionLessThan57(m_connection.ServerVersion))
			{
				Assert.Contains("serializable", lastIsolationLevelQuery.ToLower());
				return;
			}

			Assert.Contains(expectedTransactionIsolationLevel.ToLower(), lastIsolationLevelQuery.ToLower());
		}

#if !BASELINE
		[Theory]
		[InlineData(IsolationLevel.ReadUncommitted, "start transaction")]
		[InlineData(IsolationLevel.ReadCommitted, "start transaction")]
		[InlineData(IsolationLevel.RepeatableRead, "start transaction")]
		[InlineData(IsolationLevel.Serializable, "start transaction")]
		[InlineData(IsolationLevel.Unspecified, "start transaction")]
		[InlineData(IsolationLevel.Snapshot, "start transaction with consistent snapshot")]
		public void DbConnectionTransactionCommand(IsolationLevel inputIsolationLevel, string expectedTransactionIsolationLevel)
		{
			DbConnection connection = m_connection;
			m_connection.Execute(@"set global log_output = 'table';");
			m_connection.Execute(@"set global general_log = 1;");
			using (var trans = connection.BeginTransaction(inputIsolationLevel))
			{
				trans.Commit();
			}

			m_connection.Execute(@"set global general_log = 0;");
			var results = connection.Query<string>($"select convert(argument USING utf8) from mysql.general_log where thread_id = {m_connection.ServerThread} order by event_time desc limit 10;");
			var lastStartTransactionQuery = results.First(x => x.ToLower().Contains("start"));

			if (IsMySqlAndVersionLessThan57(m_connection.ServerVersion))
			{
				Assert.Contains("start transaction", lastStartTransactionQuery.ToLower());
				return;
			}

			Assert.Contains(expectedTransactionIsolationLevel.ToLower(), lastStartTransactionQuery.ToLower());
		}

		[Fact]
		public async Task CommitAsync()
		{
			using (var trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false))
			{
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.CommitAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new[] { 1, 2 }, results);
		}

		[Fact]
		public async Task CommitDisposeAsync()
		{
			MySqlTransaction trans = null;
			try
			{
				trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false);
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.CommitAsync().ConfigureAwait(false);
			}
			finally
			{
				await trans.DisposeAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new[] { 1, 2 }, results);
		}

#if !NET452 && !NET461 && !NET472 && !NETCOREAPP1_1_2 && !NETCOREAPP2_0 && !NETCOREAPP2_1
		[Fact]
		public async Task DbConnectionCommitAsync()
		{
			DbConnection connection = m_connection;
			using (var trans = await connection.BeginTransactionAsync().ConfigureAwait(false))
			{
				await connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.CommitAsync().ConfigureAwait(false);
			}
			var results = await connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new[] { 1, 2 }, results);
		}
#endif
#endif

		[Fact]
		public void Rollback()
		{
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Rollback();
			}
			var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new int[0], results);
		}

		[Fact]
		public void DbConnectionRollback()
		{
			DbConnection connection = m_connection;
			using (var trans = connection.BeginTransaction())
			{
				connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Rollback();
			}
			var results = connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new int[0], results);
		}

#if !BASELINE
		[Fact]
		public async Task RollbackAsync()
		{
			using (var trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false))
			{
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.RollbackAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new int[0], results);
		}

		[Fact]
		public async Task RollbackDisposeAsync()
		{
			MySqlTransaction trans = null;
			try
			{
				trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false);
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.RollbackAsync().ConfigureAwait(false);
			}
			finally
			{
				await trans.DisposeAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new int[0], results);
		}

#if !NET452 && !NET461 && !NET472 && !NETCOREAPP1_1_2 && !NETCOREAPP2_0 && !NETCOREAPP2_1
		[Fact]
		public async Task DbConnectionRollbackAsync()
		{
			DbConnection connection = m_connection;
			using (var trans = await connection.BeginTransactionAsync().ConfigureAwait(false))
			{
				await connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
				await trans.RollbackAsync().ConfigureAwait(false);
			}
			var results = await connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new int[0], results);
		}
#endif
#endif

		[Fact]
		public void NoCommit()
		{
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
			}
			var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new int[0], results);
		}

#if !BASELINE
		[Fact]
		public void SavepointNullName()
		{
			using var transaction = m_connection.BeginTransaction();
			Assert.Throws<ArgumentNullException>("savepointName", () => transaction.Save(null));
			Assert.Throws<ArgumentNullException>("savepointName", () => transaction.Release(null));
			Assert.Throws<ArgumentNullException>("savepointName", () => transaction.Rollback(null));
		}

		[Fact]
		public void SavepointEmptyName()
		{
			using var transaction = m_connection.BeginTransaction();
			Assert.Throws<ArgumentException>("savepointName", () => transaction.Save(""));
			Assert.Throws<ArgumentException>("savepointName", () => transaction.Release(""));
			Assert.Throws<ArgumentException>("savepointName", () => transaction.Rollback(""));
		}

		[Fact]
		public void SavepointRollbackUnknownName()
		{
			using var transaction = m_connection.BeginTransaction();
			try
			{
				transaction.Rollback("a");
				Assert.True(false);
			}
			catch (MySqlException ex)
			{
				Assert.Equal(MySqlErrorCode.StoredProcedureDoesNotExist, (MySqlErrorCode)ex.Number);
			}
		}

		[Fact]
		public void SavepointReleaseUnknownName()
		{
			using var transaction = m_connection.BeginTransaction();
			try
			{
				transaction.Release("a");
				Assert.True(false);
			}
			catch (MySqlException ex)
			{
				Assert.Equal(MySqlErrorCode.StoredProcedureDoesNotExist, (MySqlErrorCode)ex.Number);
			}
		}

		[Fact]
		public void SavepointRollbackReleasedName()
		{
			using var transaction = m_connection.BeginTransaction();
			transaction.Save("a");
			transaction.Release("a");
			try
			{
				transaction.Rollback("a");
				Assert.True(false);
			}
			catch (MySqlException ex)
			{
				Assert.Equal(MySqlErrorCode.StoredProcedureDoesNotExist, (MySqlErrorCode)ex.Number);
			}
		}

		[Fact]
		public void RollbackSavepoint()
		{
			using (var trans = m_connection.BeginTransaction())
			{
				m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
				trans.Save("a");
				m_connection.Execute("insert into transactions_test values(3), (4)", transaction: trans);
				trans.Save("b");
				m_connection.Execute("insert into transactions_test values(5), (6)", transaction: trans);
				trans.Rollback("a");
				trans.Commit();
			}
			var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new int[] { 1, 2 }, results);
		}

		[Fact]
		public async Task RollbackSavepointAsync()
		{
			using (var trans = await m_connection.BeginTransactionAsync())
			{
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans);
				await trans.SaveAsync("a");
				await m_connection.ExecuteAsync("insert into transactions_test values(3), (4)", transaction: trans);
				await trans.SaveAsync("b");
				await m_connection.ExecuteAsync("insert into transactions_test values(5), (6)", transaction: trans);
				await trans.RollbackAsync("a");
				await trans.CommitAsync();
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;");
			Assert.Equal(new int[] { 1, 2 }, results);
		}

		[Fact]
		public void SavepointRolledBackTransaction()
		{
			using (var transaction = m_connection.BeginTransaction())
			{
				transaction.Rollback();
				Assert.Throws<InvalidOperationException>(() => transaction.Save("a"));
			}
		}

		[Fact]
		public void SavepointCommittedTransaction()
		{
			using (var transaction = m_connection.BeginTransaction())
			{
				transaction.Commit();
				Assert.Throws<InvalidOperationException>(() => transaction.Save("a"));
			}
		}

		[Fact]
		public async Task DisposeAsync()
		{
			MySqlTransaction trans = null;
			try
			{
				trans = await m_connection.BeginTransactionAsync().ConfigureAwait(false);
				await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans).ConfigureAwait(false);
			}
			finally
			{
				await trans.DisposeAsync().ConfigureAwait(false);
			}
			var results = await m_connection.QueryAsync<int>(@"select value from transactions_test order by value;").ConfigureAwait(false);
			Assert.Equal(new int[0], results);
		}
#endif

		private bool IsMySqlAndVersionLessThan57(string currentVersionStr)
		{
			var version = new Version("5.7");
			Version currentVersion = null;

			if (Version.TryParse(currentVersionStr, out currentVersion))
			{
				var result = version.CompareTo(currentVersion);
				return result > 0;
			}

			return false;
		}

		readonly TransactionFixture m_database;
		readonly MySqlConnection m_connection;
	}
}
