using System;
using System.Data.Common;
using MySql.Data.MySqlClient;
using Xunit;

// mysql-connector-net will throw SqlNullValueException, which is an exception type related to SQL Server:
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

#if BASELINE
		[Theory(Skip = "Broken by https://bugs.mysql.com/bug.php?id=78917")]
#else
		[Theory]
#endif
		[InlineData("Boolean", new object[] { null, false, true, false, true, true, true })]
		[InlineData("TinyInt1", new object[] { null, false, true, false, true, true, true })]
		public void QueryBoolean(string column, object[] expected)
		{
			DoQuery("bools", column, expected, reader => reader.GetBoolean(0));
		}

		[Theory]
		[InlineData("SByte", new object[] { null, default(sbyte), sbyte.MinValue, sbyte.MaxValue, (sbyte) 123 })]
		public void QuerySByte(string column, object[] expected)
		{
			DoQuery("numbers", column, expected, reader => ((MySqlDataReader) reader).GetSByte(0), baselineCoercedNullValue: default(sbyte));
		}

		[Theory]
		[InlineData("Byte", new object[] { null, default(byte), byte.MinValue, byte.MaxValue, (byte) 123 })]
		public void QueryByte(string column, object[] expected)
		{
			DoQuery("numbers", column, expected, reader => reader.GetByte(0), baselineCoercedNullValue: default(byte));
		}

		[Theory]
		[InlineData("Int32", new object[] { null, default(int), int.MinValue, int.MaxValue, 123456789 })]
		public void QueryInt32(string column, object[] expected)
		{
			DoQuery("numbers", column, expected, reader => reader.GetInt32(0));
		}

		[Theory]
		[InlineData("UInt32", new object[] { null, default(uint), uint.MinValue, uint.MaxValue, 123456789u })]
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

		[Theory]
		[InlineData("Binary", 100)]
		[InlineData("VarBinary", 0)]
		[InlineData("TinyBlob", 0)]
		[InlineData("Blob", 0)]
		[InlineData("MediumBlob", 0)]
		[InlineData("LongBlob", 0)]
		public void QueryBlob(string column, int padLength)
		{
			var data = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xaa, 0xbb, 0xcc, 0xdd, 0xee, 0xff };
			if (data.Length < padLength)
				Array.Resize(ref data, padLength);

			DoQuery<NullReferenceException>("blobs", "`" + column + "`", new object[] { null, data }, GetBytes);
		}

		private static byte[] GetBytes(DbDataReader reader)
		{
			var size = reader.GetBytes(0, 0, null, 0, 0);
			var result = new byte[size];
			reader.GetBytes(0, 0, result, 0, result.Length);
			return result;
		}

		private void DoQuery(string table, string column, object[] expected, Func<DbDataReader, object> getValue, object baselineCoercedNullValue = null)
		{
			DoQuery<GetValueWhenNullException>(table, column, expected, getValue, baselineCoercedNullValue);
		}

		// NOTE: baselineCoercedNullValue is to work around inconsistencies in mysql-connector-net; DBNull.Value will
		// be coerced to 0 by some reader.GetX() methods, but not others.
		private void DoQuery<TException>(string table, string column, object[] expected, Func<DbDataReader, object> getValue, object baselineCoercedNullValue = null)
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
#if BASELINE
							if (baselineCoercedNullValue != null)
								Assert.Equal(baselineCoercedNullValue, getValue(reader));
							else
								Assert.Throws<TException>(() => getValue(reader));
#else
							Assert.Throws<InvalidCastException>(() => getValue(reader));
#endif
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
