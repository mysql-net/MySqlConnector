#if !NETCOREAPP1_1_2
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using Xunit;

namespace SideBySide
{
	public class SchemaProviderTests : IClassFixture<DatabaseFixture>, IDisposable
	{
		public SchemaProviderTests(DatabaseFixture database)
		{
			m_database = database;
			m_database.Connection.Open();
		}

		public void Dispose()
		{
			m_database.Connection.Close();
		}

		[Fact]
		public void GetDataSourceInformationSchemaCollection()
		{
			var dataTable = m_database.Connection.GetSchema(DbMetaDataCollectionNames.DataSourceInformation);
			Assert.Equal(m_database.Connection.ServerVersion, dataTable.Rows[0]["DataSourceProductVersion"]);
		}

		[Fact]
		public void ReservedWordsSchema()
		{
			var table = m_database.Connection.GetSchema("ReservedWords");
			Assert.NotNull(table);
			Assert.Single(table.Columns);
			Assert.Equal("ReservedWord", table.Columns[0].ColumnName);
#if !BASELINE
			// https://bugs.mysql.com/bug.php?id=89639
			Assert.Contains("CREATE", table.Rows.Cast<DataRow>().Select(x => (string) x[0]));
#endif
		}

		readonly DatabaseFixture m_database;
	}
}
#endif
