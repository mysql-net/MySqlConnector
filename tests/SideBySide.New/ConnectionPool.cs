using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ConnectionPool
	{
		[Theory]
		[InlineData(false, 11, 0L)]
		[InlineData(true, 12, 1L)]
#if BASELINE
		// baseline default behaviour is to not reset the connection, which trades correctness for speed
		// see bug report at http://bugs.mysql.com/bug.php?id=77421
		[InlineData(null, 13, 0L)]
#else
		[InlineData(null, 13, 1L)]
#endif
		public void ResetConnection(object connectionReset, int poolSize, long expected)
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.Pooling = true;
			csb.MaximumPoolSize = (uint) poolSize; // use a different pool size to create a unique connection string to force a unique pool to be created

#if BASELINE
			if (true.Equals(connectionReset))
			{
				using (var connection = new MySqlConnection(csb.ConnectionString))
				{
					connection.Open();
					using (var command = connection.CreateCommand())
					{
						command.CommandText = "create schema if not exists test;";
						command.ExecuteNonQuery();
					}
				}

				// baseline connector needs to have a database specified in the connection string to reauth properly (which is how connection reset is implemented prior to MySQL 5.7.3)
				csb.Database = "test";
			}
#endif

			if (connectionReset != null)
				csb.ConnectionReset = (bool) connectionReset;

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select @@autocommit;";
					Assert.Equal(1L, command.ExecuteScalar());
				}
			}

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "SET autocommit=0;";
					command.ExecuteNonQuery();
				}
			}

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select @@autocommit;";
					Assert.Equal(expected, command.ExecuteScalar());
				}
			}
		}

		[Fact]
		public async Task ExhaustConnectionPool()
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.Pooling = true;
			csb.MinimumPoolSize = 0;
			csb.MaximumPoolSize = 3;
			csb.ConnectionTimeout = 60;

			var connections = new List<MySqlConnection>();

			for (int i = 0; i < csb.MaximumPoolSize; i++)
			{
				var connection = new MySqlConnection(csb.ConnectionString);
				await connection.OpenAsync().ConfigureAwait(false);
				connections.Add(connection);
			}

			var closeTask = Task.Run(() =>
			{
				Thread.Sleep(5000);
				connections[0].Dispose();
				connections.RemoveAt(0);
			});

			using (var extraConnection = new MySqlConnection(csb.ConnectionString))
			{
				var stopwatch = Stopwatch.StartNew();
				await extraConnection.OpenAsync().ConfigureAwait(false);
				stopwatch.Stop();
				Assert.InRange(stopwatch.ElapsedMilliseconds, 4500, 7500);
			}

			closeTask.Wait();

			foreach (var connection in connections)
				connection.Dispose();
		}

		[Fact]
		public async Task ExhaustConnectionPoolWithTimeout()
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.Pooling = true;
			csb.MinimumPoolSize = 0;
			csb.MaximumPoolSize = 3;
			csb.ConnectionTimeout = 5;

			var connections = new List<MySqlConnection>();

			for (int i = 0; i < csb.MaximumPoolSize; i++)
			{
				var connection = new MySqlConnection(csb.ConnectionString);
				await connection.OpenAsync().ConfigureAwait(false);
				connections.Add(connection);
			}

			using (var extraConnection = new MySqlConnection(csb.ConnectionString))
			{
				var stopwatch = Stopwatch.StartNew();
				Assert.Throws<MySqlException>(() => extraConnection.Open());
				stopwatch.Stop();
				Assert.InRange(stopwatch.ElapsedMilliseconds, 4500, 5500);
			}

			foreach (var connection in connections)
				connection.Dispose();
		}

		[Fact]
		public async Task CharacterSet()
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.Pooling = true;
			csb.MinimumPoolSize = 0;
			csb.MaximumPoolSize = 21; // use a uniqe pool size to create a unique connection string to force a unique pool to be created
#if BASELINE
			csb.CharacterSet = "utf8mb4";
#endif

			// verify that connection charset is the same when retrieving a connection from the pool
			await CheckCharacterSetAsync(csb.ConnectionString).ConfigureAwait(false);
			await CheckCharacterSetAsync(csb.ConnectionString).ConfigureAwait(false);
			await CheckCharacterSetAsync(csb.ConnectionString).ConfigureAwait(false);
		}

		private async Task CheckCharacterSetAsync(string connectionString)
		{
			using (var connection = new MySqlConnection(connectionString))
			{
				await connection.OpenAsync().ConfigureAwait(false);
				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = @"select @@character_set_client, @@character_set_connection";
					using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
					{
						Assert.True(await reader.ReadAsync().ConfigureAwait(false));
						Assert.Equal("utf8mb4", reader.GetString(0));
						Assert.Equal("utf8mb4", reader.GetString(1));
						Assert.False(await reader.ReadAsync().ConfigureAwait(false));
					}
				}
			}
		}
	}
}
