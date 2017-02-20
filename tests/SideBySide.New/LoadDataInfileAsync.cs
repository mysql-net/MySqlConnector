using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;
using Dapper;

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
				);";
			m_database.Connection.Execute(initializeTable);

			m_loadDataInfileCommand = "LOAD DATA{0} INFILE '{1}' INTO TABLE " + m_testTable + " FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '\"' IGNORE 1 LINES (one, two, three, four, five) SET five = UNHEX(five);";
		}

		[BulkLoaderCsvFileFact]
		public async void CommandLoadCsvFile()
		{
			string insertInlineCommand = string.Format(m_loadDataInfileCommand, "", AppConfig.MySqlBulkLoaderCsvFile.Replace("\\", "\\\\"));
			MySqlCommand command = new MySqlCommand(insertInlineCommand, m_database.Connection);
			if (m_database.Connection.State != ConnectionState.Open) await m_database.Connection.OpenAsync();
			int rowCount = await command.ExecuteNonQueryAsync();
			m_database.Connection.Close();
			Assert.Equal(20, rowCount);
		}

		[BulkLoaderLocalCsvFileFact]
		public async void CommandLoadLocalCsvFile()
		{
			string insertInlineCommand = string.Format(m_loadDataInfileCommand, " LOCAL", AppConfig.MySqlBulkLoaderLocalCsvFile.Replace("\\", "\\\\"));
			MySqlCommand command = new MySqlCommand(insertInlineCommand, m_database.Connection);
			if (m_database.Connection.State != ConnectionState.Open) await m_database.Connection.OpenAsync();
			int rowCount = await command.ExecuteNonQueryAsync();
			m_database.Connection.Close();
			Assert.Equal(20, rowCount);
		}

		readonly DatabaseFixture m_database;
		readonly string m_testTable;
		readonly string m_loadDataInfileCommand;
	}
}
