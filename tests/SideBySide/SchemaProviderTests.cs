using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
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

		[Theory]
		[InlineData("ReservedWords")]
		[InlineData("RESERVEDWORDS")]
		[InlineData("reservedwords")]
		public void ReservedWordsSchema(string schemaName)
		{
			var table = m_database.Connection.GetSchema(schemaName);
			Assert.NotNull(table);
			Assert.Single(table.Columns);
			Assert.Equal("ReservedWord", table.Columns[0].ColumnName);
			Assert.Contains("CREATE", table.Rows.Cast<DataRow>().Select(x => (string) x[0]));
		}

		[Fact]
		public void ColumnsSchema()
		{
			var table = m_database.Connection.GetSchema("Columns");
			Assert.NotNull(table);
			AssertHasColumn("TABLE_CATALOG", typeof(string));
			AssertHasColumn("TABLE_SCHEMA", typeof(string));
			AssertHasColumn("TABLE_NAME", typeof(string));
			AssertHasColumn("COLUMN_NAME", typeof(string));
			AssertHasColumn("ORDINAL_POSITION", typeof(uint));
			AssertHasColumn("COLUMN_DEFAULT", typeof(string));
			AssertHasColumn("IS_NULLABLE", typeof(string));
			AssertHasColumn("DATA_TYPE", typeof(string));
			AssertHasColumn("CHARACTER_MAXIMUM_LENGTH", typeof(long));
			AssertHasColumn("NUMERIC_PRECISION", typeof(ulong));
			AssertHasColumn("NUMERIC_SCALE", typeof(ulong));
			AssertHasColumn("DATETIME_PRECISION", typeof(uint));
			AssertHasColumn("CHARACTER_SET_NAME", typeof(string));
			AssertHasColumn("COLLATION_NAME", typeof(string));
			AssertHasColumn("COLUMN_KEY", typeof(string));
			AssertHasColumn("EXTRA", typeof(string));
			AssertHasColumn("PRIVILEGES", typeof(string));
			AssertHasColumn("COLUMN_COMMENT", typeof(string));

			void AssertHasColumn(string name, Type type)
			{
				var column = table.Columns[name];
				Assert.NotNull(column);

				// allow integral types with a larger positive range
				if (type == typeof(int))
					Assert.True(type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong));
				else if (type == typeof(uint))
					Assert.True(type == typeof(uint) || type == typeof(long) || type == typeof(ulong));
				else if (type == typeof(long))
					Assert.True(type == typeof(long) || type == typeof(ulong));
				else
					Assert.Equal(type, column.DataType);
			}
		}

#if !BASELINE
		[Fact]
		public async Task GetMetaDataCollectionsSchemaAsync()
		{
			var table = await m_database.Connection.GetSchemaAsync();
			Assert.Equal(3, table.Columns.Count);
			Assert.Equal("CollectionName", table.Columns[0].ColumnName);
		}

		[Fact]
		public async Task GetCharacterSetsSchemaAsync()
		{
			var table = await m_database.Connection.GetSchemaAsync("CharacterSets");
			Assert.Equal(4, table.Columns.Count);
			Assert.Contains("latin1", table.Rows.Cast<DataRow>().Select(x => (string) x[0]));
			Assert.Contains("utf8", table.Rows.Cast<DataRow>().Select(x => (string) x[0]));
		}

		[Fact]
		public void GetCollationsSchema()
		{
			var table = m_database.Connection.GetSchema("Collations");
			Assert.Contains("latin1_general_ci", table.Rows.Cast<DataRow>().Select(x => (string) x[0]));
			Assert.Contains("utf8_bin", table.Rows.Cast<DataRow>().Select(x => (string) x[0]));
		}
#endif

		readonly DatabaseFixture m_database;
	}
}
