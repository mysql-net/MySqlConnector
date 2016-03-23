using System;
using Xunit;

namespace SideBySide
{
	public class DataTypes : IClassFixture<DataTypesFixture>
	{
		public DataTypes(DataTypesFixture database)
		{
			m_database = database;
		}

		[Fact]
		public void QueryInt32()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select Int32 from datatypes.numbers order by rowid";
				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal(DBNull.Value, reader.GetValue(0));
					Assert.True(reader.Read());
					Assert.Equal(0, reader.GetValue(0));
					Assert.True(reader.Read());
					Assert.Equal(int.MinValue, reader.GetValue(0));
					Assert.True(reader.Read());
					Assert.Equal(int.MaxValue, reader.GetValue(0));
					Assert.True(reader.Read());
					Assert.Equal(123456789, reader.GetValue(0));
					Assert.False(reader.Read());
					Assert.False(reader.NextResult());
				}
			}
		}

		[Fact]
		public void QueryUInt32()
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = "select UInt32 from datatypes.numbers order by rowid";
				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal(DBNull.Value, reader.GetValue(0));
					Assert.True(reader.Read());
					Assert.Equal(0u, reader.GetValue(0));
					Assert.True(reader.Read());
					Assert.Equal(uint.MinValue, reader.GetValue(0));
					Assert.True(reader.Read());
					Assert.Equal(uint.MaxValue, reader.GetValue(0));
					Assert.True(reader.Read());
					Assert.Equal(123456789u, reader.GetValue(0));
					Assert.False(reader.Read());
					Assert.False(reader.NextResult());
				}
			}
		}

		readonly DataTypesFixture m_database;
	}
}
