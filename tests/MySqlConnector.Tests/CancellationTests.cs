using System.Net.Sockets;

namespace MySqlConnector.Tests;

public class CancellationTests : IDisposable
{
	public CancellationTests()
	{
		m_server = new();
		m_server.Start();

		m_csb = new()
		{
			Server = "localhost",
			Port = (uint) m_server.Port,
		};
	}

	public void Dispose() => m_server.Stop();

	//// NOTE: Multiple nested classes in order to force tests to run in parallel against each other

	public class CancelWithCommandTimeout : CancellationTests
	{
		[SkipCITheory]
		[MemberData(nameof(GetSyncMethodSteps))]
		public void Execute(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 0, 4000, {step}, 0;";
			var stopwatch = Stopwatch.StartNew();
			var ex = Assert.Throws<MySqlException>(() => s_executeMethods[method](command));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			Assert.Equal(MySqlErrorCode.CommandTimeoutExpired, ex.ErrorCode);
			var inner = Assert.IsType<MySqlException>(ex.InnerException);
			Assert.Equal(MySqlErrorCode.QueryInterrupted, inner.ErrorCode);

			// connection should still be usable
			Assert.Equal(ConnectionState.Open, connection.State);
			command.CommandText = "SELECT 1;";
			Assert.Equal(1, command.ExecuteScalar());
		}

		[SkipCITheory]
		[MemberData(nameof(GetAsyncMethodSteps))]
		public async Task ExecuteAsync(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 0, 4000, {step}, 0;";
			var stopwatch = Stopwatch.StartNew();
			var ex = await Assert.ThrowsAsync<MySqlException>(async () => await s_executeAsyncMethods[method](command, default));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			Assert.Equal(MySqlErrorCode.CommandTimeoutExpired, ex.ErrorCode);
			var inner = Assert.IsType<MySqlException>(ex.InnerException);
			Assert.Equal(MySqlErrorCode.QueryInterrupted, inner.ErrorCode);

			// connection should still be usable
			Assert.Equal(ConnectionState.Open, connection.State);
			command.CommandText = "SELECT 1;";
			Assert.Equal(1, command.ExecuteScalar());
		}
	}

	public class CancelBufferedWithCommandTimeout : CancellationTests
	{
		[SkipCITheory]
		[MemberData(nameof(GetSyncMethodSteps))]
		public void Execute(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 0, 4000, {step}, 2;";
			var stopwatch = Stopwatch.StartNew();
			var ex = Assert.Throws<MySqlException>(() => s_executeMethods[method](command));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			Assert.Equal(MySqlErrorCode.CommandTimeoutExpired, ex.ErrorCode);
			var inner = Assert.IsType<MySqlException>(ex.InnerException);
			Assert.Equal(MySqlErrorCode.QueryInterrupted, inner.ErrorCode);

			// connection should still be usable
			Assert.Equal(ConnectionState.Open, connection.State);
			command.CommandText = "SELECT 1;";
			Assert.Equal(1, command.ExecuteScalar());
		}

		[SkipCITheory]
		[MemberData(nameof(GetAsyncMethodSteps))]
		public async Task ExecuteAsync(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 0, 4000, {step}, 2;";
			var stopwatch = Stopwatch.StartNew();
			var ex = await Assert.ThrowsAsync<MySqlException>(async () => await s_executeAsyncMethods[method](command, default));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			Assert.Equal(MySqlErrorCode.CommandTimeoutExpired, ex.ErrorCode);
			var inner = Assert.IsType<MySqlException>(ex.InnerException);
			Assert.Equal(MySqlErrorCode.QueryInterrupted, inner.ErrorCode);

			// connection should still be usable
			Assert.Equal(ConnectionState.Open, connection.State);
			command.CommandText = "SELECT 1;";
			Assert.Equal(1, command.ExecuteScalar());
		}
	}

	public class CancelWithCancel : CancellationTests
	{
		[SkipCITheory]
		[MemberData(nameof(GetSyncMethodSteps))]
		public void Execute(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 10;
			command.CommandText = $"SELECT 0, 4000, {step}, 0;";
			var task = Task.Run(async () =>
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
				command.Cancel();
			});
			var stopwatch = Stopwatch.StartNew();
			var ex = Assert.Throws<MySqlException>(() => s_executeMethods[method](command));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			Assert.Equal(MySqlErrorCode.QueryInterrupted, ex.ErrorCode);
			Assert.Null(ex.InnerException);
			task.Wait();

			// connection should still be usable
			Assert.Equal(ConnectionState.Open, connection.State);
			command.CommandText = "SELECT 1;";
			Assert.Equal(1, command.ExecuteScalar());
		}

		[SkipCITheory]
		[MemberData(nameof(GetAsyncMethodSteps))]
		public async Task ExecuteAsync(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 10;
			command.CommandText = $"SELECT 0, 4000, {step}, 0;";
			var task = Task.Run(async () =>
			{
				await Task.Delay(TimeSpan.FromSeconds(1));
				command.Cancel();
			});
			var stopwatch = Stopwatch.StartNew();
			var ex = await Assert.ThrowsAsync<MySqlException>(async () => await s_executeAsyncMethods[method](command, default));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			Assert.Equal(MySqlErrorCode.QueryInterrupted, ex.ErrorCode);
			Assert.Null(ex.InnerException);
			task.Wait();

			// connection should still be usable
			Assert.Equal(ConnectionState.Open, connection.State);
			command.CommandText = "SELECT 1;";
			Assert.Equal(1, command.ExecuteScalar());
		}
	}

	public class CancelExecuteXAsyncWithCancellationToken : CancellationTests
	{
		[SkipCITheory]
		[MemberData(nameof(GetAsyncMethodSteps))]
		public async Task Test(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 0;
			command.CommandText = $"SELECT 0, 4000, {step}, 0;";
			using var source = new CancellationTokenSource(TimeSpan.FromSeconds(1));
			var stopwatch = Stopwatch.StartNew();
			var ex = await Assert.ThrowsAsync<OperationCanceledException>(async () => await s_executeAsyncMethods[method](command, source.Token));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			var mySqlException = Assert.IsType<MySqlException>(ex.InnerException);
			Assert.Equal(MySqlErrorCode.QueryInterrupted, mySqlException.ErrorCode);

			// connection should still be usable
			Assert.Equal(ConnectionState.Open, connection.State);
			command.CommandText = "SELECT 1;";
			Assert.Equal(1, command.ExecuteScalar());
		}
	}

	public class ExecuteXBeforeCommandTimeoutExpires : CancellationTests
	{
		[SkipCITheory]
		[MemberData(nameof(GetSyncMethodSteps))]
		public void Test(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 42, 100, {step}, 0;";
			var stopwatch = Stopwatch.StartNew();
			var result = s_executeMethods[method](command);
			if (method == 1)
				Assert.Equal(0, result); // ExecuteNonQuery
			else
				Assert.Equal(42, result);
			Assert.InRange(stopwatch.ElapsedMilliseconds, 50, 250);
		}
	}

	public class ExecuteXAsyncBeforeCancellationTokenCancels : CancellationTests
	{
		[SkipCITheory]
		[MemberData(nameof(GetAsyncMethodSteps))]
		public async Task Test(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 0;
			command.CommandText = $"SELECT 42, 100, {step}, 0;";
			using var source = new CancellationTokenSource(TimeSpan.FromSeconds(1));
			var stopwatch = Stopwatch.StartNew();
			var result = await s_executeAsyncMethods[method](command, source.Token);
			if (method == 1)
				Assert.Equal(0, result); // ExecuteNonQuery
			else
				Assert.Equal(42, result);
			Assert.InRange(stopwatch.ElapsedMilliseconds, 50, 250);
		}
	}

	public class ExecuteXWithLongAggregateTime : CancellationTests
	{
		[SkipCITheory]
		[InlineData(0)]
		[InlineData(1)]
		public void Timeout(int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 0, 100, -1, 0;";
			var stopwatch = Stopwatch.StartNew();
			var ex = Assert.Throws<MySqlException>(() => s_executeMethods[method](command));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			Assert.Equal(MySqlErrorCode.CommandTimeoutExpired, ex.ErrorCode);
			var inner = Assert.IsType<MySqlException>(ex.InnerException);
			Assert.Equal(MySqlErrorCode.QueryInterrupted, inner.ErrorCode);
		}

		[SkipCITheory]
		[InlineData(2)]
		public void NoTimeout(int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 42, 100, -1, 0;";
			var stopwatch = Stopwatch.StartNew();
			var result = s_executeMethods[method](command);
			Assert.Equal(42, result);
			Assert.InRange(stopwatch.ElapsedMilliseconds, 1100, 1500);
		}
	}

	public class ExecuteXAsyncWithLongAggregateTime : CancellationTests
	{
		[SkipCITheory]
		[InlineData(0)]
		[InlineData(1)]
		public async Task Timeout(int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 0, 100, -1, 0;";
			var stopwatch = Stopwatch.StartNew();
			var ex = await Assert.ThrowsAsync<MySqlException>(async () => await s_executeAsyncMethods[method](command, default));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			Assert.Equal(MySqlErrorCode.CommandTimeoutExpired, ex.ErrorCode);
			var inner = Assert.IsType<MySqlException>(ex.InnerException);
			Assert.Equal(MySqlErrorCode.QueryInterrupted, inner.ErrorCode);
		}

		[SkipCITheory]
		[InlineData(2)]
		public async Task NoTimeout(int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 42, 100, -1, 0;";
			var stopwatch = Stopwatch.StartNew();
			var result = await s_executeAsyncMethods[method](command, default);
			Assert.Equal(42, result);
			Assert.InRange(stopwatch.ElapsedMilliseconds, 1100, 1500);
		}
	}

	public class ExecuteXTimeout : CancellationTests
	{
		[SkipCITheory]
		[MemberData(nameof(GetSyncMethodSteps))]
		public void Test(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 0, 10000, {step}, 1;";
			var stopwatch = Stopwatch.StartNew();
			var ex = Assert.Throws<MySqlException>(() => s_executeMethods[method](command));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 2900, 3500);
			Assert.Equal(MySqlErrorCode.CommandTimeoutExpired, ex.ErrorCode);
			Assert.Null(ex.InnerException);

			// connection is unusable
			Assert.Equal(ConnectionState.Broken, connection.State);
		}
	}

	public class ExecuteXAsyncTimeout : CancellationTests
	{
		[SkipCITheory]
		[MemberData(nameof(GetAsyncMethodSteps))]
		public async Task Test(int step, int method)
		{
			using var connection = new MySqlConnection(m_csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 0, 10000, {step}, 1;";
			var stopwatch = Stopwatch.StartNew();
			var ex = await Assert.ThrowsAsync<MySqlException>(async () => await s_executeAsyncMethods[method](command, default));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 2900, 3500);
			Assert.Equal(MySqlErrorCode.CommandTimeoutExpired, ex.ErrorCode);
			Assert.IsType<SocketException>(ex.InnerException);

			// connection is unusable
			Assert.Equal(ConnectionState.Broken, connection.State);
		}
	}

	public class WithCancellationTimeoutIsNegativeOne : CancellationTests
	{
		[SkipCITheory]
		[MemberData(nameof(GetSyncMethodSteps))]
		public void Execute(int step, int method)
		{
			var csb = new MySqlConnectionStringBuilder(m_csb.ConnectionString) { CancellationTimeout = -1 };
			using var connection = new MySqlConnection(csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 0, 10000, {step}, 1;";
			var stopwatch = Stopwatch.StartNew();
			var ex = Assert.Throws<MySqlException>(() => s_executeMethods[method](command));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			Assert.Equal(MySqlErrorCode.CommandTimeoutExpired, ex.ErrorCode);
			Assert.Null(ex.InnerException);

			// connection is unusable
			Assert.Equal(ConnectionState.Broken, connection.State);
		}

		[SkipCITheory]
		[MemberData(nameof(GetAsyncMethodSteps))]
		public async Task ExecuteAsync(int step, int method)
		{
			var csb = new MySqlConnectionStringBuilder(m_csb.ConnectionString) { CancellationTimeout = -1 };
			using var connection = new MySqlConnection(csb.ConnectionString);
			connection.Open();
			using var command = connection.CreateCommand();
			command.CommandTimeout = 1;
			command.CommandText = $"SELECT 0, 10000, {step}, 1;";
			var stopwatch = Stopwatch.StartNew();
			var ex = await Assert.ThrowsAsync<MySqlException>(async () => await s_executeAsyncMethods[method](command, default));
			Assert.InRange(stopwatch.ElapsedMilliseconds, 900, 1500);
			Assert.Equal(MySqlErrorCode.CommandTimeoutExpired, ex.ErrorCode);
			Assert.IsType<SocketException>(ex.InnerException);

			// connection is unusable
			Assert.Equal(ConnectionState.Broken, connection.State);
		}
	}

	public static IEnumerable<object[]> GetSyncMethodSteps()
	{
		for (var step = 1; step <= 12; step++)
		{
			for (var method = 0; method < s_executeMethods.Length; method++)
			{
				yield return new object[] { step, method };
			}
		}
	}

	public static IEnumerable<object[]> GetAsyncMethodSteps()
	{
		for (var step = 1; step <= 12; step++)
		{
			for (var method = 0; method < s_executeAsyncMethods.Length; method++)
			{
				yield return new object[] { step, method };
			}
		}
	}

	private static readonly Func<MySqlCommand, int>[] s_executeMethods = [ExecuteScalar, ExecuteNonQuery, ExecuteReader];
	private static readonly Func<MySqlCommand, CancellationToken, Task<int>>[] s_executeAsyncMethods = [ExecuteScalarAsync, ExecuteNonQueryAsync, ExecuteReaderAsync];

	private static int ExecuteScalar(MySqlCommand command) => (int) command.ExecuteScalar();
	private static async Task<int> ExecuteScalarAsync(MySqlCommand command, CancellationToken token) => (int) await command.ExecuteScalarAsync(token);

	private static int ExecuteNonQuery(MySqlCommand command)
	{
		command.ExecuteNonQuery();
		return 0;
	}

	private static async Task<int> ExecuteNonQueryAsync(MySqlCommand command, CancellationToken token)
	{
		await command.ExecuteNonQueryAsync(token);
		return 0;
	}

	private static int ExecuteReader(MySqlCommand command)
	{
		using var reader = command.ExecuteReader();
		int? value = null;
		do
		{
			while (reader.Read())
				value ??= reader.GetInt32(0);
		} while (reader.NextResult());
		return value.Value;
	}
	private static async Task<int> ExecuteReaderAsync(MySqlCommand command, CancellationToken token)
	{
		using var reader = await command.ExecuteReaderAsync(token);
		int? value = null;
		do
		{
			while (await reader.ReadAsync(token))
				value ??= reader.GetInt32(0);
		} while (await reader.NextResultAsync(token));
		return value.Value;
	}

	private readonly FakeMySqlServer m_server;
	private readonly MySqlConnectionStringBuilder m_csb;
}
