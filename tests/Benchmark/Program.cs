extern alias MySqlData;
extern alias MySqlConnector;
using System;
using System.Data.Common;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using OldMySqlConnection = MySqlData.MySql.Data.MySqlClient.MySqlConnection;
using NewMySqlConnection = MySqlConnector.MySql.Data.MySqlClient.MySqlConnection;

namespace Benchmark
{
	class Program
	{
		static void Main()
		{
			var customConfig = ManualConfig
				.Create(DefaultConfig.Instance)
				.With(JitOptimizationsValidator.FailOnError)
				.With(MemoryDiagnoser.Default)
				.With(StatisticColumn.AllStatistics)
				.With(DefaultExporters.Csv);

			var summary = BenchmarkRunner.Run<MySqlClient>(customConfig);
			Console.WriteLine(summary);
		}
	}

	public class MySqlClient
	{
		[Setup]
		public void Setup()
		{
			using (var oldConnection = new OldMySqlConnection(s_connectionString))
			{
				oldConnection.Open();
				RunSetupSql(oldConnection);
			}

			using (var newConnection = new NewMySqlConnection(s_connectionString))
			{
				newConnection.Open();
				RunSetupSql(newConnection);
			}

			s_connectionString += ";database=benchmark";

			m_oldConnection = new OldMySqlConnection(s_connectionString);
			m_oldConnection.Open();

			m_newConnection = new NewMySqlConnection(s_connectionString);
			m_newConnection.Open();
		}

		private void RunSetupSql(DbConnection connection)
		{
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = @"
create schema if not exists benchmark;

drop table if exists benchmark.integers;
create table benchmark.integers (value int not null primary key);
insert into benchmark.integers(value) values (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20);

drop table if exists benchmark.blobs;
create table benchmark.blobs(
  rowid integer not null primary key auto_increment,
  `Blob` longblob null
);
insert into benchmark.blobs(`Blob`) values(null), (@Blob1), (@Blob2);";

				// larger blobs make the tests run much slower
				AddBlobParameter(cmd, "@Blob1", 100000);
				AddBlobParameter(cmd, "@Blob2", 1000000);

				cmd.ExecuteNonQuery();
			}
		}

		private static void AddBlobParameter(DbCommand command, string name, int size)
		{
			var parameter = command.CreateParameter();
			parameter.ParameterName = name;

			var random = new Random(size);
			var value = new byte[size];
			random.NextBytes(value);
			parameter.Value = value;

			command.Parameters.Add(parameter);
		}

		[Benchmark] public Task OpenFromPoolOldAsync() => OpenFromPoolAsync(m_oldConnection);
		[Benchmark] public void OpenFromPoolOldSync() => OpenFromPoolSync(m_oldConnection);
		[Benchmark] public Task OpenFromPoolNewAsync() => OpenFromPoolAsync(m_newConnection);
		[Benchmark] public void OpenFromPoolNewSync() => OpenFromPoolSync(m_newConnection);

		private static async Task OpenFromPoolAsync(DbConnection connection)
		{
			connection.Close();
			await connection.OpenAsync();
		}

		private static void OpenFromPoolSync(DbConnection connection)
		{
			connection.Close();
			connection.Open();
		}

		[Benchmark] public Task ExecuteScalarOldAsync() => ExecuteScalarAsync(m_oldConnection);
		[Benchmark] public void ExecuteScalarOldSync() => ExecuteScalarSync(m_oldConnection);
		[Benchmark] public Task ExecuteScalarNewAsync() => ExecuteScalarAsync(m_newConnection);
		[Benchmark] public void ExecuteScalarNewSync() => ExecuteScalarSync(m_newConnection);

		private static async Task ExecuteScalarAsync(DbConnection connection)
		{
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = c_executeScalarSql;
				await cmd.ExecuteScalarAsync();
			}
		}

		private static void ExecuteScalarSync(DbConnection connection)
		{
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = c_executeScalarSql;
				cmd.ExecuteScalar();
			}
		}

		private const string c_executeScalarSql = "select max(value) from integers;";

		[Benchmark] public Task ReadBlobsOldAsync() => ReadAllRowsAsync(m_oldConnection, c_readBlobsSql);
		[Benchmark] public void ReadBlobsOldSync() => ReadAllRowsSync(m_oldConnection, c_readBlobsSql);
		[Benchmark] public Task ReadBlobsNewAsync() => ReadAllRowsAsync(m_newConnection, c_readBlobsSql);
		[Benchmark] public void ReadBlobsNewSync() => ReadAllRowsSync(m_newConnection, c_readBlobsSql);

		private const string c_readBlobsSql = "select `Blob` from blobs;";

		[Benchmark] public Task ManyRowsOldAsync() => ReadAllRowsAsync(m_oldConnection, c_manyRowsSql);
		[Benchmark] public void ManyRowsOldSync() => ReadAllRowsSync(m_oldConnection, c_manyRowsSql);
		[Benchmark] public Task ManyRowsNewAsync() => ReadAllRowsAsync(m_newConnection, c_manyRowsSql);
		[Benchmark] public void ManyRowsNewSync() => ReadAllRowsSync(m_newConnection, c_manyRowsSql);

		private const string c_manyRowsSql = "select * from integers a join integers b; select * from integers a join integers b join integers c;";

		private static async Task ReadAllRowsAsync(DbConnection connection, string sql)
		{
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = sql;
				using (var reader = await cmd.ExecuteReaderAsync())
				{
					do
					{
						while (await reader.ReadAsync())
						{
						}
					} while (await reader.NextResultAsync());
				}
			}
		}

		private static void ReadAllRowsSync(DbConnection connection, string sql)
		{
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = sql;
				using (var reader = cmd.ExecuteReader())
				{
					do
					{
						while (reader.Read())
						{
						}
					} while (reader.NextResult());
				}
			}
		}

		// TODO: move to config file
		// NOTE: Without "Connection Reset=true" here, Connector/NET doesn't reset the connection, which is buggy but 8x faster
		//       With "Connection Reset=true" here, Connector/NET is affected by https://bugs.mysql.com/bug.php?id=80030 and is 48x slower
		//       We opt for the incorrect-but-faster implementation here so the benchmarks don't take as long (and give something to aim for)
		static string s_connectionString = "server=127.0.0.1;user id=mysqltest;password='test;key=\"val';port=3306;ssl mode=none;Use Affected Rows=true";

		private OldMySqlConnection m_oldConnection;
		private NewMySqlConnection m_newConnection;
	}
}
