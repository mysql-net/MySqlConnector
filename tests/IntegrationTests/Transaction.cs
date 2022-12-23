namespace IntegrationTests;

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

	[SkippableTheory(ServerFeatures.GlobalLog)]
	[InlineData(IsolationLevel.ReadUncommitted, "read uncommitted")]
	[InlineData(IsolationLevel.ReadCommitted, "read committed")]
	[InlineData(IsolationLevel.RepeatableRead, "repeatable read")]
	[InlineData(IsolationLevel.Serializable, "serializable")]
	[InlineData(IsolationLevel.Unspecified, "repeatable read")]
#if !MYSQL_DATA
	[InlineData(IsolationLevel.Snapshot, "repeatable read")]
#endif
	public void DbConnectionIsolationLevel(IsolationLevel inputIsolationLevel, string expectedTransactionIsolationLevel)
	{
		DbConnection connection = m_connection;
		connection.Execute(@"set global log_output = 'table';");
		connection.Execute(@"set global general_log = 1;");
		using (var trans = connection.BeginTransaction(inputIsolationLevel))
			trans.Commit();

		connection.Execute(@"set global general_log = 0;");

		// we could use @tx_isolation (in MySQL 5.6/5.7) and @transaction_isolation (in MySQL 5.7/8.0), but would require a different query for different server versions
		var results = connection.Query<string>($"select convert(argument USING utf8) from mysql.general_log where thread_id = @ServerThread and convert(argument using utf8) like '%isolation level%' and argument not like 'select%' order by event_time;", new { m_connection.ServerThread });
		var lastIsolationLevelQuery = results.Last();

		Assert.Contains(expectedTransactionIsolationLevel.ToLower(), lastIsolationLevelQuery.ToLower());
	}

#if !MYSQL_DATA
	[SkippableTheory(ServerFeatures.GlobalLog)]
	[InlineData(IsolationLevel.ReadUncommitted, "start transaction")]
	[InlineData(IsolationLevel.ReadCommitted, "start transaction")]
	[InlineData(IsolationLevel.RepeatableRead, "start transaction")]
	[InlineData(IsolationLevel.Serializable, "start transaction")]
	[InlineData(IsolationLevel.Unspecified, "start transaction")]
	[InlineData(IsolationLevel.Snapshot, "start transaction with consistent snapshot")]
	public void DbConnectionTransactionCommand(IsolationLevel inputIsolationLevel, string expectedTransactionIsolationLevel)
	{
		DbConnection connection = m_connection;
		connection.Execute(@"set global log_output = 'table';");
		connection.Execute(@"set global general_log = 1;");
		using (var trans = connection.BeginTransaction(inputIsolationLevel))
			trans.Commit();

		var results = connection.Query<string>($"select convert(argument USING utf8) from mysql.general_log where thread_id = @ServerThread and convert(argument using utf8) like 'start transaction%' order by event_time;", new { m_connection.ServerThread });
		var lastStartTransactionQuery = results.Last();

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

	[Fact]
	public void ReadOnlyTransaction()
	{
		using var trans = m_connection.BeginTransaction(IsolationLevel.Serializable, isReadOnly: true);
		var exception = Assert.Throws<MySqlException>(() => m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans));
		Assert.Equal(MySqlErrorCode.CannotExecuteInReadOnlyTransaction, exception.ErrorCode);
	}

	[Fact]
	public void ReadOnlySnapshotTransaction()
	{
		using var trans = m_connection.BeginTransaction(IsolationLevel.Snapshot, isReadOnly: true);
		var exception = Assert.Throws<MySqlException>(() => m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans));
		Assert.Equal(MySqlErrorCode.CannotExecuteInReadOnlyTransaction, exception.ErrorCode);
	}

	[Fact]
	public void ReadWriteTransaction()
	{
		using (var trans = m_connection.BeginTransaction(IsolationLevel.Serializable, isReadOnly: false))
		{
			m_connection.Execute("insert into transactions_test values(1), (2)", transaction: trans);
			trans.Commit();
		}
		var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
		Assert.Equal(new[] { 1, 2 }, results);
	}

	[Fact]
	public async Task ReadOnlyTransactionAsync()
	{
		using var trans = await m_connection.BeginTransactionAsync(IsolationLevel.Serializable, isReadOnly: true);
		var exception = await Assert.ThrowsAsync<MySqlException>(async () => await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans));
		Assert.Equal(MySqlErrorCode.CannotExecuteInReadOnlyTransaction, exception.ErrorCode);
	}

	[Fact]
	public async Task ReadWriteTransactionAsync()
	{
		using (var trans = await m_connection.BeginTransactionAsync(IsolationLevel.Serializable, isReadOnly: false))
		{
			await m_connection.ExecuteAsync("insert into transactions_test values(1), (2)", transaction: trans);
			trans.Commit();
		}
		var results = m_connection.Query<int>(@"select value from transactions_test order by value;");
		Assert.Equal(new[] { 1, 2 }, results);
	}

#if !NET462 && !NET472
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

#if !MYSQL_DATA
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

#if !NET462 && !NET472
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

#if !MYSQL_DATA
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
		var ex = Assert.Throws<MySqlException>(() => transaction.Rollback("a"));
		Assert.Equal(MySqlErrorCode.StoredProcedureDoesNotExist, ex.ErrorCode);
	}

	[Fact]
	public void SavepointReleaseUnknownName()
	{
		using var transaction = m_connection.BeginTransaction();
		var ex = Assert.Throws<MySqlException>(() => transaction.Release("a"));
		Assert.Equal(MySqlErrorCode.StoredProcedureDoesNotExist, ex.ErrorCode);
	}

	[Fact]
	public void SavepointRollbackReleasedName()
	{
		using var transaction = m_connection.BeginTransaction();
		transaction.Save("a");
		transaction.Release("a");
		var ex = Assert.Throws<MySqlException>(() => transaction.Rollback("a"));
		Assert.Equal(MySqlErrorCode.StoredProcedureDoesNotExist, ex.ErrorCode);
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

	[SkippableFact(MySqlData = "https://bugs.mysql.com/bug.php?id=109390")]
	public void TransactionHoldsLocks()
	{
		using (var connection = new MySqlConnection(AppConfig.ConnectionString))
		{
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandText = """
				drop table if exists transaction_locks;
				create table transaction_locks(id int not null primary key, val int);
				insert into transaction_locks(id, val) values(1, 1);
				""";
			command.ExecuteNonQuery();
		}
		
		using var barrier = new Barrier(2);
		using var barrier2 = new Barrier(2);

		var task1 = Task.Run(() =>
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using var transaction = connection.BeginTransaction();
				using var command = connection.CreateCommand();
				command.CommandText = "select * from transaction_locks where id = 1 for update";
				command.Transaction = transaction;
				command.ExecuteNonQuery();
				barrier.SignalAndWait();
				barrier2.SignalAndWait();
			}
		});
		var task2 = Task.Run(() =>
		{
			barrier.SignalAndWait();
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using var transaction = connection.BeginTransaction();
				using var command = connection.CreateCommand();
				command.CommandText = "update transaction_locks set val = val + 1 where id = 1";
				command.CommandTimeout = 3;
				command.Transaction = transaction;
				Assert.Throws<MySqlException>(() => command.ExecuteNonQuery());
			}
			barrier2.SignalAndWait();
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using var transaction = connection.BeginTransaction();
				using var command = connection.CreateCommand();
				command.CommandText = "update transaction_locks set val = val + 1 where id = 1";
				command.CommandTimeout = 3;
				command.Transaction = transaction;
				command.ExecuteNonQuery();
				transaction.Commit();
			}
		});

		task1.GetAwaiter().GetResult();
		Assert.Equal(0, Task.WaitAny(task2, Task.Delay(TimeSpan.FromSeconds(10))));
		task2.GetAwaiter().GetResult();
	}

	readonly TransactionFixture m_database;
	readonly MySqlConnection m_connection;
}
