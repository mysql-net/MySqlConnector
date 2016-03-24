using System;
using System.Data.Common;
using Xunit;

// Baseline MySql.Data will throw SqlNullValueException, which is an exception type related to SQL Server:
// "The exception that is thrown when the Value property of a System.Data.SqlTypes structure is set to null."
// However, DbDataReader.GetString etc. are documented as throwing InvalidCastException: https://msdn.microsoft.com/en-us/library/system.data.common.dbdatareader.getstring.aspx
// Additionally, that is what DbDataReader.GetFieldValue<T> throws. For consistency, we prefer InvalidCastException.
#if BASELINE
using GetValueWhenNullException = System.Data.SqlTypes.SqlNullValueException;
#else
using GetValueWhenNullException = System.InvalidCastException;
#endif

namespace SideBySide
{
	public class DataTypes : IClassFixture<DataTypesFixture>
	{
		public DataTypes(DataTypesFixture database)
		{
			m_database = database;
		}

		[Theory]
		[InlineData("Int32", new object[] { null, 0, int.MinValue, int.MaxValue, 123456789 })]
		public void QueryInt32(string column, object[] expected)
		{
			DoQuery("numbers", column, expected, reader => reader.GetInt32(0));
		}

		[Theory]
		[InlineData("UInt32", new object[] { null, 0u, uint.MinValue, uint.MaxValue, 123456789u })]
		public void QueryUInt32(string column, object[] expected)
		{
			DoQuery<InvalidCastException>("numbers", column, expected, reader => reader.GetFieldValue<uint>(0));
		}

		[Theory]
		[InlineData("utf8", new[] { null, "", "ASCII", "Ũńıċōđĕ" })]
		[InlineData("latin1", new[] { null, "", "ASCII", "Lãtïñ" })]
		[InlineData("cp1251", new[] { null, "", "ASCII", "АБВГабвг" })]
		public void QueryString(string column, string[] expected)
		{
			DoQuery("strings", column, expected, reader => reader.GetString(0));
		}

		private void DoQuery(string table, string column, object[] expected, Func<DbDataReader, object> getValue)
		{
			DoQuery<GetValueWhenNullException>(table, column, expected, getValue);
		}

		private void DoQuery<TException>(string table, string column, object[] expected, Func<DbDataReader, object> getValue)
			where TException : Exception
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = $"select {column} from datatypes.{table} order by rowid";
				using (var reader = cmd.ExecuteReader())
				{
					foreach (var value in expected)
					{
						Assert.True(reader.Read());
						if (value == null)
						{
							Assert.Equal(DBNull.Value, reader.GetValue(0));
							Assert.Throws<TException>(() => getValue(reader));
						}
						else
						{
							Assert.Equal(value, reader.GetValue(0));
							Assert.Equal(value, getValue(reader));
						}
					}
					Assert.False(reader.Read());
					Assert.False(reader.NextResult());
				}
			}
		}

		readonly DataTypesFixture m_database;
	}
}
