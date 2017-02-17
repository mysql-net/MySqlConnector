using System;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Xunit;
using Dapper;

namespace SideBySide.New
{
	[Collection("BulkLoaderCollection")]
	public class BulkLoaderSync : IClassFixture<DatabaseFixture>
	{
		public BulkLoaderSync(DatabaseFixture database)
		{
			m_database = database;
			//xUnit runs tests in different classes in parallel, so use different table names for the different test classes
			string testClient;
#if BASELINE
			testClient = "Baseline";
#else
			testClient = "New";
#endif
			m_testTable = "test.BulkLoaderSyncTest" + testClient;

			m_initializeTable = @"
				create schema if not exists test;
				drop table if exists " + m_testTable + @";
				CREATE TABLE " + m_testTable + @"
				(
					one int primary key
					, ignore_one int
					, two varchar(200)
					, ignore_two varchar(200)
					, three varchar(200)
					, four datetime
					, five blob
				) CHARACTER SET = UTF8;";
			m_removeTable = "drop table if exists " + m_testTable + @";";
			m_memoryStreamBytes = System.Text.Encoding.UTF8.GetBytes(@"1,'two-1','three-1'
2,'two-2','three-2'
3,'two-3','three-3'
4,'two-4','three-4'
5,'two-5','three-5'
");
		}

		[BulkLoaderTsvFileFact]
		public void BulkLoadTsvFile()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				bl.FileName = AppConfig.MySqlBulkLoaderTsvFile;
				bl.TableName = m_testTable;
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = false;
				int rowCount = bl.Load();
				Assert.Equal(20, rowCount);
			}
			finally
			{
				FinalizeTest();
			}
		}

		[BulkLoaderTsvFileFact]
		public void BulkLoadLocalTsvFile()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				bl.FileName = AppConfig.MySqlBulkLoaderTsvFile;
				bl.TableName = m_testTable;
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = true;
				int rowCount = bl.Load();
				Assert.Equal(20, rowCount);
			}
			finally
			{
				FinalizeTest();
			}
		}

		[BulkLoaderTsvFileFact]
		public void BulkLoadLocalTsvFileDoubleEscapedTerminators()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				bl.FileName = AppConfig.MySqlBulkLoaderTsvFile;
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
			finally
			{
				FinalizeTest();
			}
		}

		[BulkLoaderCsvFileFact]
		public void BulkLoadCsvFile()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
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
			finally
			{
				FinalizeTest();
			}
		}

		[BulkLoaderLocalCsvFileFact]
		public void BulkLoadLocalCsvFile()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
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
			finally
			{
				FinalizeTest();
			}
		}

		[Fact]
		public void BulkLoadCsvFileNotFound()
		{
			var secureFilePath = m_database.Connection.Query<string>(@"select @@global.secure_file_priv;").FirstOrDefault() ?? "";

			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
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
				catch (MySqlException mySqlException)
				{
					if (mySqlException.InnerException != null)
					{
						Assert.IsType(typeof(System.IO.FileNotFoundException), mySqlException.InnerException);
					}
					else
					{
						Assert.Contains("Errcode: 2 - No such file or directory", mySqlException.Message);
					}
				}
			}
			finally
			{
				FinalizeTest();
			}
		}

		[Fact]
		public void BulkLoadLocalCsvFileNotFound()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
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
					while (mySqlException.InnerException != null)
					{
						if (mySqlException.InnerException.GetType() == typeof(MySqlException))
						{
							mySqlException = (MySqlException)mySqlException.InnerException;
						}
						else
						{
							Assert.IsType(typeof(System.IO.FileNotFoundException), mySqlException.InnerException);
							break;
						}
					}
					if (mySqlException.InnerException == null)
					{
						Assert.IsType(typeof(System.IO.FileNotFoundException), mySqlException);
					}
				}
				catch (Exception exception)
				{
					//We know that the exception is not a MySqlException, just use the assertion to fail the test
					Assert.IsType(typeof(MySqlException), exception);
				};
			}
			finally
			{
				FinalizeTest();
			}
		}
		[Fact]
		public void BulkLoadMissingFileName()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
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
			finally
			{
				FinalizeTest();
			}
		}

		[BulkLoaderLocalCsvFileFact]
		public void BulkLoadMissingTableName()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
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
			finally
			{
				FinalizeTest();
			}
		}

#if BASELINE
		[Fact(Skip = "InfileStream not implemented")]
		public void BulkLoadFileStreamInvalidOperation() {}
#else
		[BulkLoaderLocalCsvFileFact]
		public void BulkLoadFileStreamInvalidOperation()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				using (var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
				{
					bl.InfileStream = fileStream;
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
			}
			finally
			{
				FinalizeTest();
			}
		}
#endif
#if BASELINE
		[Fact(Skip = "InfileStream not implemented")]
		public void BulkLoadLocalFileStream() {}
#else
		[BulkLoaderLocalCsvFileFact]
		public void BulkLoadLocalFileStream()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				using (var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
				{
					bl.InfileStream = fileStream;
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
			}
			finally
			{
				FinalizeTest();
			}
		}
#endif
#if BASELINE
		[Fact(Skip = "InfileStream not implemented")]
		public void BulkLoadMemoryStreamInvalidOperation() {}
#else
		[Fact]
		public void BulkLoadMemoryStreamInvalidOperation()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				using (var memoryStream = new MemoryStream(m_memoryStreamBytes, false))
				{
					bl.InfileStream = memoryStream;
					bl.TableName = m_testTable;
					bl.CharacterSet = "UTF8";
					bl.Columns.AddRange(new string[] { "one", "two", "three" });
					bl.NumberOfLinesToSkip = 0;
					bl.FieldTerminator = ",";
					bl.FieldQuotationCharacter = '"';
					bl.FieldQuotationOptional = true;
					bl.Local = false;
					Assert.Throws<System.InvalidOperationException>(() =>
					{
						int rowCount = bl.Load();
					});
				}
			}
			finally
			{
				FinalizeTest();
			}
		}
#endif
#if BASELINE
		[Fact(Skip = "InfileStream not implemented")]
		public void BulkLoadLocalMemoryStream() {}
#else
		[Fact]
		public void BulkLoadLocalMemoryStream()
		{
			try
			{
				InitializeTest();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				using (var memoryStream = new MemoryStream(m_memoryStreamBytes, false))
				{
					bl.InfileStream = memoryStream;
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
			}
			finally
			{
				FinalizeTest();
			}
		}
#endif

		private void InitializeTest()
		{
			MySqlConnection.ClearAllPools();
			using (MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Execute(m_initializeTable);
			}
		}
		private void FinalizeTest()
		{
			if (AppConfig.MySqlBulkLoaderRemoveTables)
			{
				using (MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString))
				{
					connection.Execute(m_removeTable);
				}
			}
		}

		readonly DatabaseFixture m_database;
		readonly string m_testTable;
		readonly string m_initializeTable;
		readonly string m_removeTable;
		readonly byte[] m_memoryStreamBytes;
	}
}
