using System.Data;
using System.Threading.Tasks;
using Dapper;
#if BASELINE
using MySql.Data.MySqlClient;
#else
using MySqlConnector;
#endif
using Xunit;

namespace SideBySide;

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
	public async Task CommandLoadCsvFile()
	{
		var insertInlineCommand = string.Format(m_loadDataInfileCommand, "", AppConfig.MySqlBulkLoaderCsvFile.Replace("\\", "\\\\"));
		using var command = new MySqlCommand(insertInlineCommand, m_database.Connection);
		if (m_database.Connection.State != ConnectionState.Open) await m_database.Connection.OpenAsync();
		var rowCount = await command.ExecuteNonQueryAsync();
		m_database.Connection.Close();
		Assert.Equal(20, rowCount);
	}

	[SkippableFact(ConfigSettings.LocalCsvFile | ConfigSettings.TrustedHost)]
	public async Task CommandLoadLocalCsvFile()
	{
		var insertInlineCommand = string.Format(m_loadDataInfileCommand, " LOCAL",
			AppConfig.MySqlBulkLoaderLocalCsvFile.Replace("\\", "\\\\"));
		using var command = new MySqlCommand(insertInlineCommand, m_database.Connection);

		if (m_database.Connection.State != ConnectionState.Open)
			await m_database.Connection.OpenAsync();

		var rowCount = await command.ExecuteNonQueryAsync();

		m_database.Connection.Close();
		Assert.Equal(20, rowCount);
	}

	[SkippableFact(ConfigSettings.LocalCsvFile | ConfigSettings.TrustedHost, Baseline = "Doesn't require trusted host for LOAD DATA LOCAL INFILE")]
	public async Task ThrowsNotSupportedExceptionForNotTrustedHostAndNotStream()
	{
		var insertInlineCommand = string.Format(m_loadDataInfileCommand, " LOCAL",
			AppConfig.MySqlBulkLoaderLocalCsvFile.Replace("\\", "\\\\"));
		using var command = new MySqlCommand(insertInlineCommand, m_database.Connection);

		if (m_database.Connection.State != ConnectionState.Open)
			await m_database.Connection.OpenAsync();

		await Assert.ThrowsAsync<MySqlException>(async () => await command.ExecuteNonQueryAsync());
	}

	readonly DatabaseFixture m_database;
	readonly string m_testTable;
	readonly string m_loadDataInfileCommand;
}
