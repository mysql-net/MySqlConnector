using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;
using Dapper;

namespace SideBySide.New
{
    public class LoadDataInfileSync : IClassFixture<DatabaseFixture>
    {
        public LoadDataInfileSync(DatabaseFixture database)
        {
            m_database = database;
            m_initializeTable = @"
                create schema if not exists test; 
                drop table if exists test.LoadDataInfileSyncTest; 
                CREATE TABLE test.LoadDataInfileSyncTest
                (
	                one int primary key
                    , ignore_one int
                    , two varchar(200)
                    , ignore_two varchar(200)
                    , three varchar(200)
                    , four datetime
                    , five blob
                );";
            m_removeTable = "drop table if exists test.LoadDataInfileSyncTest;";
            m_loadDataInfileCommand = "LOAD DATA{0} INFILE '{1}' INTO TABLE test.LoadDataInfileSyncTest FIELDS TERMINATED BY ',' OPTIONALLY ENCLOSED BY '\"' IGNORE 1 LINES (one, two, three, four, five) SET five = UNHEX(five);";
        }

        [Fact]
        public void CommandLoadCsvFile()
        {
            tableSemaphore.Wait();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                string insertInlineCommand = string.Format(m_loadDataInfileCommand, "", AppConfig.MySqlBulkLoaderCsvFile.Replace("\\", "\\\\"));
                MySqlCommand command = new MySqlCommand(insertInlineCommand, m_database.Connection);
                if (m_database.Connection.State != ConnectionState.Open) m_database.Connection.Open();
                int rowCount = command.ExecuteNonQuery();
                m_database.Connection.Close();
                Assert.Equal(20, rowCount);
            }
            finally
            {
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
            }
        }
        [Fact]
        public void CommandLoadLocalCsvFile()
        {
            tableSemaphore.Wait();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                string insertInlineCommand = string.Format(m_loadDataInfileCommand, " LOCAL", AppConfig.MySqlBulkLoaderLocalCsvFile.Replace("\\", "\\\\"));
                MySqlCommand command = new MySqlCommand(insertInlineCommand, m_database.Connection);
                if (m_database.Connection.State != ConnectionState.Open) m_database.Connection.Open();
                int rowCount = command.ExecuteNonQuery();
                m_database.Connection.Close();
                Assert.Equal(20, rowCount);
            }
            finally
            {
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
            }
        }

        readonly SemaphoreSlim tableSemaphore = new SemaphoreSlim(1, 1); //Use a semaphore to limit access to the load table to one test at a time
        readonly DatabaseFixture m_database;
        readonly string m_initializeTable;
        readonly string m_removeTable;
        readonly string m_loadDataInfileCommand;
    }
}
