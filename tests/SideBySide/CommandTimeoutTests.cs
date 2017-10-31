using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace SideBySide
{
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

			using (var connection = new MySqlConnection(csb.ConnectionString))
			using (var command = connection.CreateCommand())
			{
				Assert.Equal(defaultCommandTimeout, command.CommandTimeout);
			}
		}

		[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=87316")]
		public void NegativeCommandTimeout()
		{
			using (var command = m_connection.CreateCommand())
			{
				Assert.Throws<ArgumentOutOfRangeException>(() => command.CommandTimeout = -1);
			}
		}

		[Fact]
		public void LargeCommandTimeoutIsCoerced()
		{
			using (var command = m_connection.CreateCommand())
			{
				command.CommandTimeout = 2_000_000_000;
				Assert.Equal(2_147_483, command.CommandTimeout);
			}
		}

		[Fact]
		public void CommandTimeoutWithSleepSync()
		{
			using (var cmd = new MySqlCommand("SELECT SLEEP(120);", m_connection))
			{
				cmd.CommandTimeout = 2;
				var sw = Stopwatch.StartNew();
				try
				{
					using (var reader = cmd.ExecuteReader())
					{
						// shouldn't get here
						Assert.True(false);
					}
				}
				catch (MySqlException ex)
				{
					sw.Stop();
					Assert.Contains("timeout", ex.Message, StringComparison.OrdinalIgnoreCase);
#if !BASELINE
					// https://bugs.mysql.com/bug.php?id=86009
					TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
#endif
				}
			}

#if !BASELINE
			Assert.Equal(ConnectionState.Closed, m_connection.State);
#endif
		}

		[Fact]
		public async Task CommandTimeoutWithSleepAsync()
		{
			using (var cmd = new MySqlCommand("SELECT SLEEP(120);", m_connection))
			{
				cmd.CommandTimeout = 2;
				var sw = Stopwatch.StartNew();
				try
				{
					using (var reader = await cmd.ExecuteReaderAsync())
					{
						// shouldn't get here
						Assert.True(false);
					}
				}
				catch (MySqlException ex)
				{
					sw.Stop();
					Assert.Contains("timeout", ex.Message, StringComparison.OrdinalIgnoreCase);
#if !BASELINE
					// https://bugs.mysql.com/bug.php?id=86009
					TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
#endif
				}
			}

#if !BASELINE
			Assert.Equal(ConnectionState.Closed, m_connection.State);
#endif
		}

		[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=87307")]
		public void MultipleCommandTimeoutWithSleepSync()
		{
			var csb = new MySqlConnectionStringBuilder(m_connection.ConnectionString);
			using (var cmd = new MySqlCommand("SELECT 1; SELECT SLEEP(120);", m_connection))
			{
				cmd.CommandTimeout = 2;
				var readFirstResultSet = false;
				var sw = Stopwatch.StartNew();
				try
				{
					using (var reader = cmd.ExecuteReader())
					{
						Assert.True(reader.Read());
						Assert.Equal(1, reader.GetInt32(0));
						Assert.False(reader.Read());
						readFirstResultSet = true;

#if !BASELINE
						// if not buffering, the following call to a public API resets the internal timer
						if (!csb.BufferResultSets)
#endif
							sw.Restart();

						reader.NextResult();

						// shouldn't get here
						TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
						Assert.True(false);
					}
				}
				catch (MySqlException ex)
				{
					sw.Stop();
					Assert.True(readFirstResultSet);
					Assert.Contains("timeout", ex.Message, StringComparison.OrdinalIgnoreCase);
					TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
				}
			}

#if !BASELINE
			Assert.Equal(ConnectionState.Closed, m_connection.State);
#endif
		}

		[SkippableFact(Baseline = "https://bugs.mysql.com/bug.php?id=87307")]
		public async Task MultipleCommandTimeoutWithSleepAsync()
		{
			var csb = new MySqlConnectionStringBuilder(m_connection.ConnectionString);
			using (var cmd = new MySqlCommand("SELECT 1; SELECT SLEEP(120);", m_connection))
			{
				cmd.CommandTimeout = 2;
				var readFirstResultSet = false;
				var sw = Stopwatch.StartNew();
				try
				{
					using (var reader = await cmd.ExecuteReaderAsync())
					{
						Assert.True(await reader.ReadAsync());
						Assert.Equal(1, reader.GetInt32(0));
						Assert.False(await reader.ReadAsync());
						readFirstResultSet = true;

#if !BASELINE
						// if not buffering, the following call to a public API resets the internal timer
						if (!csb.BufferResultSets)
#endif
							sw.Restart();

						await reader.NextResultAsync();

						// shouldn't get here
						Assert.True(false);
					}
				}
				catch (MySqlException ex)
				{
					sw.Stop();
					Assert.True(readFirstResultSet);
					Assert.Contains("timeout", ex.Message, StringComparison.OrdinalIgnoreCase);
					TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
				}
			}

#if !BASELINE
			Assert.Equal(ConnectionState.Closed, m_connection.State);
#endif
		}

		[SkippableFact(ConfigSettings.UnbufferedResultSets, Baseline = "https://bugs.mysql.com/bug.php?id=88124")]
		public void CommandTimeoutResetsOnReadSync()
		{
			var csb = new MySqlConnectionStringBuilder(m_connection.ConnectionString);
			using (var cmd = new MySqlCommand("SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1);", m_connection))
			{
				cmd.CommandTimeout = 3;
				using (var reader = cmd.ExecuteReader())
				{
					for (int i = 0; i < 5; i++)
					{
						Assert.True(reader.Read());
						Assert.Equal(0, reader.GetInt32(0));
						Assert.False(reader.Read());
						Assert.Equal(i < 4, reader.NextResult());
					}
				}
			}

			Assert.Equal(ConnectionState.Open, m_connection.State);
		}

		[SkippableFact(ConfigSettings.UnbufferedResultSets, Baseline = "https://bugs.mysql.com/bug.php?id=88124")]
		public async Task CommandTimeoutResetsOnReadAsync()
		{
			var csb = new MySqlConnectionStringBuilder(m_connection.ConnectionString);
			using (var cmd = new MySqlCommand("SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1); SELECT SLEEP(1);", m_connection))
			{
				cmd.CommandTimeout = 3;
				using (var reader = await cmd.ExecuteReaderAsync())
				{
					for (int i = 0; i < 5; i++)
					{
						Assert.True(await reader.ReadAsync());
						Assert.Equal(0, reader.GetInt32(0));
						Assert.False(await reader.ReadAsync());
						Assert.Equal(i < 4, await reader.NextResultAsync());
					}
				}
			}

			Assert.Equal(ConnectionState.Open, m_connection.State);
		}


		[Fact]
		public void TransactionCommandTimeoutWithSleepSync()
		{
			using (var transaction = m_connection.BeginTransaction())
			using (var cmd = new MySqlCommand("SELECT SLEEP(120);", m_connection, transaction))
			{
				cmd.CommandTimeout = 2;
				var sw = Stopwatch.StartNew();
				try
				{
					using (var reader = cmd.ExecuteReader())
					{
						// shouldn't get here
						Assert.True(false);
					}
				}
				catch (MySqlException ex)
				{
					sw.Stop();
					Assert.Contains("timeout", ex.Message, StringComparison.OrdinalIgnoreCase);
#if !BASELINE
					// https://bugs.mysql.com/bug.php?id=86009
					TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
#endif
				}
			}

#if !BASELINE
			Assert.Equal(ConnectionState.Closed, m_connection.State);
#endif
		}

		[Fact]
		public async Task TransactionCommandTimeoutWithSleepAsync()
		{
			using (var transaction = await m_connection.BeginTransactionAsync())
			using (var cmd = new MySqlCommand("SELECT SLEEP(120);", m_connection, transaction))
			{
				cmd.CommandTimeout = 2;
				var sw = Stopwatch.StartNew();
				try
				{
					using (var reader = await cmd.ExecuteReaderAsync())
					{
						// shouldn't get here
						Assert.True(false);
					}
				}
				catch (MySqlException ex)
				{
					sw.Stop();
					Assert.Contains("timeout", ex.Message, StringComparison.OrdinalIgnoreCase);
#if !BASELINE
					// https://bugs.mysql.com/bug.php?id=86009
					TestUtilities.AssertDuration(sw, cmd.CommandTimeout * 1000 - 100, 500);
#endif
				}
			}

#if !BASELINE
			Assert.Equal(ConnectionState.Closed, m_connection.State);
#endif
		}

		readonly DatabaseFixture m_database;
		readonly MySqlConnection m_connection;
	}
}
