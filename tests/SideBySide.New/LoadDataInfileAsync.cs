using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;
using Dapper;

namespace SideBySide.New
{
	[Collection("BulkLoaderCollection")]
	public class LoadDataInfileAsync : IClassFixture<DatabaseFixture>
	{
		public LoadDataInfileAsync(DatabaseFixture database)
		{
			m_database = database;
			//xUnit runs tests in different classes in parallel, so use different table names for the different test classes
			string testClient;
#if BASELINE
			testClient = "Baseline";
#else
			testClient = "New";
#endif
			m_testTable = "test.LoadDataInfileAsyncTest" + testClient;

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
				);";
			m_removeTable = "drop table if exists " + m_testTable + @";";
			m_loadDataInfileCommand = "LOAD DATA{0} INFILE '{1}' INTO TABLE " + m_testTable + " FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '\"' IGNORE 1 LINES (one, two, three, four, five) SET five = UNHEX(five);";
		}

		[BulkLoaderCsvFileFact]
		public async void CommandLoadCsvFile()
		{
			try
			{
				await InitializeTestAsync();

				string insertInlineCommand = string.Format(m_loadDataInfileCommand, "", AppConfig.MySqlBulkLoaderCsvFile.Replace("\\", "\\\\"));
				MySqlCommand command = new MySqlCommand(insertInlineCommand, m_database.Connection);
				if (m_database.Connection.State != ConnectionState.Open) await m_database.Connection.OpenAsync();
				int rowCount = await command.ExecuteNonQueryAsync();
				m_database.Connection.Close();
				Assert.Equal(20, rowCount);
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}

		[BulkLoaderLocalCsvFileFact]
		public async void CommandLoadLocalCsvFile()
		{
			try
			{
				await InitializeTestAsync();

				string insertInlineCommand = string.Format(m_loadDataInfileCommand, " LOCAL", AppConfig.MySqlBulkLoaderLocalCsvFile.Replace("\\", "\\\\"));
				MySqlCommand command = new MySqlCommand(insertInlineCommand, m_database.Connection);
				if (m_database.Connection.State != ConnectionState.Open) await m_database.Connection.OpenAsync();
				int rowCount = await command.ExecuteNonQueryAsync();
				m_database.Connection.Close();
				Assert.Equal(20, rowCount);
			}
			finally
			{
				await FinalizeTestAsync();
			}
		}

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
		readonly string m_loadDataInfileCommand;
	}
}
