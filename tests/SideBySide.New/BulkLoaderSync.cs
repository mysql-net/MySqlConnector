using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;
using Dapper;

namespace SideBySide.New
{
    public class BulkLoaderSync : IClassFixture<DatabaseFixture>
    {
        public BulkLoaderSync(DatabaseFixture database)
        {
            m_database = database;
            m_initializeTable = @"
                create schema if not exists test; 
                drop table if exists test.BulkLoaderSyncTest; 
                CREATE TABLE test.BulkLoaderSyncTest
                (
	                one int primary key
                    , ignore_one int
                    , two varchar(200)
                    , ignore_two varchar(200)
                    , three varchar(200)
                    , four datetime
                    , five blob
                ) CHARACTER SET = UTF8;";
            m_removeTable = "drop table if exists test.BulkLoaderSyncTest;";
            m_memoryStreamBytes = System.Text.Encoding.UTF8.GetBytes(@"1,'two-1','three-1'
2,'two-2','three-2'
3,'two-3','three-3'
4,'two-4','three-4'
5,'two-5','three-5'
");
        }

        [Fact]
        public void BulkLoadCsvFile()
        {
            tableSemaphore.Wait();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                bl.FileName = AppConfig.MySqlBulkLoaderCsvFile;
                bl.TableName = "test.BulkLoaderSyncTest";
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
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
            }
        }
        [Fact]
        public void BulkLoadLocalCsvFile()
        {
            tableSemaphore.Wait();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                bl.FileName = AppConfig.MySqlBulkLoaderLocalCsvFile;
                bl.TableName = "test.BulkLoaderSyncTest";
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
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
            }
        }
        [Fact]
        public void BulkLoadCsvFileNotFound()
        {
            tableSemaphore.Wait();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
                {
                    MySqlBulkLoader bl = new MySqlBulkLoader(connection);
                    bl.FileName = AppConfig.MySqlBulkLoaderCsvFile + "-junk";
                    bl.TableName = "test.BulkLoaderSyncTest";
                    bl.CharacterSet = "UTF8";
                    bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
                    bl.NumberOfLinesToSkip = 1;
                    bl.FieldTerminator = ",";
                    bl.FieldQuotationCharacter = '"';
                    bl.FieldQuotationOptional = true;
                    bl.Expressions.Add("five = UNHEX(five)");
                    bl.Local = false;
                    Assert.Throws<MySqlException>(() =>
                    {
                        int rowCount = bl.Load();
                    });
                }
            }
            finally
            {
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
            }
        }
        [Fact]
        public void BulkLoadLocalCsvFileNotFound()
        {
            tableSemaphore.Wait();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
                {
                    MySqlBulkLoader bl = new MySqlBulkLoader(connection);
                    bl.FileName = AppConfig.MySqlBulkLoaderLocalCsvFile + "-junk";
                    bl.TableName = "test.BulkLoaderSyncTest";
                    bl.CharacterSet = "UTF8";
                    bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
                    bl.NumberOfLinesToSkip = 1;
                    bl.FieldTerminator = ",";
                    bl.FieldQuotationCharacter = '"';
                    bl.FieldQuotationOptional = true;
                    bl.Expressions.Add("five = UNHEX(five)");
                    bl.Local = true;
                    Assert.Throws<MySqlException>(() =>
                    {
                        int rowCount = bl.Load();
                    });
                }
            }
            finally
            {
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
            }
        }
        [Fact]
        public void BulkLoadMissingFileName()
        {
            MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
            bl.TableName = "test.BulkLoaderSyncTest";
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
        [Fact]
        public void BulkLoadMissingTableName()
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

#if BASELINE
        [Fact(Skip = "InfileStream not implemented")]
        public void BulkLoadFileStreamInvalidOperation() {}
#else
        [Fact]
        public void BulkLoadFileStreamInvalidOperation()
        {
            tableSemaphore.Wait();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                using (var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    bl.InfileStream = fileStream;
                    bl.TableName = "test.BulkLoaderSyncTest";
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
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
            }
        }
#endif
#if BASELINE
        [Fact(Skip = "InfileStream not implemented")]
        public void BulkLoadLocalFileStream() {}
#else
        [Fact]
        public void BulkLoadLocalFileStream()
        {
            tableSemaphore.Wait();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                using (var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    bl.InfileStream = fileStream;
                    bl.TableName = "test.BulkLoaderSyncTest";
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
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
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
            tableSemaphore.Wait();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                using (var memoryStream = new MemoryStream(m_memoryStreamBytes, false))
                {
                    bl.InfileStream = memoryStream;
                    bl.TableName = "test.BulkLoaderSyncTest";
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
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
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
            tableSemaphore.Wait();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                using (var memoryStream = new MemoryStream(m_memoryStreamBytes, false))
                {
                    bl.InfileStream = memoryStream;
                    bl.TableName = "test.BulkLoaderSyncTest";
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
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
            }
        }
#endif

        readonly SemaphoreSlim tableSemaphore = new SemaphoreSlim(1, 1); //Use a semaphore to limit access to the load table to one test at a time
        readonly DatabaseFixture m_database;
        readonly string m_initializeTable;
        readonly string m_removeTable;
        readonly byte[] m_memoryStreamBytes;
    }
}
