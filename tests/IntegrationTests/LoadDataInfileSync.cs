namespace IntegrationTests;

[Collection("BulkLoaderCollection")]
public class LoadDataInfileSync : IClassFixture<DatabaseFixture>
{
	public LoadDataInfileSync(DatabaseFixture database)
	{
		m_database = database;

		m_testTable = "LoadDataInfileSyncTest";
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
	public void CommandLoadCsvFile()
	{
		var insertInlineCommand = string.Format(m_loadDataInfileCommand, "", AppConfig.MySqlBulkLoaderCsvFile.Replace("\\", "\\\\"));
		using var command = new MySqlCommand(insertInlineCommand, m_database.Connection);
		if (m_database.Connection.State != ConnectionState.Open)
			m_database.Connection.Open();
		var rowCount = command.ExecuteNonQuery();
		m_database.Connection.Close();
		Assert.Equal(20, rowCount);
	}

	[SkippableFact(ConfigSettings.LocalCsvFile | ConfigSettings.TrustedHost)]
	public void CommandLoadLocalCsvFile()
	{
		var insertInlineCommand = string.Format(m_loadDataInfileCommand, " LOCAL", AppConfig.MySqlBulkLoaderLocalCsvFile.Replace("\\", "\\\\"));
		using var command = new MySqlCommand(insertInlineCommand, m_database.Connection);
		if (m_database.Connection.State != ConnectionState.Open)
			m_database.Connection.Open();
		var rowCount = command.ExecuteNonQuery();
		m_database.Connection.Close();
		Assert.Equal(20, rowCount);
	}

	[SkippableFact(ConfigSettings.LocalCsvFile | ConfigSettings.TrustedHost, MySqlData = "Doesn't require trusted host for LOAD DATA LOCAL INFILE")]
	public void ThrowsNotSupportedExceptionForNotTrustedHostAndNotStream()
	{
		var insertInlineCommand = string.Format(m_loadDataInfileCommand, " LOCAL",
			AppConfig.MySqlBulkLoaderLocalCsvFile.Replace("\\", "\\\\"));
		using var command = new MySqlCommand(insertInlineCommand, m_database.Connection);
		if (m_database.Connection.State != ConnectionState.Open)
			m_database.Connection.Open();

		Assert.Throws<MySqlException>(() => command.ExecuteNonQuery());

		m_database.Connection.Close();
	}

	private readonly DatabaseFixture m_database;
	private readonly string m_testTable;
	private readonly string m_loadDataInfileCommand;
}
