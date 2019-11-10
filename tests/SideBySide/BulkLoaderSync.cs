using System;
using System.Data;
using System.IO;
using MySql.Data.MySqlClient;
using Xunit;
using Dapper;
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

		[SkippableFact(ConfigSettings.LocalTsvFile)]
		public void BulkLoadLocalTsvFileDoubleEscapedTerminators()
		{
			using var connection = new MySqlConnection(GetLocalConnectionString());
			connection.Open();
			MySqlBulkLoader bl = new MySqlBulkLoader(connection);
			bl.FileName = AppConfig.MySqlBulkLoaderLocalTsvFile;
			bl.TableName = m_testTable;
			bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
			bl.NumberOfLinesToSkip = 1;
			bl.Expressions.Add("five = UNHEX(five)");
			bl.LineTerminator = "\\n";
			bl.FieldTerminator = "\\t";
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
				while (exception.InnerException is object)
					exception = exception.InnerException;

				if (!(exception is FileNotFoundException))
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
				while (mySqlException.InnerException is object)
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

#if !NETCOREAPP1_1_2
		[Fact]
		public void BulkLoadDataTableWithLongData()
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
					new object[] { 1, new byte[8388500] },
					new object[] { 12345678, new byte[8388500] },
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
		}

		[Fact]
		public void BulkLoadDataTableWithTooLongData()
		{
			var dataTable = new DataTable()
			{
				Columns =
				{
					new DataColumn("data", typeof(byte[])),
				},
				Rows =
				{
					new object[] { new byte[8388700] },
				}
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
			Assert.Throws<MySqlException>(() => bulkCopy.WriteToServer(dataTable));
		}
#endif
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
