using System;
using System.Collections.Generic;
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

		[Params(500, 5_000, 50_000, 100_000)]
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
				five int,
				six datetime,
				seven int
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
					Five = id + 1,
					Six = startDate.AddSeconds(id + 1),
					Seven = id + 2,
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

		[Benchmark(Baseline = true, Description = "MySqlBulkCopy.DataRows")]
		public async Task DataRowsCase()
		{
			DataTable table = new();
			table.Columns.Add("one", typeof(int));
			table.Columns.Add("two", typeof(string));
			table.Columns.Add("three", typeof(string));
			table.Columns.Add("four", typeof(DateTime));
			table.Columns.Add("five", typeof(int));
			table.Columns.Add("six", typeof(DateTime));
			table.Columns.Add("seven", typeof(int));

			var rows = new List<DataRow>();
			foreach (var rowData in collection)
			{
				var newRow = table.NewRow();
				newRow["one"] = rowData.One;
				newRow["two"] = rowData.Two;
				newRow["three"] = rowData.Three;
				newRow["four"] = rowData.Four;
				newRow["five"] = rowData.Five;
				newRow["six"] = rowData.Six;
				newRow["seven"] = rowData.Seven;

				rows.Add(newRow);
			}

			await using var connection = MySqlDataSource.CreateConnection();
			var bulkCopy = new MySqlBulkCopy(connection)
			{
				DestinationTableName = "mysql_bulk_import",
				BulkCopyTimeout = 0,
			};

			await bulkCopy.WriteToServerAsync(rows, 7);
		}

		[Benchmark(Description = "MySqlBulkCopy.IDataReader")]
		public async Task DataReaderCase()
		{
			await using var connection = MySqlDataSource.CreateConnection();

			var bc = new MySqlBulkCopy(connection)
			{
				DestinationTableName = "mysql_bulk_import",
				ColumnMappings =
				{
					new(0, "one"),
					new(1, "two"),
					new(2, "three"),
					new(3, "four"),
					new(4, "five"),
					new(5, "six"),
					new(6, "seven"),
				},
				BulkCopyTimeout = 0,
			};

			var reader = new DataReader(collection);
			await bc.WriteToServerAsync(reader);
		}

		[Benchmark(Description = "MySqlBulkImport")]
		public async Task MySqlBulkImportCase()
		{
			await using var connection = MySqlDataSource.CreateConnection();
			await using var import = new MySqlConnector.MySqlBulkImport(connection);
			import.StartImport("mysql_bulk_import", ["one", "two", "three", "four", "five", "six", "seven"], default);

			foreach (var row in collection)
			{
				import.WriteColumnValue(row.One);
				import.WriteColumnValue(row.Two);
				import.WriteColumnValue(row.Three);
				import.WriteColumnValue(row.Four);
				import.WriteColumnValue(row.Five);
				import.WriteColumnValue(row.Six);
				import.WriteColumnValue(row.Seven);

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

			public int Five { get; set; }

			public DateTime Six { get; set; }

			public int Seven { get; set; }
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

			public int FieldCount => 7;

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
				var value = collection[currentIndex];

				values[0] = value.One;
				values[1] = value.Two;
				values[2] = value.Three;
				values[3] = value.Four;
				values[4] = value.Five;
				values[5] = value.Six;
				values[6] = value.Seven;

				return 7;
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
