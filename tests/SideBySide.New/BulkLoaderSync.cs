using System.Data;
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
            m_database.Connection.Execute(@"create schema if not exists test; drop table if exists test.SimpleCSV; CREATE TABLE test.SimpleCSV
(
	one int primary key
    , two varchar(100)
    , three varchar(100)
);");
        }

        [Fact]
        public void LoadSimpleCsv()
        {
            MySqlBulkLoader bl = new MySqlBulkLoader(m_database.Connection);
            bl.FileName = @"E:\development\MySqlOfficialTest\TestData\Simple.csv";
            bl.TableName = "test.SimpleCSV";
            bl.NumberOfLinesToSkip = 1;
            bl.FieldTerminator = ",";
            bl.Local = true;
            int rowCount = bl.Load();
            Assert.True(rowCount == 4);
        }
        readonly DatabaseFixture m_database;
    }
}
