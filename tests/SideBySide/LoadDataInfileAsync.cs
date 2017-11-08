using System.Data;
using Dapper;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	[Collection("BulkLoaderCollection")]
	public class LoadDataInfileAsync : IClassFixture<DatabaseFixture>
	{
		public LoadDataInfileAsync(DatabaseFixture database)
		{
			m_database = database;

			m_testTable = "LoadDataInfileAsyncTest";
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
				) character set = utf8mb4;";
			m_database.Connection.Execute(initializeTable);

			m_loadDataInfileCommand = "LOAD DATA{0} INFILE '{1}' INTO TABLE " + m_testTable + " CHARACTER SET UTF8MB4 FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '\"' IGNORE 1 LINES (one, two, three, four, five) SET five = UNHEX(five);";
		}

		[SkippableFact(ConfigSettings.CsvFile)]
		public async void CommandLoadCsvFile()
		{
			string insertInlineCommand = string.Format(m_loadDataInfileCommand, "", AppConfig.MySqlBulkLoaderCsvFile.Replace("\\", "\\\\"));
			MySqlCommand command = new MySqlCommand(insertInlineCommand, m_database.Connection);
			if (m_database.Connection.State != ConnectionState.Open) await m_database.Connection.OpenAsync();
			int rowCount = await command.ExecuteNonQueryAsync();
			m_database.Connection.Close();
			Assert.Equal(20, rowCount);
		}

		[SkippableFact(ConfigSettings.LocalCsvFile | ConfigSettings.TrustedHost)]
		public async void CommandLoadLocalCsvFile()
		{
			string insertInlineCommand = string.Format(m_loadDataInfileCommand, " LOCAL",
				AppConfig.MySqlBulkLoaderLocalCsvFile.Replace("\\", "\\\\"));
			MySqlCommand command = new MySqlCommand(insertInlineCommand, m_database.Connection);

			if (m_database.Connection.State != ConnectionState.Open)
				await m_database.Connection.OpenAsync();

			int rowCount = await command.ExecuteNonQueryAsync();

			m_database.Connection.Close();
			Assert.Equal(20, rowCount);
		}

		[SkippableFact(ConfigSettings.LocalCsvFile | ConfigSettings.TrustedHost, Baseline = "Doesn't require trusted host for LOAD DATA LOCAL INFILE")]
		public async void ThrowsNotSupportedExceptionForNotTrustedHostAndNotStream()
		{
			string insertInlineCommand = string.Format(m_loadDataInfileCommand, " LOCAL",
				AppConfig.MySqlBulkLoaderLocalCsvFile.Replace("\\", "\\\\"));
			MySqlCommand command = new MySqlCommand(insertInlineCommand, m_database.Connection);

			if (m_database.Connection.State != ConnectionState.Open)
				await m_database.Connection.OpenAsync();

			await Assert.ThrowsAsync<MySqlException>(async () => await command.ExecuteNonQueryAsync());
		}

		readonly DatabaseFixture m_database;
		readonly string m_testTable;
		readonly string m_loadDataInfileCommand;
	}
}
