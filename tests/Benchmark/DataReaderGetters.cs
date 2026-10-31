using System;
using BenchmarkDotNet.Attributes;
using MySqlConnector;

namespace Benchmark;

/// <summary>
/// Measures the CPU cost of the <see cref="MySqlDataReader"/> typed getters by repeatedly reading the columns of a single row.
/// </summary>
public class DataReaderGetters
{
	[Params("Text", "Binary")]
	public string Protocol { get; set; }

	[GlobalSetup]
	public void GlobalSetup()
	{
		var connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING") ?? throw new InvalidOperationException("Set MYSQL_CONNECTION_STRING to the MySQL connection string.");
		var csb = new MySqlConnectionStringBuilder(connectionString) { IgnorePrepare = false };
		using (var connection = new MySqlConnection(csb.ConnectionString))
		{
			connection.Open();
			using var cmd = connection.CreateCommand();
			cmd.CommandText = @"
create schema if not exists benchmark;
drop table if exists benchmark.getters;
create table benchmark.getters(
	int_value int not null,
	bigint_value bigint not null,
	smallint_value smallint not null,
	bool_value bool not null,
	double_value double not null,
	float_value float not null,
	decimal_value decimal(18, 4) not null,
	datetime_value datetime(6) not null,
	time_value time(6) not null,
	guid_value char(36) not null,
	string_value varchar(100) not null,
	ubigint_value bigint unsigned not null
);
insert into benchmark.getters values(123456789, 1234567890123456789, 12345, true, 3.14159265358979, 2.5, 12345.6789,
	'2025-10-30 12:34:56.123456', '12:34:56.123456', '0b2c7f8e-5d3a-4c1b-9e6f-7a8b9c0d1e2f', 'hello, world', 18446744073709551615);";
			cmd.ExecuteNonQuery();
		}

		m_connection = new MySqlConnection(csb.ConnectionString);
		m_connection.Open();
		m_command = new MySqlCommand("select * from benchmark.getters;", m_connection);
		if (Protocol == "Binary")
			m_command.Prepare();
		m_reader = m_command.ExecuteReader();
		if (!m_reader.Read())
			throw new InvalidOperationException("Expected a row.");
	}

	[GlobalCleanup]
	public void GlobalCleanup()
	{
		m_reader.Dispose();
		m_command.Dispose();
		m_connection.Dispose();
		MySqlConnection.ClearAllPools();
	}

	[Benchmark] public object GetValue() => m_reader.GetValue(0);
	[Benchmark] public int GetInt32() => m_reader.GetInt32(0);
	[Benchmark] public long GetInt64() => m_reader.GetInt64(1);
	[Benchmark] public long GetInt64FromInt() => m_reader.GetInt64(0);
	[Benchmark] public short GetInt16() => m_reader.GetInt16(2);
	[Benchmark] public bool GetBoolean() => m_reader.GetBoolean(3);
	[Benchmark] public double GetDouble() => m_reader.GetDouble(4);
	[Benchmark] public float GetFloat() => m_reader.GetFloat(5);
	[Benchmark] public decimal GetDecimal() => m_reader.GetDecimal(6);
	[Benchmark] public DateTime GetDateTime() => m_reader.GetDateTime(7);
	[Benchmark] public TimeSpan GetTimeSpan() => m_reader.GetTimeSpan(8);
	[Benchmark] public Guid GetGuid() => m_reader.GetGuid(9);
	[Benchmark] public string GetString() => m_reader.GetString(10);
	[Benchmark] public ulong GetUInt64() => m_reader.GetUInt64(11);
	[Benchmark] public int GetFieldValueInt32() => m_reader.GetFieldValue<int>(0);
	[Benchmark] public long GetFieldValueInt64() => m_reader.GetFieldValue<long>(1);
	[Benchmark] public DateTime GetFieldValueDateTime() => m_reader.GetFieldValue<DateTime>(7);

	private MySqlConnection m_connection;
	private MySqlCommand m_command;
	private MySqlDataReader m_reader;
}
