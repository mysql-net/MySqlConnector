namespace IntegrationTests;

public class CommandTimeoutTests : IClassFixture<DatabaseFixture>, IDisposable
{
	public CommandTimeoutTests(DatabaseFixture database)
	{
		m_database = database;
		m_connection = new MySqlConnection(m_database.Connection.ConnectionString);
		m_connection.Open();
	}

	public void Dispose()
	{
		m_connection.Dispose();
	}

	[Theory]
	[InlineData(3)]
	[InlineData(13)]
	public void DefaultCommandTimeoutIsInherited(int defaultCommandTimeout)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.DefaultCommandTimeout = (uint) defaultCommandTimeout;

		using var connection = new MySqlConnection(csb.ConnectionString);
		using var command = connection.CreateCommand();
		Assert.Equal(defaultCommandTimeout, command.CommandTimeout);
	}

	[Fact]
	public void NegativeCommandTimeout()
	{
		using var command = m_connection.CreateCommand();
#if MYSQL_DATA
		Assert.Throws<ArgumentException>(() => command.CommandTimeout = -1);
#else
		Assert.Throws<ArgumentOutOfRangeException>(() => command.CommandTimeout = -1);
#endif
	}

	[Fact]
	public void LargeCommandTimeoutIsCoerced()
	{
		using var command = m_connection.CreateCommand();
		command.CommandTimeout = 2_000_000_000;
		Assert.Equal(2_147_483, command.CommandTimeout);
	}

	[SkippableFact(ServerFeatures.CancelSleepSuccessfully)]
	public void CommandTimeoutWithSleepSync()
	{
		var connectionState = m_connection.State;
		using (var cmd = new MySqlCommand("SELECT SLEEP(120);", m_connection))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
#if MYSQL_DATA
			var ex = Assert.Throws<MySqlException>(cmd.ExecuteReader);
			Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			using (var reader = cmd.ExecuteReader())
			{
				Assert.True(reader.Read());
				Assert.Equal(1, reader.GetInt32(0));
			}
#endif
			sw.Stop();
			TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	[SkippableFact(ServerFeatures.CancelSleepSuccessfully | ServerFeatures.Timeout)]
	public async Task CommandTimeoutWithSleepAsync()
	{
		var connectionState = m_connection.State;
		using (var cmd = new MySqlCommand("SELECT SLEEP(120);", m_connection))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
#if MYSQL_DATA
			var exception = await Assert.ThrowsAsync<MySqlException>(cmd.ExecuteReaderAsync);
			Assert.Contains("fatal error", exception.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			using (var reader = await cmd.ExecuteReaderAsync())
			{
				Assert.True(await reader.ReadAsync());
				Assert.Equal(1, reader.GetInt32(0));
			}
#endif
			sw.Stop();
			TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 700);
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	[SkippableTheory(ServerFeatures.CancelSleepSuccessfully)]
	[InlineData(true)]
	[InlineData(false)]
	public void CommandTimeoutWithStoredProcedureSleepSync(bool pooling)
	{
		using (var setupCmd = new MySqlCommand(@"drop procedure if exists sleep_sproc;
create procedure sleep_sproc(IN seconds INT)
begin
select sleep(seconds);
end;", m_connection))
		{
			setupCmd.ExecuteNonQuery();
		}

		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Pooling = pooling;
		using var connection = new MySqlConnection(csb.ConnectionString);
		using var cmd = new MySqlCommand("sleep_sproc", connection);
		connection.Open();
		cmd.CommandType = CommandType.StoredProcedure;
		cmd.Parameters.AddWithValue("seconds", 10);
		cmd.CommandTimeout = 2;

		var sw = Stopwatch.StartNew();
#if MYSQL_DATA
		var ex = Assert.Throws<MySqlException>(cmd.ExecuteReader);
		Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
#else
		using (var reader = cmd.ExecuteReader())
		{
			Assert.True(reader.Read());
			Assert.Equal(1, reader.GetInt32(0));
		}
#endif
		sw.Stop();
		TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
	}

	[SkippableFact(ServerFeatures.CancelSleepSuccessfully)]
	public void MultipleCommandTimeoutWithSleepSync()
	{
		var connectionState = m_connection.State;
		var csb = new MySqlConnectionStringBuilder(m_connection.ConnectionString);
		using (var cmd = new MySqlCommand("SELECT 1; SELECT SLEEP(120);", m_connection))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
			using var reader = cmd.ExecuteReader();
			Assert.True(reader.Read());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.False(reader.Read());

			// the following call to a public API resets the internal timer
			sw.Restart();

#if MYSQL_DATA
			var ex = Assert.Throws<MySqlException>(() => reader.NextResult());
			Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			Assert.True(reader.NextResult());
			Assert.True(reader.Read());
			Assert.Equal(1, reader.GetInt32(0));
#endif

			sw.Stop();
			TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	[SkippableFact(ServerFeatures.CancelSleepSuccessfully | ServerFeatures.Timeout)]
	public async Task MultipleCommandTimeoutWithSleepAsync()
	{
		var connectionState = m_connection.State;
		using (var cmd = new MySqlCommand("SELECT 1; SELECT SLEEP(120);", m_connection))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
			using var reader = await cmd.ExecuteReaderAsync();
			Assert.True(await reader.ReadAsync());
			Assert.Equal(1, reader.GetInt32(0));
			Assert.False(await reader.ReadAsync());

			// the following call to a public API resets the internal timer
			sw.Restart();

#if MYSQL_DATA
			var ex = await Assert.ThrowsAsync<MySqlException>(async () => await reader.NextResultAsync());
			Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			Assert.True(await reader.NextResultAsync());
			Assert.True(reader.Read());
			Assert.Equal(1, reader.GetInt32(0));
#endif

			sw.Stop();
			TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 550);
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	[SkippableFact(ServerFeatures.CancelSleepSuccessfully, MySqlData = "https://bugs.mysql.com/bug.php?id=88124")]
	public void CommandTimeoutResetsOnReadSync()
	{
		var csb = new MySqlConnectionStringBuilder(m_connection.ConnectionString);
		using (var cmd = new MySqlCommand("SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1);", m_connection))
		{
			cmd.CommandTimeout = 3;
			using var reader = cmd.ExecuteReader();

			for (int i = 0; i < 5; i++)
			{
				Assert.True(reader.Read());
				Assert.Equal(0, reader.GetInt32(0));
				Assert.False(reader.Read());
				Assert.Equal(i < 4, reader.NextResult());
			}
		}

		Assert.Equal(ConnectionState.Open, m_connection.State);
	}

	[SkippableFact(ServerFeatures.CancelSleepSuccessfully | ServerFeatures.Timeout, MySqlData = "https://bugs.mysql.com/bug.php?id=88124")]
	public async Task CommandTimeoutResetsOnReadAsync()
	{
		var csb = new MySqlConnectionStringBuilder(m_connection.ConnectionString);
		using (var cmd = new MySqlCommand("SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1);", m_connection))
		{
			cmd.CommandTimeout = 3;
			using var reader = await cmd.ExecuteReaderAsync();

			for (int i = 0; i < 5; i++)
			{
				Assert.True(await reader.ReadAsync());
				Assert.Equal(0, reader.GetInt32(0));
				Assert.False(await reader.ReadAsync());
				Assert.Equal(i < 4, await reader.NextResultAsync());
			}
		}

		Assert.Equal(ConnectionState.Open, m_connection.State);
	}


	[SkippableFact(ServerFeatures.CancelSleepSuccessfully)]
	public void TransactionCommandTimeoutWithSleepSync()
	{
		var connectionState = m_connection.State;
		using (var transaction = m_connection.BeginTransaction())
		using (var cmd = new MySqlCommand("SELECT SLEEP(120);", m_connection, transaction))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
#if MYSQL_DATA
			var ex = Assert.Throws<MySqlException>(cmd.ExecuteReader);
			Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			using (var reader = cmd.ExecuteReader())
			{
				Assert.True(reader.Read());
				Assert.Equal(1, reader.GetInt32(0));
			}
#endif
			sw.Stop();
			TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	[SkippableFact(ServerFeatures.CancelSleepSuccessfully)]
	public async Task TransactionCommandTimeoutWithSleepAsync()
	{
		var connectionState = m_connection.State;
		using (var transaction = await m_connection.BeginTransactionAsync())
		using (var cmd = new MySqlCommand("SELECT SLEEP(120);", m_connection, transaction))
		{
			cmd.CommandTimeout = 2;
			var sw = Stopwatch.StartNew();
#if MYSQL_DATA
			var ex = await Assert.ThrowsAsync<MySqlException>(cmd.ExecuteReaderAsync);
			Assert.Contains("fatal error", ex.Message, StringComparison.OrdinalIgnoreCase);
			connectionState = ConnectionState.Closed;
#else
			using (var reader = await cmd.ExecuteReaderAsync())
			{
				Assert.True(await reader.ReadAsync());
				Assert.Equal(1, reader.GetInt32(0));
			}
#endif
			sw.Stop();
		}

		Assert.Equal(connectionState, m_connection.State);
	}

	readonly DatabaseFixture m_database;
	readonly MySqlConnection m_connection;
}
