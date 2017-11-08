using System.Data;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ParameterTests
	{
		[Theory]
		[InlineData(DbType.Byte, MySqlDbType.UByte)]
		[InlineData(DbType.SByte, MySqlDbType.Byte)]
		[InlineData(DbType.Int16, MySqlDbType.Int16)]
		[InlineData(DbType.UInt16, MySqlDbType.UInt16)]
		[InlineData(DbType.Int64, MySqlDbType.Int64)]
		[InlineData(DbType.Single, MySqlDbType.Float)]
		[InlineData(DbType.Double, MySqlDbType.Double)]
		[InlineData(DbType.Guid, MySqlDbType.Guid)]
		public void DbTypeToMySqlDbType(DbType dbType, MySqlDbType mySqlDbType)
		{
			var parameter = new MySqlParameter { DbType = dbType };
			Assert.Equal(dbType, parameter.DbType);
			Assert.Equal(mySqlDbType, parameter.MySqlDbType);

			parameter = new MySqlParameter { MySqlDbType = mySqlDbType };
			Assert.Equal(mySqlDbType, parameter.MySqlDbType);
			Assert.Equal(dbType, parameter.DbType);
		}

		[Theory]
		[InlineData(new[] { DbType.StringFixedLength, DbType.AnsiStringFixedLength }, new[] { MySqlDbType.String })]
		[InlineData(new[] { DbType.Int32 }, new[] { MySqlDbType.Int32, MySqlDbType.Int24 })]
		[InlineData(new[] { DbType.UInt32 }, new[] { MySqlDbType.UInt32, MySqlDbType.UInt24 })]
		[InlineData(new[] { DbType.UInt64 }, new[] { MySqlDbType.UInt64, MySqlDbType.Bit })]
		[InlineData(new[] { DbType.DateTime }, new[] { MySqlDbType.DateTime, MySqlDbType.Timestamp })]
		[InlineData(new[] { DbType.Date }, new[] { MySqlDbType.Date, MySqlDbType.Newdate })]
#if !BASELINE
		[InlineData(new[] { DbType.Int32 }, new[] { MySqlDbType.Int32, MySqlDbType.Year })]
		[InlineData(new[] { DbType.Binary }, new[] { MySqlDbType.Blob, MySqlDbType.Binary, MySqlDbType.TinyBlob, MySqlDbType.MediumBlob, MySqlDbType.LongBlob, MySqlDbType.Geometry })]
		[InlineData(new[] { DbType.String, DbType.AnsiString, DbType.Xml },
			new[] { MySqlDbType.VarChar, MySqlDbType.VarString, MySqlDbType.Text, MySqlDbType.TinyText, MySqlDbType.MediumText, MySqlDbType.LongText, MySqlDbType.JSON, MySqlDbType.Enum, MySqlDbType.Set })]
		[InlineData(new[] { DbType.Decimal, DbType.Currency }, new[] { MySqlDbType.NewDecimal, MySqlDbType.Decimal })]
#else
		[InlineData(new[] { DbType.Decimal, DbType.Currency }, new[] { MySqlDbType.Decimal, MySqlDbType.NewDecimal })]
#endif
		public void DbTypesToMySqlDbTypes(DbType[] dbTypes, MySqlDbType[] mySqlDbTypes)
		{
			foreach (var dbType in dbTypes)
			{
				var parameter = new MySqlParameter { DbType = dbType };
				Assert.Equal(dbType, parameter.DbType);
				Assert.Equal(mySqlDbTypes[0], parameter.MySqlDbType);
			}

			foreach (var mySqlDbType in mySqlDbTypes)
			{
				var parameter = new MySqlParameter { MySqlDbType = mySqlDbType };
				Assert.Equal(mySqlDbType, parameter.MySqlDbType);
				Assert.Equal(dbTypes[0], parameter.DbType);
			}

		}
	}
}
