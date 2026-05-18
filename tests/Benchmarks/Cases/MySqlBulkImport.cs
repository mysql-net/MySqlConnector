using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MySqlConnector;

namespace Benchmarks.Cases
{
	[MemoryDiagnoser]
	[ThreadingDiagnoser]
	[SimpleJob(RuntimeMoniker.Net10_0)]
	public class MySqlBulkImport : BenchmarkBase
    {
		private RowData[] collection;

		[Params(500, 5_000, 50_000, 500_000)]
#pragma warning disable SA1401 // Fields should be private
		public int RowsToInsert;
#pragma warning restore SA1401 // Fields should be private

		[GlobalSetup]
		public async Task GlobalSetup()
        {
            await OneTimeSetUp();
		}

		[GlobalCleanup]
		public async Task GlobalCleanup()
        {
            await OneTimeTearDown();
        }

		[IterationSetup]
		public void IterationSetup()
        {
			var sql = $@"
			create table mysql_bulk_import
			(
				one int primary key,
				ignore_one int,
				two varchar(200),
				ignore_two varchar(200),
				three varchar(200),
				four datetime,
				five blob
			)
			CHARACTER SET = UTF8;";

			using var connection = MySqlDataSource.OpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = sql;

			command.ExecuteNonQuery();

			collection = new RowData[RowsToInsert];
			var startDate = DateTime.UtcNow;
			for (var i = 0; i < collection.Length; i++)
			{
				var id = i + 1;
				collection[i] = new RowData
				{
					One = id,
					Two = $"two-{id}",
					Three = $"three-{id}",
					Four = startDate.AddSeconds(id),
				};
			}
		}

		[IterationCleanup]
		public void IterationCleanup()
        {
			var sql = "drop table if exists mysql_bulk_import;";

			using var connection = MySqlDataSource.OpenConnection();
			using var command = connection.CreateCommand();
			command.CommandText = sql;

			command.ExecuteNonQuery();
		}

		[Benchmark(Baseline = true, Description = "MySqlBulkCopy.IDataReader")]
		public async Task DataReaderCase()
		{
			using var connection = MySqlDataSource.CreateConnection();

			var bc = new MySqlBulkCopy(connection)
			{
				DestinationTableName = "mysql_bulk_import",
				ColumnMappings =
				{
					new(0, "one"),
					new(1, "two"),
					new(2, "three"),
					new(3, "four"),
				},
				BulkCopyTimeout = 0,
			};

			var reader = new DataReader(collection);
			await bc.WriteToServerAsync(reader);
		}

		[Benchmark(Description = "MySqlBulkImport2")]
		public async Task MySqlBulkImportCase()
		{
			using var connection = MySqlDataSource.CreateConnection();
			await using var import = new MySqlConnector.MySqlBulkImport2(connection, 1048575);
			import.StartImport("mysql_bulk_import", ["one", "two", "three", "four"], default);

			foreach (var row in collection)
			{
				import.WriteColumnValue(row.One);
				import.WriteColumnValue(row.Two);
				import.WriteColumnValue(row.Three);
				import.WriteColumnValue(row.Four);
				import.EndRow();
			}

			await import.WaitFinishImportAsync();
		}

		public class RowData
		{
			public int One { get; set; }

			public string Two { get; set; }

			public string Three { get; set; }

			public DateTime Four { get; set; }
		}

		public class DataReader : IDataReader
		{
			private readonly RowData[] collection;
			private int currentIndex;

			public DataReader(RowData[] collection)
			{
				this.collection = collection;
			}

			public object this[int i] => throw new NotImplementedException();

			public object this[string name] => throw new NotImplementedException();

			public int Depth => throw new NotImplementedException();

			public bool IsClosed => throw new NotImplementedException();

			public int RecordsAffected => throw new NotImplementedException();

			public int FieldCount => 4;

			public void Close() => throw new NotImplementedException();
			public void Dispose() => throw new NotImplementedException();
			public bool GetBoolean(int i) => throw new NotImplementedException();
			public byte GetByte(int i) => throw new NotImplementedException();
			public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => throw new NotImplementedException();
			public char GetChar(int i) => throw new NotImplementedException();
			public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => throw new NotImplementedException();
			public IDataReader GetData(int i) => throw new NotImplementedException();
			public string GetDataTypeName(int i) => throw new NotImplementedException();
			public DateTime GetDateTime(int i) => throw new NotImplementedException();
			public decimal GetDecimal(int i) => throw new NotImplementedException();
			public double GetDouble(int i) => throw new NotImplementedException();
			[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
			public Type GetFieldType(int i) => throw new NotImplementedException();
			public float GetFloat(int i) => throw new NotImplementedException();
			public Guid GetGuid(int i) => throw new NotImplementedException();
			public short GetInt16(int i) => throw new NotImplementedException();
			public int GetInt32(int i) => throw new NotImplementedException();
			public long GetInt64(int i) => throw new NotImplementedException();
			public string GetName(int i) => throw new NotImplementedException();
			public int GetOrdinal(string name) => throw new NotImplementedException();
			public DataTable GetSchemaTable() => throw new NotImplementedException();
			public string GetString(int i) => throw new NotImplementedException();
			public object GetValue(int i) => throw new NotImplementedException();

			public int GetValues(object[] values)
			{
				values[0] = collection[currentIndex].One;
				values[1] = collection[currentIndex].Two;
				values[2] = collection[currentIndex].Three;
				values[3] = collection[currentIndex].Four;

				return 4;
			}

			public bool IsDBNull(int i) => throw new NotImplementedException();
			public bool NextResult() => throw new NotImplementedException();

			public bool Read()
			{
				if (currentIndex >= collection.Length - 1)
				{
					return false;
				}

				currentIndex++;
				return true;
			}
		}
	}
}
