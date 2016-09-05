using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using MySql.Data.MySqlClient;
using Xunit;
using static System.FormattableString;

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
		[Theory(Skip = "https://bugs.mysql.com/bug.php?id=78917")]
#else
		[Theory]
#endif
		[InlineData("Boolean", "BOOL", new object[] { null, false, true, false, true, true, true })]
		[InlineData("TinyInt1", "BOOL", new object[] { null, false, true, false, true, true, true })]
		public void QueryBoolean(string column, string dataTypeName, object[] expected)
		{
			DoQuery("bools", column, dataTypeName, expected, reader => reader.GetBoolean(0));
		}

		[Theory]
		[InlineData("SByte", "TINYINT", new object[] { null, default(sbyte), sbyte.MinValue, sbyte.MaxValue, (sbyte) 123 })]
		public void QuerySByte(string column, string dataTypeName, object[] expected)
		{
			DoQuery("integers", column, dataTypeName, expected, reader => ((MySqlDataReader) reader).GetSByte(0), baselineCoercedNullValue: default(sbyte));
		}

		[Theory]
		[InlineData("Byte", "TINYINT", new object[] { null, default(byte), byte.MinValue, byte.MaxValue, (byte) 123 })]
		public void QueryByte(string column, string dataTypeName, object[] expected)
		{
			DoQuery("integers", column, dataTypeName, expected, reader => reader.GetByte(0), baselineCoercedNullValue: default(byte));
		}

		[Theory]
		[InlineData("Int16", "SMALLINT", new object[] { null, default(short), short.MinValue, short.MaxValue, (short) 12345 })]
		public void QueryInt16(string column, string dataTypeName, object[] expected)
		{
			DoQuery("integers", column, dataTypeName, expected, reader => reader.GetInt16(0));
		}

		[Theory]
		[InlineData("UInt16", "SMALLINT", new object[] { null, default(ushort), ushort.MinValue, ushort.MaxValue, (ushort) 12345 })]
		public void QueryUInt16(string column, string dataTypeName, object[] expected)
		{
			DoQuery<InvalidCastException>("integers", column, dataTypeName, expected, reader => reader.GetFieldValue<ushort>(0));
		}

		[Theory]
		[InlineData("Int24", "MEDIUMINT", new object[] { null, default(int), -8388608, 8388607, 1234567 })]
		[InlineData("Int32", "INT", new object[] { null, default(int), int.MinValue, int.MaxValue, 123456789 })]
		public void QueryInt32(string column, string dataTypeName, object[] expected)
		{
			DoQuery("integers", column, dataTypeName, expected, reader => reader.GetInt32(0));
		}

		[Theory]
		[InlineData("UInt24", "MEDIUMINT", new object[] { null, default(uint), 0u, 16777215u, 1234567u })]
		[InlineData("UInt32", "INT", new object[] { null, default(uint), uint.MinValue, uint.MaxValue, 123456789u })]
		public void QueryUInt32(string column, string dataTypeName, object[] expected)
		{
#if BASELINE
			// mysql-connector-int incorrectly returns "INT" for "MEDIUMINT UNSIGNED"
			dataTypeName = "INT";
#endif
			DoQuery<InvalidCastException>("integers", column, dataTypeName, expected, reader => reader.GetFieldValue<uint>(0));
		}

		[Theory]
		[InlineData("Int64", "BIGINT", new object[] { null, default(long), long.MinValue, long.MaxValue, 1234567890123456789 })]
		public void QueryInt64(string column, string dataTypeName, object[] expected)
		{
			DoQuery("integers", column, dataTypeName, expected, reader => reader.GetInt64(0));
		}

		[Theory]
		[InlineData("UInt64", "BIGINT", new object[] { null, default(ulong), ulong.MinValue, ulong.MaxValue, 1234567890123456789u })]
		public void QueryUInt64(string column, string dataTypeName, object[] expected)
		{
			DoQuery<InvalidCastException>("integers", column, dataTypeName, expected, reader => reader.GetFieldValue<ulong>(0));
		}

		[Theory]
		[InlineData("Bit1",  new object[] { null, 0UL, 1UL, 1UL })]
		[InlineData("Bit32",  new object[] { null, 0UL, 1UL, (ulong) uint.MaxValue })]
		[InlineData("Bit64",  new object[] { null, 0UL, 1UL, ulong.MaxValue })]
		public void QueryBits(string column, object[] expected)
		{
			DoQuery<InvalidCastException>("bits", column, "BIT", expected, reader => reader.GetFieldValue<ulong>(0));
		}

		[Theory]
		[InlineData("Single", "FLOAT", new object[] { null, default(float), -3.40282e38f, -1.4013e-45f, 3.40282e38f, 1.4013e-45f })]
		public void QueryFloat(string column, string dataTypeName, object[] expected)
		{
			DoQuery("reals", column, dataTypeName, expected, reader => reader.GetFloat(0));
		}

		[Theory]
		[InlineData("`Double`", "DOUBLE", new object[] { null, default(double), -1.7976931348623157e308, -5e-324, 1.7976931348623157e308, 5e-324 })]
		public void QueryDouble(string column, string dataTypeName, object[] expected)
		{
			DoQuery("reals", column, dataTypeName, expected, reader => reader.GetDouble(0));
		}

		[Theory]
		[InlineData("SmallDecimal", new object[] { null, "0", "-999.99", "-0.01", "999.99", "0.01" })]
		[InlineData("MediumDecimal", new object[] { null, "0", "-999999999999.99999999", "-0.00000001", "999999999999.99999999", "0.00000001" })]
		// value exceeds the range of a decimal and cannot be deserialized
		// [InlineData("BigDecimal", new object[] { null, "0", "-99999999999999999999.999999999999999999999999999999", "-0.000000000000000000000000000001", "99999999999999999999.999999999999999999999999999999", "0.000000000000000000000000000001" })]
		public void QueryDecimal(string column, object[] expected)
		{
			for (int i = 0; i < expected.Length; i++)
				if (expected[i] != null)
					expected[i] = decimal.Parse((string) expected[i], CultureInfo.InvariantCulture);
			DoQuery("reals", column, "DECIMAL", expected, reader => reader.GetDecimal(0));
		}

		[Theory]
		[InlineData("utf8", new[] { null, "", "ASCII", "Ũńıċōđĕ", c_251ByteString })]
		[InlineData("utf8bin", new[] { null, "", "ASCII", "Ũńıċōđĕ", c_251ByteString })]
		[InlineData("latin1", new[] { null, "", "ASCII", "Lãtïñ", c_251ByteString })]
		[InlineData("latin1bin", new[] { null, "", "ASCII", "Lãtïñ", c_251ByteString })]
		[InlineData("cp1251", new[] { null, "", "ASCII", "АБВГабвг", c_251ByteString })]
		public void QueryString(string column, string[] expected)
		{
			DoQuery("strings", column, "VARCHAR", expected, reader => reader.GetString(0));
		}
		const string c_251ByteString = "This string has exactly 251 characters in it. The encoded length is stored as 0xFC 0xFB 0x00. 0xFB (i.e., 251) is the sentinel byte indicating \"this field is null\". Incorrectly interpreting the (decoded) length as the sentinel byte would corrupt data.";

		[Theory]
		[InlineData("guid", "CHAR(36)", new object[] { null, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-c000-000000000046", "fd24a0e8-c3f2-4821-a456-35da2dc4bb8f", "6A0E0A40-6228-11D3-A996-0050041896C8" })]
		[InlineData("guidbin", "CHAR(36)", new object[] { null, "00000000-0000-0000-0000-000000000000", "00000000-0000-0000-c000-000000000046", "fd24a0e8-c3f2-4821-a456-35da2dc4bb8f", "6A0E0A40-6228-11D3-A996-0050041896C8" })]
		public void QueryGuid(string column, string dataTypeName, object[] expected)
		{
			for (int i = 0; i < expected.Length; i++)
				if (expected[i] != null)
					expected[i] = Guid.Parse((string) expected[i]);
#if BASELINE
			DoQuery<MySqlException>("strings", column, dataTypeName, expected, reader => reader.GetGuid(0));
#else
			DoQuery("strings", column, dataTypeName, expected, reader => reader.GetGuid(0));
#endif
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void QueryBinaryGuid(bool oldGuids)
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.OldGuids = oldGuids;
			csb.Database = "datatypes";
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = @"select guidbin from blobs order by rowid;";
					using (var reader = cmd.ExecuteReader())
					{
						Assert.True(reader.Read());
						Assert.Equal(DBNull.Value, reader.GetValue(0));
						Assert.True(reader.Read());
						if (oldGuids)
						{
							var expected = new Guid(0x33221100, 0x5544, 0x7766, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);
							Assert.Equal(expected, reader.GetValue(0));
							Assert.Equal(expected, reader.GetGuid(0));
						}
						else
						{
							var expected = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
							Assert.Equal(expected, GetBytes(reader));
							Assert.Equal(expected, reader.GetValue(0));
						}
						Assert.False(reader.Read());
					}

					cmd.CommandText = @"select guidbin from strings order by rowid;";
					using (var reader = cmd.ExecuteReader())
					{
						Assert.True(reader.Read());
						Assert.Equal(DBNull.Value, reader.GetValue(0));
						Assert.True(reader.Read());
						if (oldGuids)
						{
							Assert.Equal("00000000-0000-0000-0000-000000000000", reader.GetValue(0));
							Assert.Equal("00000000-0000-0000-0000-000000000000", reader.GetString(0));
						}
						else
						{
							Assert.Equal(Guid.Empty, reader.GetValue(0));
							Assert.Equal(Guid.Empty, reader.GetGuid(0));
						}
					}
				}
			}
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task QueryWithGuidParameter(bool oldGuids)
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.OldGuids = oldGuids;
			csb.Database = "datatypes";
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				await connection.OpenAsync().ConfigureAwait(false);
				Assert.Equal(oldGuids? 0L : 1L, (await connection.QueryAsync<long>(@"select count(*) from strings where guid = @guid", new { guid = new Guid("fd24a0e8-c3f2-4821-a456-35da2dc4bb8f") }).ConfigureAwait(false)).SingleOrDefault());
				Assert.Equal(oldGuids ? 0L : 1L, (await connection.QueryAsync<long>(@"select count(*) from strings where guidbin = @guid", new { guid = new Guid("fd24a0e8-c3f2-4821-a456-35da2dc4bb8f") }).ConfigureAwait(false)).SingleOrDefault());
				Assert.Equal(oldGuids ? 1L : 0L, (await connection.QueryAsync<long>(@"select count(*) from blobs where guidbin = @guid", new { guid = new Guid(0x33221100, 0x5544, 0x7766, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF) }).ConfigureAwait(false)).SingleOrDefault());
			}
		}

		[Theory]
		[InlineData("`Date`", "DATE", new object[] { null, "1000 01 01", "9999 12 31", "0001 01 01", "2016 04 05" })]
		[InlineData("`DateTime`", "DATETIME", new object[] { null, "1000 01 01 0 0 0", "9999 12 31 23 59 59 999999", "0001 01 01 0 0 0", "2016 4 5 14 3 4 567890" })]
		[InlineData("`Timestamp`", "TIMESTAMP", new object[] { null, "1970 01 01 0 0 1", "2038 1 18 3 14 7 999999", "0001 01 01 0 0 0", "2016 4 5 14 3 4 567890" })]
		public void QueryDate(string column, string dataTypeName, object[] expected)
		{
			DoQuery("times", column, dataTypeName, ConvertToDateTime(expected), reader => reader.GetDateTime(0));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void QueryZeroDateTime(bool convertZeroDateTime)
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.ConvertZeroDateTime = convertZeroDateTime;
			csb.Database = "datatypes";
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = @"select `Date`, `DateTime`, `Timestamp` from times where `Date` = 0;";
					using (var reader = cmd.ExecuteReader())
					{
						Assert.True(reader.Read());
						if (convertZeroDateTime)
						{
							Assert.Equal(DateTime.MinValue, reader.GetDateTime(0));
							Assert.Equal(DateTime.MinValue, reader.GetDateTime(1));
							Assert.Equal(DateTime.MinValue, reader.GetDateTime(2));
						}
						else
						{
#if BASELINE
							Assert.Throws<MySql.Data.Types.MySqlConversionException>(() => reader.GetDateTime(0));
							Assert.Throws<MySql.Data.Types.MySqlConversionException>(() => reader.GetDateTime(1));
							Assert.Throws<MySql.Data.Types.MySqlConversionException>(() => reader.GetDateTime(2));
#else
							Assert.Throws<InvalidCastException>(() => reader.GetDateTime(0));
							Assert.Throws<InvalidCastException>(() => reader.GetDateTime(1));
							Assert.Throws<InvalidCastException>(() => reader.GetDateTime(2));
#endif
						}
					}
				}
			}
		}

		[Theory]
		[InlineData("`Time`", "TIME", new object[] { null, "-838 -59 -59", "838 59 59", "0 0 0", "0 14 3 4 567890" })]
		public void QueryTime(string column, string dataTypeName, object[] expected)
		{
			DoQuery<InvalidCastException>("times", column, dataTypeName, ConvertToTimeSpan(expected), reader => reader.GetFieldValue<TimeSpan>(0));
		}

		[Theory]
		[InlineData("`Year`", "YEAR", new object[] { null, 1901, 2155, 0, 2016 })]
		public void QueryYear(string column, string dataTypeName, object[] expected)
		{
#if BASELINE
			// mysql-connector-int incorrectly returns "INT" for "YEAR"
			dataTypeName = "INT";
#endif
			DoQuery("times", column, dataTypeName, expected, reader => reader.GetInt32(0));
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

			DoQuery<NullReferenceException>("blobs", "`" + column + "`", "BLOB", new object[] { null, data }, GetBytes);
		}

		[Theory]
		[InlineData("TinyBlob", 255)]
		[InlineData("Blob", 65535)]
#if false
		// MySQL has a default max_allowed_packet size of 4MB; without changing the server configuration, it's impossible
		// to send more than 4MB of data.
		[InlineData("MediumBlob", 16777215)]
		[InlineData("LongBlob", 67108864)]
#endif
		public void InsertLargeBlob(string column, int size)
		{
			var data = new byte[size];
			for (int i = 0; i < data.Length; i++)
				data[i] = (byte) (i % 256);

			long lastInsertId;
			using (var cmd = new MySqlCommand(Invariant($"insert into datatypes.blobs(`{column}`) values(?)"), m_database.Connection)
			{
				Parameters = { new MySqlParameter { Value = data } }
			})
			{
				cmd.ExecuteNonQuery();
				lastInsertId = cmd.LastInsertedId;
			}

			foreach (var queryResult in m_database.Connection.Query<byte[]>(Invariant($"select `{column}` from datatypes.blobs where rowid = {lastInsertId}")))
			{
				Assert.Equal(data, queryResult);
				break;
			}

			m_database.Connection.Execute(Invariant($"delete from datatypes.blobs where rowid = {lastInsertId}"));
		}

		private static byte[] GetBytes(DbDataReader reader)
		{
			var size = reader.GetBytes(0, 0, null, 0, 0);
			var result = new byte[size];
			reader.GetBytes(0, 0, result, 0, result.Length);
			return result;
		}

		private void DoQuery(string table, string column, string dataTypeName, object[] expected, Func<DbDataReader, object> getValue, object baselineCoercedNullValue = null)
		{
			DoQuery<GetValueWhenNullException>(table, column, dataTypeName, expected, getValue, baselineCoercedNullValue);
		}

		// NOTE: baselineCoercedNullValue is to work around inconsistencies in mysql-connector-net; DBNull.Value will
		// be coerced to 0 by some reader.GetX() methods, but not others.
		private void DoQuery<TException>(string table, string column, string dataTypeName, object[] expected, Func<DbDataReader, object> getValue, object baselineCoercedNullValue = null)
			where TException : Exception
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = Invariant($"select {column} from datatypes.{table} order by rowid");
				using (var reader = cmd.ExecuteReader())
				{
					Assert.Equal(dataTypeName, reader.GetDataTypeName(0));
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
							Assert.Equal(value.GetType(), reader.GetFieldType(0));
						}
					}
					Assert.False(reader.Read());
					Assert.False(reader.NextResult());
				}

				// don't perform exact queries for floating-point values; they may fail
				// http://dev.mysql.com/doc/refman/5.7/en/problems-with-float.html
				if (expected.Last() is float)
					return;

				cmd.CommandText = Invariant($"select rowid from datatypes.{table} where {column} = @value");
				var p = cmd.CreateParameter();
				p.ParameterName = "@value";
				p.Value = expected.Last();
				cmd.Parameters.Add(p);
				var result = cmd.ExecuteScalar();
				Assert.Equal(Array.IndexOf(expected, p.Value) + 1, result);
			}
		}

		private static object[] ConvertToDateTime(object[] input)
		{
			var output = new object[input.Length];
			for (int i = 0; i < input.Length; i++)
			{
				var value = SplitAndParse(input[i]);
				if (value?.Length == 3)
					output[i] = new DateTime(value[0], value[1], value[2]);
				else if (value?.Length == 6)
					output[i] = new DateTime(value[0], value[1], value[2], value[3], value[4], value[5]);
				else if (value?.Length == 7)
					output[i] = new DateTime(value[0], value[1], value[2], value[3], value[4], value[5], value[6] / 1000).AddTicks(value[6] % 1000 * 10);
			}
			return output;
		}

		private static object[] ConvertToTimeSpan(object[] input)
		{
			var output = new object[input.Length];
			for (int i = 0; i < input.Length; i++)
			{
				var value = SplitAndParse(input[i]);
				if (value?.Length == 3)
					output[i] = new TimeSpan(value[0], value[1], value[2]);
				else if (value?.Length == 5)
					output[i] = new TimeSpan(value[0], value[1], value[2], value[3], value[4] / 1000) + TimeSpan.FromTicks(value[4] % 1000 * 10);
			}
			return output;
		}

		private static int[] SplitAndParse(object obj)
		{
			var value = obj as string;
			if (value == null)
				return null;

			var split = value.Split();
			var output = new int[split.Length];
			for (int i = 0; i < split.Length; i++)
				output[i] = int.Parse(split[i], CultureInfo.InvariantCulture);
			return output;
		}

		readonly DataTypesFixture m_database;
	}
}
