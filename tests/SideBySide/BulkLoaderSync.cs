using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using Dapper;
#if BASELINE
using MySql.Data.MySqlClient;
#else
using MySqlConnector;
#endif
using Xunit;
using Xunit.Sdk;

namespace SideBySide
{
	[Collection("BulkLoaderCollection")]
	public class BulkLoaderSync : IClassFixture<DatabaseFixture>
	{
		public BulkLoaderSync(DatabaseFixture database)
		{
			m_testTable = "BulkLoaderSyncTest";
			var initializeTable = $@"
				drop table if exists {m_testTable};
				create table {m_testTable}
				(
					one int primary key
					, ignore_one int
					, two varchar(200)
					, ignore_two varchar(200)
					, three varchar(200)
					, four datetime
					, five blob
				) CHARACTER SET = UTF8;";
			database.Connection.Execute(initializeTable);

			m_memoryStreamBytes = System.Text.Encoding.UTF8.GetBytes(@"1,'two-1','three-1'
2,'two-2','three-2'
3,'two-3','three-3'
4,'two-4','three-4'
5,'two-5','three-5'
");
		}

#if !BASELINE
		[Fact]
		public void FileNameAndSourceStream()
		{
			using var connection = new MySqlConnection(GetConnectionString());
			connection.Open();
			var bl = new MySqlBulkLoader(connection)
			{
				FileName = "test.dat",
				SourceStream = new MemoryStream(),
				TableName = m_testTable,
			};
			Assert.Throws<InvalidOperationException>(() => bl.Load());
		}
#endif

		[SkippableFact(ConfigSettings.TsvFile)]
		public void BulkLoadTsvFile()
		{
			using var connection = new MySqlConnection(GetConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			bl.FileName = AppConfig.MySqlBulkLoaderTsvFile;
			bl.TableName = m_testTable;
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.Local = false;
			int rowCount = bl.Load();
			Assert.Equal(20, rowCount);
		}

		[SkippableFact(ConfigSettings.LocalTsvFile)]
		public void BulkLoadLocalTsvFile()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			bl.FileName = AppConfig.MySqlBulkLoaderLocalTsvFile;
			bl.TableName = m_testTable;
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.Local = true;
			int rowCount = bl.Load();
			Assert.Equal(20, rowCount);
		}

		[SkippableFact(ConfigSettings.CsvFile)]
		public void BulkLoadCsvFile()
		{
			using var connection = new MySqlConnection(GetConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			bl.FileName = AppConfig.MySqlBulkLoaderCsvFile;
			bl.TableName = m_testTable;
			bl.CharacterSet = "UTF8";
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.FieldTerminator = ",";
			bl.FieldQuotationCharacter = '"';
			bl.FieldQuotationOptional = true;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.Local = false;
			int rowCount = bl.Load();
			Assert.Equal(20, rowCount);
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public void BulkLoadLocalCsvFile()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			bl.FileName = AppConfig.MySqlBulkLoaderLocalCsvFile;
			bl.TableName = m_testTable;
			bl.CharacterSet = "UTF8";
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.FieldTerminator = ",";
			bl.FieldQuotationCharacter = '"';
			bl.FieldQuotationOptional = true;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.Local = true;
			int rowCount = bl.Load();
			Assert.Equal(20, rowCount);
		}

		[Fact]
		public void BulkLoadCsvFileNotFound()
		{
			using var connection = new MySqlConnection(GetConnectionString());
			connection.Open();
			var secureFilePath = connection.ExecuteScalar<string>(@"select @@global.secure_file_priv;");
			if (string.IsNullOrEmpty(secureFilePath) || secureFilePath == "NULL")
				return;

			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			bl.FileName = Path.Combine(secureFilePath, AppConfig.MySqlBulkLoaderCsvFile + "-junk");
			bl.TableName = m_testTable;
			bl.CharacterSet = "UTF8";
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.FieldTerminator = ",";
			bl.FieldQuotationCharacter = '"';
			bl.FieldQuotationOptional = true;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.Local = false;
			try
			{
				int rowCount = bl.Load();
			}
			catch (Exception exception)
			{
				while (exception.InnerException is not null)
					exception = exception.InnerException;

				if (exception is not FileNotFoundException)
				{
					try
					{
						Assert.Contains("Errcode: 2 ", exception.Message, StringComparison.OrdinalIgnoreCase);
					}
					catch (ContainsException)
					{
						Assert.Contains("OS errno 2 ", exception.Message, StringComparison.OrdinalIgnoreCase);
					}
					Assert.Contains("No such file or directory", exception.Message);
				}
			}
		}

		[Fact]
		public void BulkLoadLocalCsvFileNotFound()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			bl.Timeout = 3; //Set a short timeout for this test because the file not found exception takes a long time otherwise, the timeout does not change the result
			bl.FileName = AppConfig.MySqlBulkLoaderLocalCsvFile + "-junk";
			bl.TableName = m_testTable;
			bl.CharacterSet = "UTF8";
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.FieldTerminator = ",";
			bl.FieldQuotationCharacter = '"';
			bl.FieldQuotationOptional = true;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.Local = true;
			try
			{
				int rowCount = bl.Load();
			}
			catch (MySqlException mySqlException)
			{
				while (mySqlException.InnerException is not null)
				{
					if (mySqlException.InnerException is MySqlException innerException)
					{
						mySqlException = innerException;
					}
					else
					{
						Assert.IsType<System.IO.FileNotFoundException>(mySqlException.InnerException);
						break;
					}
				}
				if (mySqlException.InnerException is null)
				{
					Assert.IsType<System.IO.FileNotFoundException>(mySqlException);
				}
			}
			catch (Exception exception)
			{
				//We know that the exception is not a MySqlException, just use the assertion to fail the test
				Assert.IsType<MySqlException>(exception);
			}
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public void BulkLoadLocalCsvFileInTransactionWithCommit()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			using (var transaction = connection.BeginTransaction())
			{
				var bulkLoader = new MySqlBulkLoader(connection)
				{
					FileName = AppConfig.MySqlBulkLoaderLocalCsvFile,
					TableName = m_testTable,
					CharacterSet = "UTF8",
					NumberOfLinesToSkip = 1,
					FieldTerminator = ",",
					FieldQuotationCharacter = '"',
					FieldQuotationOptional = true,
					Local = true,
				};
				bulkLoader.Expressions.Add("five = UNHEX(five)");
				bulkLoader.Columns.AddRange(new[] { "one", "two", "three", "four", "five" });

				var rowCount = bulkLoader.Load();
				Assert.Equal(20, rowCount);

				transaction.Commit();
			}

			Assert.Equal(20, connection.ExecuteScalar<int>($@"select count(*) from {m_testTable};"));
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public void BulkLoadLocalCsvFileBeforeTransactionWithCommit()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			var bulkLoader = new MySqlBulkLoader(connection)
			{
				FileName = AppConfig.MySqlBulkLoaderLocalCsvFile,
				TableName = m_testTable,
				CharacterSet = "UTF8",
				NumberOfLinesToSkip = 1,
				FieldTerminator = ",",
				FieldQuotationCharacter = '"',
				FieldQuotationOptional = true,
				Local = true,
			};
			bulkLoader.Expressions.Add("five = UNHEX(five)");
			bulkLoader.Columns.AddRange(new[] { "one", "two", "three", "four", "five" });

			using (var transaction = connection.BeginTransaction())
			{
				var rowCount = bulkLoader.Load();
				Assert.Equal(20, rowCount);

				transaction.Commit();
			}

			Assert.Equal(20, connection.ExecuteScalar<int>($@"select count(*) from {m_testTable};"));
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public void BulkLoadLocalCsvFileInTransactionWithRollback()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			using (var transaction = connection.BeginTransaction())
			{
				var bulkLoader = new MySqlBulkLoader(connection)
				{
					FileName = AppConfig.MySqlBulkLoaderLocalCsvFile,
					TableName = m_testTable,
					CharacterSet = "UTF8",
					NumberOfLinesToSkip = 1,
					FieldTerminator = ",",
					FieldQuotationCharacter = '"',
					FieldQuotationOptional = true,
					Local = true,
				};
				bulkLoader.Expressions.Add("five = UNHEX(five)");
				bulkLoader.Columns.AddRange(new[] { "one", "two", "three", "four", "five" });

				var rowCount = bulkLoader.Load();
				Assert.Equal(20, rowCount);

				transaction.Rollback();
			}

			Assert.Equal(0, connection.ExecuteScalar<int>($@"select count(*) from {m_testTable};"));
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public void BulkLoadLocalCsvFileBeforeTransactionWithRollback()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			var bulkLoader = new MySqlBulkLoader(connection)
			{
				FileName = AppConfig.MySqlBulkLoaderLocalCsvFile,
				TableName = m_testTable,
				CharacterSet = "UTF8",
				NumberOfLinesToSkip = 1,
				FieldTerminator = ",",
				FieldQuotationCharacter = '"',
				FieldQuotationOptional = true,
				Local = true,
			};
			bulkLoader.Expressions.Add("five = UNHEX(five)");
			bulkLoader.Columns.AddRange(new[] { "one", "two", "three", "four", "five" });

			using (var transaction = connection.BeginTransaction())
			{
				var rowCount = bulkLoader.Load();
				Assert.Equal(20, rowCount);

				transaction.Rollback();
			}

			Assert.Equal(0, connection.ExecuteScalar<int>($@"select count(*) from {m_testTable};"));
		}

		[Fact]
		public void BulkLoadMissingFileName()
		{
			using var connection = new MySqlConnection(GetConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			bl.TableName = m_testTable;
			bl.CharacterSet = "UTF8";
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.FieldTerminator = ",";
			bl.FieldQuotationCharacter = '"';
			bl.FieldQuotationOptional = true;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.Local = false;
#if BASELINE
			Assert.Throws<System.NullReferenceException>(() =>
			{
				int rowCount = bl.Load();
			});
#else
			Assert.Throws<System.InvalidOperationException>(() =>
			{
				int rowCount = bl.Load();
			});
#endif
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public void BulkLoadMissingTableName()
		{
			using var connection = new MySqlConnection(GetConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			bl.FileName = AppConfig.MySqlBulkLoaderLocalCsvFile;
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.FieldTerminator = ",";
			bl.FieldQuotationCharacter = '"';
			bl.FieldQuotationOptional = true;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.Local = false;
#if BASELINE
			Assert.Throws<MySqlException>(() =>
			{
				int rowCount = bl.Load();
			});
#else
			Assert.Throws<System.InvalidOperationException>(() =>
			{
				int rowCount = bl.Load();
			});
#endif
		}

#if !BASELINE
		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public void BulkLoadFileStreamInvalidOperation()
		{
			using var connection = new MySqlConnection(GetConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			using var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
			bl.SourceStream = fileStream;
			bl.TableName = m_testTable;
			bl.CharacterSet = "UTF8";
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.FieldTerminator = ",";
			bl.FieldQuotationCharacter = '"';
			bl.FieldQuotationOptional = true;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.Local = false;
			Assert.Throws<System.InvalidOperationException>(() =>
			{
				int rowCount = bl.Load();
			});
		}

		[SkippableFact(ConfigSettings.LocalCsvFile)]
		public void BulkLoadLocalFileStream()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			using var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
			bl.SourceStream = fileStream;
			bl.TableName = m_testTable;
			bl.CharacterSet = "UTF8";
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.FieldTerminator = ",";
			bl.FieldQuotationCharacter = '"';
			bl.FieldQuotationOptional = true;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.Local = true;
			int rowCount = bl.Load();
			Assert.Equal(20, rowCount);
		}

		[Fact]
		public void BulkLoadMemoryStreamInvalidOperation()
		{
			using var connection = new MySqlConnection(GetConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			using var memoryStream = new MemoryStream(m_memoryStreamBytes, false);
			bl.SourceStream = memoryStream;
			bl.TableName = m_testTable;
			bl.CharacterSet = "UTF8";
			bl.Columns.AddRange(new string[] { "one", "two", "three" });
			bl.NumberOfLinesToSkip = 0;
			bl.FieldTerminator = ",";
			bl.FieldQuotationCharacter = '"';
			bl.FieldQuotationOptional = true;
			bl.Local = false;
			Assert.Throws<System.InvalidOperationException>(() => bl.Load());
		}

		[Fact]
		public void BulkLoadLocalMemoryStream()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			using var memoryStream = new MemoryStream(m_memoryStreamBytes, false);
			bl.SourceStream = memoryStream;
			bl.TableName = m_testTable;
			bl.CharacterSet = "UTF8";
			bl.Columns.AddRange(new string[] { "one", "two", "three" });
			bl.NumberOfLinesToSkip = 0;
			bl.FieldTerminator = ",";
			bl.FieldQuotationCharacter = '"';
			bl.FieldQuotationOptional = true;
			bl.Local = true;
			int rowCount = bl.Load();
			Assert.Equal(5, rowCount);
		}

		[Fact]
		public void BulkCopyDataReader()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			using var connection2 = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			connection2.Open();
			using (var cmd = new MySqlCommand(@"drop table if exists bulk_load_data_reader_source;
drop table if exists bulk_load_data_reader_destination;
create table bulk_load_data_reader_source(value int, name text);
create table bulk_load_data_reader_destination(value int, name text);
insert into bulk_load_data_reader_source values(0, 'zero'),(1,'one'),(2,'two'),(3,'three'),(4,'four'),(5,'five'),(6,'six');", connection))
			{
				cmd.ExecuteNonQuery();
			}

			using (var cmd = new MySqlCommand("select * from bulk_load_data_reader_source;", connection))
			using (var reader = cmd.ExecuteReader())
			{
				var bulkCopy = new MySqlBulkCopy(connection2) { DestinationTableName = "bulk_load_data_reader_destination", };
				bulkCopy.WriteToServer(reader);
			}

			using var cmd1 = new MySqlCommand("select * from bulk_load_data_reader_source order by value;", connection);
			using var cmd2 = new MySqlCommand("select * from bulk_load_data_reader_destination order by value;", connection2);
			using var reader1 = cmd1.ExecuteReader();
			using var reader2 = cmd2.ExecuteReader();
			while (reader1.Read())
			{
				Assert.True(reader2.Read());
				Assert.Equal(reader1.GetInt32(0), reader2.GetInt32(0));
				Assert.Equal(reader1.GetString(1), reader2.GetString(1));
			}
			Assert.False(reader2.Read());
		}

#if !NETCOREAPP1_1_2
		[Fact]
		public void BulkCopyNullDataTable()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			var bulkCopy = new MySqlBulkCopy(connection);
			Assert.Throws<ArgumentNullException>(() => bulkCopy.WriteToServer(default(DataTable)));
		}

		[Fact]
		public void BulkCopyDataTableWithLongBlob()
		{
			var dataTable = new DataTable()
			{
				Columns =
				{
					new DataColumn("id", typeof(int)),
					new DataColumn("data", typeof(byte[])),
				},
				Rows =
				{
					new object[] { 1, new byte[524200] },
					new object[] { 12345678, new byte[524200] },
				},
			};

			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			using (var cmd = new MySqlCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a int, b longblob);", connection))
			{
				cmd.ExecuteNonQuery();
			}

			var bulkCopy = new MySqlBulkCopy(connection)
			{
				DestinationTableName = "bulk_load_data_table",
			};
			bulkCopy.WriteToServer(dataTable);

			using (var cmd = new MySqlCommand(@"select sum(length(b)) from bulk_load_data_table;", connection))
			{
				Assert.Equal(1_048_400m, cmd.ExecuteScalar());
			}
		}

		[SkippableTheory(ServerFeatures.LargePackets)]
		[InlineData(6)]
		[InlineData(12)]
		[InlineData(21)]
		[InlineData(50)]
		[InlineData(100)]
		public void BulkCopyDataTableWithLongData(int rows)
		{
			// create a string that will be about 120,000 UTF-8 bytes
			var sb = new StringBuilder { Capacity = 121_000 };
			for (var j = 0; j < 4; j++)
			{
				for (var i = 0x20; i < 0x2000; i++)
					sb.Append((char) i);
				for (var i = 0x10000; i < 0x10780; i++)
					sb.Append(char.ConvertFromUtf32(i));
			}
			var str = sb.ToString();

			var bytes = new byte[50_000];
			var random = new Random(1);
			random.NextBytes(bytes);

			var dataTable = new DataTable()
			{
				Columns =
				{
					new DataColumn("data1", typeof(string)),
					new DataColumn("data2", typeof(byte[])),
				},
			};
			for (var i = 0; i < rows; i++)
				dataTable.Rows.Add(str, bytes);

			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			using (var cmd = new MySqlCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a mediumtext collate utf8mb4_bin, b mediumblob);", connection))
			{
				cmd.ExecuteNonQuery();
			}

			var bulkCopy = new MySqlBulkCopy(connection)
			{
				DestinationTableName = "bulk_load_data_table",
			};
			bulkCopy.WriteToServer(dataTable);

			using (var cmd = new MySqlCommand("select a, b from bulk_load_data_table;", connection))
			using (var reader = cmd.ExecuteReader())
			{
				var readRows = 0;
				var readBytes = new byte[50_000];
				while (reader.Read())
				{
					readRows++;
					Assert.Equal(str, reader.GetString(0));
					reader.GetBytes(1, 0, readBytes, 0, readBytes.Length);
					Assert.Equal(bytes, readBytes);
				}
				Assert.Equal(rows, readRows);
			}
		}

		[Fact]
		public void BulkCopyDataTableWithSpecialCharacters()
		{
			var dataTable = new DataTable()
			{
				Columns =
				{
					new DataColumn("id", typeof(int)),
					new DataColumn("data", typeof(string)),
				},
			};

			var strings = new[] { " ", "\t", ",", "\n", "\r", "\\", "ab\t", "\tcd", "ab\tcd", "\tab\ncd\t" };
			for (var i = 0; i < strings.Length; i++)
				dataTable.Rows.Add(i, strings[i]);

			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			using (var cmd = new MySqlCommand(@"drop table if exists bulk_load_data_table;
create table bulk_load_data_table(a int, b text);", connection))
			{
				cmd.ExecuteNonQuery();
			}

			var bulkCopy = new MySqlBulkCopy(connection)
			{
				DestinationTableName = "bulk_load_data_table",
			};
			bulkCopy.WriteToServer(dataTable);

			using (var cmd = new MySqlCommand("select * from bulk_load_data_table order by a;", connection))
			using (var reader = cmd.ExecuteReader())
			{
				for (int i = 0; i < strings.Length; i++)
				{
					Assert.True(reader.Read());
					Assert.Equal(i, reader.GetInt32(0));
					Assert.Equal(strings[i], reader.GetString(1));
				}
				Assert.False(reader.Read());
			}
		}

		[Theory]
		[InlineData(0, 15, 0, 0)]
		[InlineData(5, 15, 3, 15)]
		[InlineData(5, 16, 3, 15)]
		[InlineData(int.MaxValue, 0, 0, 0)]
		public void BulkCopyNotifyAfter(int notifyAfter, int rowCount, int expectedEventCount, int expectedRowsCopied)
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			using (var cmd = new MySqlCommand(@"drop table if exists bulk_copy_notify_after;
				create table bulk_copy_notify_after(value int);", connection))
			{
				cmd.ExecuteNonQuery();
			}

			var bulkCopy = new MySqlBulkCopy(connection)
			{
				NotifyAfter = notifyAfter,
				DestinationTableName = "bulk_copy_notify_after",
			};
			int eventCount = 0;
			long rowsCopied = 0;
			bulkCopy.MySqlRowsCopied += (s, e) =>
			{
				eventCount++;
				rowsCopied = e.RowsCopied;
				Assert.Equal(bulkCopy.RowsCopied, e.RowsCopied);
			};

			var dataTable = new DataTable()
			{
				Columns = { new DataColumn("value", typeof(int)) },
			};
			foreach (var x in Enumerable.Range(1, rowCount))
				dataTable.Rows.Add(new object[] { x });

			bulkCopy.WriteToServer(dataTable);
			Assert.Equal(expectedEventCount, eventCount);
			Assert.Equal(expectedRowsCopied, rowsCopied);
			Assert.Equal(rowCount, bulkCopy.RowsCopied);
		}

		[Theory]
		[InlineData(0, 40, 0, 0, 0, 40)]
		[InlineData(5, 40, 15, 3, 15, 0)]
		[InlineData(5, 40, 20, 4, 20, 17)]
		[InlineData(int.MaxValue, 20, 0, 0, 0, 20)]
		public void BulkCopyAbort(int notifyAfter, int rowCount, int abortAfter, int expectedEventCount, int expectedRowsCopied, long expectedCount)
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			using (var cmd = new MySqlCommand(@"drop table if exists bulk_copy_abort;
				create table bulk_copy_abort(value longtext);", connection))
			{
				cmd.ExecuteNonQuery();
			}

			var bulkCopy = new MySqlBulkCopy(connection)
			{
				NotifyAfter = notifyAfter,
				DestinationTableName = "bulk_copy_abort",
			};
			int eventCount = 0;
			long rowsCopied = 0;
			bulkCopy.MySqlRowsCopied += (s, e) =>
			{
				eventCount++;
				rowsCopied = e.RowsCopied;
				if (e.RowsCopied >= abortAfter)
					e.Abort = true;
			};

			var dataTable = new DataTable()
			{
				Columns = { new DataColumn("value", typeof(string)) },
			};
			var str = new string('a', 62500);
			foreach (var x in Enumerable.Range(1, rowCount))
				dataTable.Rows.Add(new object[] { str });

			bulkCopy.WriteToServer(dataTable);
			Assert.Equal(expectedEventCount, eventCount);
			Assert.Equal(expectedRowsCopied, rowsCopied);

			using (var cmd = new MySqlCommand("select count(value) from bulk_copy_abort;", connection))
				Assert.Equal(expectedCount, cmd.ExecuteScalar());
		}

		[Fact]
		public void BulkCopyColumnMappings()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			using (var cmd = new MySqlCommand(@"drop table if exists bulk_copy_column_mapping;
				create table bulk_copy_column_mapping(intvalue int, `text` text, data blob);", connection))
			{
				cmd.ExecuteNonQuery();
			}

			var bulkCopy = new MySqlBulkCopy(connection)
			{
				DestinationTableName = "bulk_copy_column_mapping",
				ColumnMappings =
				{
					new(1, "@val", "intvalue = @val + 1"),
					new(3, "text"),
					new(4, "data"),
				},
			};

			var dataTable = new DataTable()
			{
				Columns =
				{
					new DataColumn("c1", typeof(int)),
					new DataColumn("c2", typeof(int)),
					new DataColumn("c3", typeof(string)),
					new DataColumn("c4", typeof(string)),
					new DataColumn("c5", typeof(byte[])),
				},
				Rows =
				{
					new object[] { 1, 100, "a", "A", new byte[] { 0x33, 0x30 } },
					new object[] { 2, 200, "bb", "BB", new byte[] { 0x33, 0x31 } },
					new object[] { 3, 300, "ccc", "CCC", new byte[] { 0x33, 0x32 } },
				}
			};

			bulkCopy.WriteToServer(dataTable);

			using var reader = connection.ExecuteReader(@"select * from bulk_copy_column_mapping;");
			Assert.True(reader.Read());
			Assert.Equal(101, reader.GetValue(0));
			Assert.Equal("A", reader.GetValue(1));
			Assert.Equal(new byte[] { 0x33, 0x30 }, reader.GetValue(2));
			Assert.True(reader.Read());
			Assert.Equal(201, reader.GetValue(0));
			Assert.Equal("BB", reader.GetValue(1));
			Assert.Equal(new byte[] { 0x33, 0x31 }, reader.GetValue(2));
			Assert.True(reader.Read());
			Assert.Equal(301, reader.GetValue(0));
			Assert.Equal("CCC", reader.GetValue(1));
			Assert.Equal(new byte[] { 0x33, 0x32 }, reader.GetValue(2));
			Assert.False(reader.Read());
		}

		[Fact]
		public void BulkCopyColumnMappingsInvalidSourceOrdinal()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			using (var cmd = new MySqlCommand(@"drop table if exists bulk_copy_column_mapping;
				create table bulk_copy_column_mapping(intvalue int, `text` text, data blob);", connection))
			{
				cmd.ExecuteNonQuery();
			}

			var bulkCopy = new MySqlBulkCopy(connection)
			{
				DestinationTableName = "bulk_copy_column_mapping",
				ColumnMappings =
				{
					new(6, "@val", "intvalue = @val + 1"),
				},
			};

			var dataTable = new DataTable()
			{
				Columns =
				{
					new DataColumn("c1", typeof(int)),
				},
				Rows =
				{
					new object[] { 1 },
					new object[] { 2 },
					new object[] { 3 },
				}
			};

			Assert.Throws<InvalidOperationException>(() => bulkCopy.WriteToServer(dataTable));
		}

		[Fact]
		public void BulkCopyColumnMappingsInvalidDestinationColumn()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			using (var cmd = new MySqlCommand(@"drop table if exists bulk_copy_column_mapping;
				create table bulk_copy_column_mapping(intvalue int, `text` text, data blob);", connection))
			{
				cmd.ExecuteNonQuery();
			}

			var bulkCopy = new MySqlBulkCopy(connection)
			{
				DestinationTableName = "bulk_copy_column_mapping",
				ColumnMappings =
				{
					new() { SourceOrdinal = 0 },
				},
			};

			var dataTable = new DataTable()
			{
				Columns =
				{
					new DataColumn("c1", typeof(int)),
				},
				Rows =
				{
					new object[] { 1 },
					new object[] { 2 },
					new object[] { 3 },
				}
			};

			Assert.Throws<InvalidOperationException>(() => bulkCopy.WriteToServer(dataTable));
		}

		[Fact]
		public void BulkCopyDoesNotInsertAllRows()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();

			connection.Execute(@"drop table if exists bulk_copy_duplicate_pk;
create table bulk_copy_duplicate_pk(id integer primary key, value text not null);");

			var bcp = new MySqlBulkCopy(connection)
			{
				DestinationTableName = "bulk_copy_duplicate_pk"
			};

			var dataTable = new DataTable()
			{
				Columns =
				{
					new DataColumn("id", typeof(int)),
					new DataColumn("value", typeof(string)),
				},
				Rows =
				{
					new object[] { 1, "a" },
					new object[] { 1, "b" },
					new object[] { 3, "c" },
				}
			};

			var ex = Assert.Throws<MySqlException>(() => bcp.WriteToServer(dataTable));
			Assert.Equal(MySqlErrorCode.BulkCopyFailed, ex.ErrorCode);
		}
#endif

		[Fact]
		public void BulkCopyNullDataReader()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			var bulkCopy = new MySqlBulkCopy(connection);
			Assert.Throws<ArgumentNullException>(() => bulkCopy.WriteToServer(default(DbDataReader)));
		}
#endif

		internal static string GetConnectionString() => AppConfig.ConnectionString;

		internal static string GetLocalConnectionString()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.AllowLoadLocalInfile = true;
			return csb.ConnectionString;
		}

		readonly string m_testTable;
		readonly byte[] m_memoryStreamBytes;
	}
}
