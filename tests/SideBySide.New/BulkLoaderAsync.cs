using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;
using Xunit.Abstractions;
using Dapper;

namespace SideBySide.New
{
	[Collection("BulkLoaderCollection")]
	public class BulkLoaderAsync : IClassFixture<DatabaseFixture>
	{
		private readonly ITestOutputHelper output;

		public BulkLoaderAsync(DatabaseFixture database, ITestOutputHelper output)
		{
			this.output = output;

			m_database = database;
			//xUnit runs tests in different classes in parallel, so use different table names for the different test classes
			string testClient;
#if BASELINE
			testClient = "Baseline";
#else
			testClient = "New";
#endif
			m_testTable = "test.BulkLoaderAsyncTest" + testClient;

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
		public async Task BulkLoadTsvFile()
		{
			try
			{
				await InitializeTestAsync();

				using (MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString))
				{
					MySqlBulkLoader bl = new MySqlBulkLoader(connection);
					bl.FileName = AppConfig.MySqlBulkLoaderTsvFile;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
					bl.NumberOfLinesToSkip = 1;
					bl.Expressions.Add("five = UNHEX(five)");
					bl.Local = false;
					int rowCount = await bl.LoadAsync();
					Assert.Equal(20, rowCount);
				}
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}

		[BulkLoaderTsvFileFact]
		public async Task BulkLoadLocalTsvFile()
		{
			try
			{
				await InitializeTestAsync();

				using (MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString))
				{
					MySqlBulkLoader bl = new MySqlBulkLoader(connection);
					bl.FileName = AppConfig.MySqlBulkLoaderTsvFile;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
					bl.NumberOfLinesToSkip = 1;
					bl.Expressions.Add("five = UNHEX(five)");
					bl.Local = true;
					int rowCount = await bl.LoadAsync();
					Assert.Equal(20, rowCount);
				}
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}

		[BulkLoaderTsvFileFact]
		public async Task BulkLoadLocalTsvFileDoubleEscapedTerminators()
		{
			try
			{
				await InitializeTestAsync();

				using (MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString))
				{
					MySqlBulkLoader bl = new MySqlBulkLoader(connection);
					bl.FileName = AppConfig.MySqlBulkLoaderTsvFile;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
					bl.NumberOfLinesToSkip = 1;
					bl.Expressions.Add("five = UNHEX(five)");
					bl.LineTerminator = "\\n";
					bl.FieldTerminator = "\\t";
					bl.Local = true;
					int rowCount = await bl.LoadAsync();
					Assert.Equal(20, rowCount);
				}
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}

		[BulkLoaderCsvFileFact]
		public async Task BulkLoadCsvFile()
		{
			try
			{
				await InitializeTestAsync();

				using (MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString))
				{
					MySqlBulkLoader bl = new MySqlBulkLoader(connection);
					bl.FileName = AppConfig.MySqlBulkLoaderCsvFile;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
					bl.NumberOfLinesToSkip = 1;
					bl.FieldTerminator = ",";
					bl.FieldQuotationCharacter = '"';
					bl.FieldQuotationOptional = true;
					bl.Expressions.Add("five = UNHEX(five)");
					bl.Local = false;
					int rowCount = await bl.LoadAsync();
					Assert.Equal(20, rowCount);
				}
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}

		[BulkLoaderLocalCsvFileFact]
		public async Task BulkLoadLocalCsvFile()
		{
			try
			{
				await InitializeTestAsync();

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
				int rowCount = await bl.LoadAsync();
				Assert.Equal(20, rowCount);
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}

		[Fact]
		public async Task BulkLoadCsvFileNotFound()
		{
			try
			{
				await InitializeTestAsync();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				bl.FileName = AppConfig.MySqlBulkLoaderCsvFile + "-junk";
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
 					int rowCount = await bl.LoadAsync();
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
				catch (System.InvalidOperationException invalidOperationException)
				{
					Assert.IsType(typeof(System.InvalidOperationException), invalidOperationException);
				}
				catch (Exception exception)
				{
					//We know that the exception is not a MySqlException, just the assertion to fail the test
					Assert.IsType(typeof(MySqlException), exception);
				};
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}

		[Fact]
		public async Task BulkLoadLocalCsvFileNotFound()
		{
			try
			{
				await InitializeTestAsync();

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
					int rowCount = await bl.LoadAsync();
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
				await FinalizeTestAsync();
			}
		}
		[Fact]
		public async Task BulkLoadMissingFileName()
		{
			try
			{
				await InitializeTestAsync();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				bl.TableName = m_testTable;
				bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
				bl.NumberOfLinesToSkip = 1;
				bl.FieldTerminator = ",";
				bl.FieldQuotationCharacter = '"';
				bl.FieldQuotationOptional = true;
				bl.Expressions.Add("five = UNHEX(five)");
				bl.Local = false;
#if BASELINE
						await Assert.ThrowsAsync<System.NullReferenceException>(async () =>
						{
							int rowCount = await bl.LoadAsync();
						});
#else
				await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
				{
					int rowCount = await bl.LoadAsync();
				});
#endif
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}

		[BulkLoaderLocalCsvFileFact]
		public async Task BulkLoadMissingTableName()
		{
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
			await Assert.ThrowsAsync<MySqlException>(async () =>
			{
				int rowCount = await bl.LoadAsync();
			});
#else
			await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
			{
				int rowCount = await bl.LoadAsync();
			});
#endif
		}

#if BASELINE
		[Fact(Skip = "InfileStream not implemented")]
		public void BulkLoadFileStreamInvalidOperation() {}
#else
		[BulkLoaderLocalCsvFileFact]
		public async Task BulkLoadFileStreamInvalidOperation()
		{
			try
			{
				await InitializeTestAsync();

				using (MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString))
				{
					MySqlBulkLoader bl = new MySqlBulkLoader(connection);
					using (var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
					{
						bl.InfileStream = fileStream;
						bl.TableName = m_testTable;
						bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
						bl.NumberOfLinesToSkip = 1;
						bl.FieldTerminator = ",";
						bl.FieldQuotationCharacter = '"';
						bl.FieldQuotationOptional = true;
						bl.Expressions.Add("five = UNHEX(five)");
						bl.Local = false;
						await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
						{
							int rowCount = await bl.LoadAsync();
						});
					}
				}
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}
#endif

#if BASELINE
		[Fact(Skip = "InfileStream not implemented")]
		public void BulkLoadLocalFileStream() {}
#else
		[BulkLoaderLocalCsvFileFact]
		public async Task BulkLoadLocalFileStream()
		{
			try
			{
				await InitializeTestAsync();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				using (var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
				{
					bl.InfileStream = fileStream;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
					bl.NumberOfLinesToSkip = 1;
					bl.FieldTerminator = ",";
					bl.FieldQuotationCharacter = '"';
					bl.FieldQuotationOptional = true;
					bl.Expressions.Add("five = UNHEX(five)");
					bl.Local = true;
					int rowCount = await bl.LoadAsync();
					Assert.Equal(20, rowCount);
				}
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}
#endif
#if BASELINE
		[Fact(Skip = "InfileStream not implemented")]
		public void BulkLoadMemoryStreamInvalidOperation() {}
#else
		[Fact]
		public async Task BulkLoadMemoryStreamInvalidOperation()
		{
			try
			{
				await InitializeTestAsync();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				using (var memoryStream = new MemoryStream(m_memoryStreamBytes, false))
				{
					bl.InfileStream = memoryStream;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three" });
					bl.NumberOfLinesToSkip = 0;
					bl.FieldTerminator = ",";
					bl.FieldQuotationCharacter = '"';
					bl.FieldQuotationOptional = true;
					bl.Local = false;
					await Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
					{
						int rowCount = await bl.LoadAsync();
					});
				}
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}
#endif
#if BASELINE
		[Fact(Skip = "InfileStream not implemented")]
		public void BulkLoadLocalMemoryStream() {}
#else
		[Fact]
		public async Task BulkLoadLocalMemoryStream()
		{
			try
			{
				await InitializeTestAsync();

				MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
				using (var memoryStream = new MemoryStream(m_memoryStreamBytes, false))
				{
					bl.InfileStream = memoryStream;
					bl.TableName = m_testTable;
					bl.Columns.AddRange(new string[] { "one", "two", "three" });
					bl.NumberOfLinesToSkip = 0;
					bl.FieldTerminator = ",";
					bl.FieldQuotationCharacter = '"';
					bl.FieldQuotationOptional = true;
					bl.Local = true;
					int rowCount = await bl.LoadAsync();
					Assert.Equal(5, rowCount);
				}
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}
#endif

		private async Task InitializeTestAsync()
		{
			MySqlConnection.ClearAllPools();
			using (MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				await connection.ExecuteAsync(m_initializeTable);
			}
		}
		private async Task FinalizeTestAsync()
		{
			if (AppConfig.MySqlBulkLoaderRemoveTables)
			{
				using (MySqlConnection connection = new MySqlConnection(AppConfig.ConnectionString))
				{
					await connection.ExecuteAsync(m_removeTable);
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
