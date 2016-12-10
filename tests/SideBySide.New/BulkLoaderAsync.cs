using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;
using Dapper;

namespace SideBySide.New
{
    public class BulkLoaderAsync : IClassFixture<DatabaseFixture>
    {
        public BulkLoaderAsync(DatabaseFixture database)
        {
            m_database = database;
            m_initializeTable = @"
                create schema if not exists test; 
                drop table if exists test.BulkLoaderAsyncTest; 
                CREATE TABLE test.BulkLoaderAsyncTest
                (
	                one int primary key
                    , ignore_one int
                    , two varchar(200)
                    , ignore_two varchar(200)
                    , three varchar(200)
                    , four datetime
                    , five blob
                ) CHARACTER SET = UTF8;";
            m_removeTable = "drop table if exists test.BulkLoaderAsyncTest;";
            m_memoryStreamBytes = System.Text.Encoding.UTF8.GetBytes(@"1,'two-1','three-1'
2,'two-2','three-2'
3,'two-3','three-3'
4,'two-4','three-4'
5,'two-5','three-5'
");
        }

        [Fact]
        public async void BulkLoadCsvFile()
        {
            await tableSemaphore.WaitAsync();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                bl.FileName = AppConfig.MySqlBulkLoaderCsvFile;
                bl.TableName = "test.BulkLoaderAsyncTest";
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
            finally
            {
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
            }
        }
        [Fact]
        public async void BulkLoadLocalCsvFile()
        {
            await tableSemaphore.WaitAsync();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                bl.FileName = AppConfig.MySqlBulkLoaderLocalCsvFile;
                bl.TableName = "test.BulkLoaderAsyncTest";
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
                if (AppConfig.MySqlBulkLoaderRemoveTables) m_database.Connection.Execute(m_removeTable);
                tableSemaphore.Release();
            }
        }
        [Fact]
        public async void BulkLoadCsvFileNotFound()
        {
            await tableSemaphore.WaitAsync();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
                {
                    MySqlBulkLoader bl = new MySqlBulkLoader(connection);
                    bl.FileName = AppConfig.MySqlBulkLoaderCsvFile + "-junk";
                    bl.TableName = "test.BulkLoaderAsyncTest";
                    bl.CharacterSet = "UTF8";
                    bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
                    bl.NumberOfLinesToSkip = 1;
                    bl.FieldTerminator = ",";
                    bl.FieldQuotationCharacter = '"';
                    bl.FieldQuotationOptional = true;
                    bl.Expressions.Add("five = UNHEX(five)");
                    bl.Local = false;
                    await Assert.ThrowsAsync<MySqlException>(async () =>
                    {
                        int rowCount = await bl.LoadAsync();
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
        public async void BulkLoadLocalCsvFileNotFound()
        {
            await tableSemaphore.WaitAsync();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
                {
                    MySqlBulkLoader bl = new MySqlBulkLoader(connection);
                    bl.FileName = AppConfig.MySqlBulkLoaderLocalCsvFile + "-junk";
                    bl.TableName = "test.BulkLoaderAsyncTest";
                    bl.CharacterSet = "UTF8";
                    bl.Columns.AddRange(new string[] { "one", "two", "three", "four", "five" });
                    bl.NumberOfLinesToSkip = 1;
                    bl.FieldTerminator = ",";
                    bl.FieldQuotationCharacter = '"';
                    bl.FieldQuotationOptional = true;
                    bl.Expressions.Add("five = UNHEX(five)");
                    bl.Local = true;
                    await Assert.ThrowsAsync<MySqlException>(async () =>
                    {
                        int rowCount = await bl.LoadAsync();
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
        public async void BulkLoadMissingFileName()
        {
            MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
            bl.TableName = "test.BulkLoaderAsyncTest";
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
        [Fact]
        public async void BulkLoadMissingTableName()
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
        [Fact]
        public async void BulkLoadFileStreamInvalidOperation()
        {
            await tableSemaphore.WaitAsync();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                using (var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    bl.InfileStream = fileStream;
                    bl.TableName = "test.BulkLoaderAsyncTest";
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
        public async void BulkLoadLocalFileStream()
        {
            await tableSemaphore.WaitAsync();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                using (var fileStream = new FileStream(AppConfig.MySqlBulkLoaderLocalCsvFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    bl.InfileStream = fileStream;
                    bl.TableName = "test.BulkLoaderAsyncTest";
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
        public async void BulkLoadMemoryStreamInvalidOperation()
        {
            await tableSemaphore.WaitAsync();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                using (var memoryStream = new MemoryStream(m_memoryStreamBytes, false))
                {
                    bl.InfileStream = memoryStream;
                    bl.TableName = "test.BulkLoaderAsyncTest";
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
        public async void BulkLoadLocalMemoryStream()
        {
            await tableSemaphore.WaitAsync();
            try
            {
                m_database.Connection.Execute(m_initializeTable);

                MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
                using (var memoryStream = new MemoryStream(m_memoryStreamBytes, false))
                {
                    bl.InfileStream = memoryStream;
                    bl.TableName = "test.BulkLoaderAsyncTest";
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
