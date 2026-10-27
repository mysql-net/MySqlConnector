using System;
using System.Data.Common;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Benchmark;

class Program
{
	static void Main(string[] args)
	{
		var customConfig = ManualConfig
			.Create(DefaultConfig.Instance)
			.AddValidator(JitOptimizationsValidator.FailOnError)
			.AddDiagnoser(MemoryDiagnoser.Default)
			.AddColumn(StatisticColumn.AllStatistics)
			.AddJob(Job.Default.WithRuntime(CoreRuntime.Core11_0))
			.AddExporter(DefaultExporters.Csv);

		// pass through command-line arguments so the run can be narrowed with --filter, --job, --runtimes, etc.
		var summary = BenchmarkRunner.Run<MySqlClient>(customConfig, args);
		Console.WriteLine(summary);
	}
}

public class MySqlClient
{
	[Params("MySql.Data", "MySqlConnector")]
	public string Library { get; set; }

	[GlobalSetup]
	public void GlobalSetup()
	{
		var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") ?? throw new InvalidOperationException("Set MYSQL_CONNECTION_STRING to the MySQL connection string.");
		using (var connection = new MySqlConnector.MySqlConnection(connectionString))
		{
			connection.Open();
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = @"
create schema if not exists benchmark;

drop table if exists benchmark.integers;
create table benchmark.integers (value int not null primary key);
insert into benchmark.integers(value) values (0),(1),(2),(3),(4),(5),(6),(7),(8),(9);

drop table if exists benchmark.blobs;
create table benchmark.blobs(
rowid integer not null primary key auto_increment,
`Blob` longblob null
);
insert into benchmark.blobs(`Blob`) values(null), (@Blob1), (@Blob2);";

				// larger blobs make the tests run much slower
				AddBlobParameter(cmd, "@Blob1", 75000);
				AddBlobParameter(cmd, "@Blob2", 150000);

				cmd.ExecuteNonQuery();
			}
		}

		connectionString += ";database=benchmark";

		// network latency to a cloud server can differ by hundreds of microseconds between TCP connections (and stays stable for the
		// lifetime of each connection), so rotate through several connections to measure the library instead of one network path
		m_connections = new DbConnection[c_connectionCount];
		for (var i = 0; i < m_connections.Length; i++)
		{
			m_connections[i] = Library == "MySqlConnector" ? new MySqlConnector.MySqlConnection(connectionString) : new MySql.Data.MySqlClient.MySqlConnection(connectionString);
			m_connections[i].Open();
		}
	}

	[GlobalCleanup]
	public void GlobalCleanup()
	{
		foreach (var connection in m_connections)
			connection.Dispose();
		MySqlConnector.MySqlConnection.ClearAllPools();
		MySql.Data.MySqlClient.MySqlConnection.ClearAllPools();
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

	[Benchmark]
	public async Task OpenFromPoolAsync()
	{
		var connection = GetNextConnection();
		connection.Close();
		await connection.OpenAsync();
	}

	[Benchmark]
	public void OpenFromPoolSync()
	{
		var connection = GetNextConnection();
		connection.Close();
		connection.Open();
	}

	[Benchmark]
	public async Task ExecuteScalarAsync()
	{
		using var cmd = GetNextConnection().CreateCommand();
		cmd.CommandText = c_executeScalarSql;
		await cmd.ExecuteScalarAsync();
	}

	[Benchmark]
	public void ExecuteScalarSync()
	{
		using var cmd = GetNextConnection().CreateCommand();
		cmd.CommandText = c_executeScalarSql;
		cmd.ExecuteScalar();
	}

	private const string c_executeScalarSql = "select max(value) from integers;";

	[Benchmark] public Task ReadBlobsAsync() => ReadAllRowsAsync(c_readBlobsSql);
	[Benchmark] public void ReadBlobsSync() => ReadAllRowsSync(c_readBlobsSql);

	private const string c_readBlobsSql = "select `Blob` from blobs;";

	[Benchmark] public Task ManyRowsAsync() => ReadAllRowsAsync(c_manyRowsSql);
	[Benchmark] public void ManyRowsSync() => ReadAllRowsSync(c_manyRowsSql);

	private const string c_manyRowsSql = "select * from integers a join integers b join integers c;";

	private async Task<int> ReadAllRowsAsync(string sql)
	{
		int total = 0;
		using (var cmd = GetNextConnection().CreateCommand())
		{
			cmd.CommandText = sql;
			using var reader = await cmd.ExecuteReaderAsync();
			do
			{
				while (await reader.ReadAsync())
				{
					if (reader.FieldCount > 1)
						total += reader.GetInt32(1);
				}
			} while (await reader.NextResultAsync());
		}
		return total;
	}

	private int ReadAllRowsSync(string sql)
	{
		int total = 0;
		using (var cmd = GetNextConnection().CreateCommand())
		{
			cmd.CommandText = sql;
			using var reader = cmd.ExecuteReader();
			do
			{
				while (reader.Read())
				{
					if (reader.FieldCount > 1)
						total += reader.GetInt32(1);
				}
			} while (reader.NextResult());
		}
		return total;
	}

	private DbConnection GetNextConnection()
	{
		var connection = m_connections[m_nextConnection];
		m_nextConnection = (m_nextConnection + 1) % m_connections.Length;
		return connection;
	}

	private const int c_connectionCount = 8;

	private DbConnection[] m_connections;
	private int m_nextConnection;
}
