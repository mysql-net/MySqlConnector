using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;
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

		[Theory]
		[InlineData("SByte", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, true, true, true })]
		[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, false, true, true })]
		[InlineData("Int16", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, true, true, true })]
		[InlineData("UInt16", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, false, true, true })]
		[InlineData("Int24", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, true, true, true })]
		[InlineData("UInt24", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, false, true, true })]
		[InlineData("Int32", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, true, true, true })]
		[InlineData("UInt32", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, false, true, true })]
		[InlineData("Int64", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, true, true, true })]
		[InlineData("UInt64", new[] { 1, 0, 0, 0, 0 }, new[] { false, false, false, true, true })]
		public async Task GetBoolean(string column, int[] flags, bool[] values)
		{
			await DoGetValue(column, (r, n) => r.GetBoolean(n), flags, values).ConfigureAwait(false);
		}

		[Theory]
		[InlineData("SByte", new[] { 1, 0, 0, 0, 0 }, new short[] { 0, 0, -128, 127, 123 })]
		[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new short[] { 0, 0, 0, 255, 123 })]
		[InlineData("Int16", new[] { 1, 0, 0, 0, 0 }, new short[] { 0, 0, -32768, 32767, 12345 })]
		[InlineData("UInt16", new[] { 1, 0, 0, 2, 0 }, new short[] { 0, 0, 0, 0, 12345 })]
		[InlineData("Int24", new[] { 1, 0, 2, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
		[InlineData("UInt24", new[] { 1, 0, 0, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
		[InlineData("Int32", new[] { 1, 0, 2, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
		[InlineData("UInt32", new[] { 1, 0, 0, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
		[InlineData("Int64", new[] { 1, 0, 2, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
		[InlineData("UInt64", new[] { 1, 0, 0, 2, 2 }, new short[] { 0, 0, 0, 0, 0 })]
		public async Task GetInt16(string column, int[] flags, short[] values)
		{
			await DoGetValue(column, (r, n) => r.GetInt16(n), flags, values).ConfigureAwait(false);
		}

		[Theory]
		[InlineData("SByte", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, -128, 127, 123 })]
		[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, 0, 255, 123 })]
		[InlineData("Int16", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, -32768, 32767, 12345 })]
		[InlineData("UInt16", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, 0, 65535, 12345 })]
		[InlineData("Int24", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, -8388608, 8388607, 1234567 })]
		[InlineData("UInt24", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, 0, 16777215, 1234567 })]
		[InlineData("Int32", new[] { 1, 0, 0, 0, 0 }, new[] { 0, 0, -2147483648, 2147483647, 123456789 })]
		[InlineData("UInt32", new[] { 1, 0, 0, 2, 0 }, new[] { 0, 0, 0, 0, 123456789 })]
		[InlineData("Int64", new[] { 1, 0, 2, 2, 2 }, new[] { 0, 0, 0, 0, 0 })]
		[InlineData("UInt64", new[] { 1, 0, 0, 2, 2 }, new[] { 0, 0, 0, 0, 0 })]
		public async Task GetInt32(string column, int[] flags, int[] values)
		{
			await DoGetValue(column, (r, n) => r.GetInt32(n), flags, values).ConfigureAwait(false);
		}

		[Theory]
		[InlineData("SByte", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, -128, 127, 123 })]
		[InlineData("Byte", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, 0, 255, 123 })]
		[InlineData("Int16", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, -32768, 32767, 12345 })]
		[InlineData("UInt16", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, 0, 65535, 12345 })]
		[InlineData("Int24", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, -8388608, 8388607, 1234567 })]
		[InlineData("UInt24", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, 0, 16777215, 1234567 })]
		[InlineData("Int32", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, -2147483648, 2147483647, 123456789 })]
		[InlineData("UInt32", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, 0, 4294967295L, 123456789 })]
		[InlineData("Int64", new[] { 1, 0, 0, 0, 0 }, new[] { 0L, 0, -9223372036854775808, 9223372036854775807, 1234567890123456789 })]
		[InlineData("UInt64", new[] { 1, 0, 0, 2, 0 }, new[] { 0L, 0, 0, 0, 1234567890123456789 })]
		public async Task GetInt64(string column, int[] flags, long[] values)
		{
			await DoGetValue(column, (r, n) => r.GetInt64(n), flags, values).ConfigureAwait(false);
		}

		public async Task DoGetValue<T>(string column, Func<DbDataReader, int, T> getInt, int[] flags, T[] values)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = Invariant($"select {column} from datatypes_integers order by rowid");
				using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
				{
					for (int i = 0; i < flags.Length; i++)
					{
						Assert.True(await reader.ReadAsync().ConfigureAwait(false));
						switch (flags[i])
						{
						case 0: // normal
							Assert.Equal(values[i], getInt(reader, 0));
							break;

						case 1: // null
							Assert.True(await reader.IsDBNullAsync(0).ConfigureAwait(false));
							break;

						case 2: // overflow
							Assert.Throws<OverflowException>(() => getInt(reader, 0));
							break;
						}
					}
					Assert.False(await reader.ReadAsync().ConfigureAwait(false));
					Assert.False(await reader.NextResultAsync().ConfigureAwait(false));
				}
			}
		}

		[Theory]
		[InlineData("size", "ENUM", new object[] { null, "small", "medium" })]
		[InlineData("color", "ENUM", new object[] { "red", "orange", "green" })]
		public void QueryEnum(string column, string dataTypeName, object[] expected)
		{
#if BASELINE
			// mysql-connector-net incorrectly returns "VARCHAR" for "ENUM"
			dataTypeName = "VARCHAR";
#endif
			DoQuery("enums", column, dataTypeName, expected, reader => reader.GetString(0));
		}

		[Theory]
		[InlineData("value", "SET", new object[] { null, "", "one", "two", "one,two", "four", "one,four", "two,four", "one,two,four" })]
		public void QuerySet(string column, string dataTypeName, object[] expected)
		{
#if BASELINE
			// mysql-connector-net incorrectly returns "VARCHAR" for "ENUM"
			dataTypeName = "VARCHAR";
#endif
			DoQuery("set", column, dataTypeName, expected, reader => reader.GetString(0));
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
		[InlineData("TinyInt1", "TINYINT", new object[] { null, (sbyte)0, (sbyte)1, (sbyte)0, (sbyte)1, (sbyte)-1, (sbyte)123 })]
		[InlineData("Boolean", "TINYINT", new object[] { null, (sbyte)0, (sbyte)1, (sbyte)0, (sbyte)1, (sbyte)-1, (sbyte)123 })]
		public void QueryTinyIntSbyte(string column, string dataTypeName, object[] expected)
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.TreatTinyAsBoolean = false;
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				DoQuery("bools", column, dataTypeName, expected, reader => ((MySqlDataReader) reader).GetSByte(0), baselineCoercedNullValue: default(sbyte), connection: connection);
			}
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
			// mysql-connector-net incorrectly returns "INT" for "MEDIUMINT UNSIGNED"
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
			// don't perform exact queries for floating-point values; they may fail: http://dev.mysql.com/doc/refman/5.7/en/problems-with-float.html
			DoQuery("reals", column, dataTypeName, expected, reader => reader.GetFloat(0), omitWhereTest: true);
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
			DoQuery<MySqlException>("strings", column, dataTypeName, expected, reader => reader.GetGuid(0));
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void QueryBinaryGuid(bool oldGuids)
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.OldGuids = oldGuids;
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = @"select guidbin from datatypes_blobs order by rowid;";
					using (var reader = cmd.ExecuteReader())
					{
						Assert.True(reader.Read());
						Assert.Equal(DBNull.Value, reader.GetValue(0));
						Assert.True(reader.Read());
						var expectedBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
						var expectedGuid = new Guid(expectedBytes);
						if (oldGuids)
						{
							Assert.Equal(typeof(Guid), reader.GetFieldType(0));
							Assert.Equal(expectedGuid, reader.GetValue(0));
						}
						else
						{
							Assert.Equal(typeof(byte[]), reader.GetFieldType(0));
							Assert.Equal(expectedBytes, reader.GetValue(0));
						}
						Assert.Equal(expectedGuid, reader.GetGuid(0));
						Assert.Equal(expectedBytes, GetBytes(reader));
						Assert.False(reader.Read());
					}

					cmd.CommandText = @"select guidbin from datatypes_strings order by rowid;";
					using (var reader = cmd.ExecuteReader())
					{
						Assert.True(reader.Read());
						Assert.Equal(DBNull.Value, reader.GetValue(0));
						Assert.True(reader.Read());
						if (oldGuids)
						{
							Assert.Equal(typeof(string), reader.GetFieldType(0));
							Assert.Equal("00000000-0000-0000-0000-000000000000", reader.GetValue(0));
							Assert.Equal("00000000-0000-0000-0000-000000000000", reader.GetString(0));
						}
						else
						{
							Assert.Equal(typeof(Guid), reader.GetFieldType(0));
							Assert.Equal(Guid.Empty, reader.GetValue(0));
						}
						Assert.Equal(Guid.Empty, reader.GetGuid(0));
					}
				}
			}
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public async Task QueryWithGuidParameter(bool oldGuids)
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.OldGuids = oldGuids;
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				await connection.OpenAsync().ConfigureAwait(false);
				Assert.Equal(oldGuids? 0L : 1L, (await connection.QueryAsync<long>(@"select count(*) from datatypes_strings where guid = @guid", new { guid = new Guid("fd24a0e8-c3f2-4821-a456-35da2dc4bb8f") }).ConfigureAwait(false)).SingleOrDefault());
				Assert.Equal(oldGuids ? 0L : 1L, (await connection.QueryAsync<long>(@"select count(*) from datatypes_strings where guidbin = @guid", new { guid = new Guid("fd24a0e8-c3f2-4821-a456-35da2dc4bb8f") }).ConfigureAwait(false)).SingleOrDefault());
				Assert.Equal(oldGuids ? 1L : 0L, (await connection.QueryAsync<long>(@"select count(*) from datatypes_blobs where guidbin = @guid", new { guid = new Guid(0x33221100, 0x5544, 0x7766, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF) }).ConfigureAwait(false)).SingleOrDefault());
			}
		}

		[Theory]
		[InlineData("char38", typeof(string))]
		[InlineData("char38bin", typeof(string))]
		[InlineData("text", typeof(string))]
		[InlineData("blob", typeof(byte[]))]
		public async Task GetGuid(string column, Type fieldType)
		{
			using (var cmd = m_database.Connection.CreateCommand())
			{
				cmd.CommandText = Invariant($"select `{column}` from datatypes_guids order by rowid");
				using (var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false))
				{
					Assert.Equal(fieldType, reader.GetFieldType(0));

					Assert.True(await reader.ReadAsync().ConfigureAwait(false));
					Assert.True(reader.IsDBNull(0));
					Assert.Throws<MySqlException>(() => reader.GetGuid(0));

					Assert.True(await reader.ReadAsync().ConfigureAwait(false));
					Assert.False(reader.IsDBNull(0));
					Assert.NotNull(reader.GetValue(0));
					Assert.IsType(fieldType, reader.GetValue(0));

					Type exceptionType = typeof(MySqlException);
#if BASELINE
					// baseline throws FormatException when conversion from string fails
					if (fieldType == typeof(string))
						exceptionType = typeof(FormatException);
#endif
					Assert.Throws(exceptionType, () => reader.GetGuid(0));

					Assert.True(await reader.ReadAsync().ConfigureAwait(false));
					Assert.NotNull(reader.GetValue(0));
					Assert.IsType(fieldType, reader.GetValue(0));
					Assert.Equal(new Guid("33221100-5544-7766-8899-aabbccddeeff"), reader.GetGuid(0));

					Assert.True(await reader.ReadAsync().ConfigureAwait(false));
					Assert.NotNull(reader.GetValue(0));
					Assert.IsType(fieldType, reader.GetValue(0));
					Assert.Equal(new Guid("33221100-5544-7766-8899-aabbccddeeff"), reader.GetGuid(0));

					Assert.False(await reader.ReadAsync().ConfigureAwait(false));
				}
			}
		}

		[Theory]
		[InlineData("`Date`", "DATE", new object[] { null, "1000 01 01", "9999 12 31", null, "2016 04 05" })]
		[InlineData("`DateTime`", "DATETIME", new object[] { null, "1000 01 01 0 0 0", "9999 12 31 23 59 59 999999", null, "2016 4 5 14 3 4 567890" })]
		[InlineData("`Timestamp`", "TIMESTAMP", new object[] { null, "1970 01 01 0 0 1", "2038 1 18 3 14 7 999999", null, "2016 4 5 14 3 4 567890" })]
		public void QueryDate(string column, string dataTypeName, object[] expected)
		{
			DoQuery("times", column, dataTypeName, ConvertToDateTime(expected), reader => reader.GetDateTime(0));
#if !BASELINE
			DoQuery("times", column, dataTypeName, ConvertToDateTimeOffset(expected), reader => (reader as MySqlDataReader).GetDateTimeOffset(0), matchesDefaultType: false);
#endif
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void QueryZeroDateTime(bool convertZeroDateTime)
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.ConvertZeroDateTime = convertZeroDateTime;
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				using (var cmd = connection.CreateCommand())
				{
					cmd.CommandText = @"select cast(0 as date), cast(0 as datetime);";
					using (var reader = cmd.ExecuteReader() as MySqlDataReader)
					{
						Assert.True(reader.Read());
						if (convertZeroDateTime)
						{
							Assert.Equal(DateTime.MinValue, reader.GetDateTime(0));
							Assert.Equal(DateTime.MinValue, reader.GetDateTime(1));
#if !BASELINE
							Assert.Equal(DateTimeOffset.MinValue, reader.GetDateTimeOffset(0));
							Assert.Equal(DateTimeOffset.MinValue, reader.GetDateTimeOffset(1));
#endif
						}
						else
						{
#if BASELINE
							Assert.Throws<MySql.Data.Types.MySqlConversionException>(() => reader.GetDateTime(0));
							Assert.Throws<MySql.Data.Types.MySqlConversionException>(() => reader.GetDateTime(1));
#else
							Assert.Throws<InvalidCastException>(() => reader.GetDateTime(0));
							Assert.Throws<InvalidCastException>(() => reader.GetDateTime(1));
							Assert.Throws<InvalidCastException>(() => reader.GetDateTimeOffset(0));
							Assert.Throws<InvalidCastException>(() => reader.GetDateTimeOffset(1));
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
			// mysql-connector-net incorrectly returns "INT" for "YEAR"
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

#if BASELINE
			DoQuery<NullReferenceException>("blobs", "`" + column + "`", "BLOB", new object[] { null, data }, GetBytes);
#else
			DoQuery<InvalidCastException>("blobs", "`" + column + "`", "BLOB", new object[] { null, data }, GetBytes);
#endif
		}

		[Theory]
		[InlineData("TinyBlob", 255)]
		[InlineData("Blob", 65535)]
		[InlineData("MediumBlob", 16777215)]
		[InlineData("LongBlob", 33554432)]
		[InlineData("LongBlob", 67108864)]
		public async Task InsertLargeBlobAsync(string column, int size)
		{
			// NOTE: MySQL Server will reset the connection when it receives an oversize packet, so we need to create a test-specific connection here
			using (var connection = new MySqlConnection(AppConfig.CreateConnectionStringBuilder().ConnectionString))
			{
				await connection.OpenAsync();
				var transaction = await connection.BeginTransactionAsync();

				// verify that this amount of data can be sent to MySQL successfully
				var maxAllowedPacket = (await connection.QueryAsync<int>("select @@max_allowed_packet").ConfigureAwait(false)).Single();
				var shouldFail = maxAllowedPacket < size + 100;

				var data = CreateByteArray(size);

				long lastInsertId;
				using (var cmd = new MySqlCommand(Invariant($"insert into datatypes_blobs(`{column}`) values(?)"), connection, transaction)
				{
					Parameters = { new MySqlParameter { Value = data } }
				})
				{
					try
					{
						await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
						lastInsertId = cmd.LastInsertedId;
						Assert.False(shouldFail);
					}
					catch (MySqlException ex)
					{
						lastInsertId = -1;
						Assert.True(shouldFail);
						Assert.Contains("packet", ex.Message);
					}
				}

				if (!shouldFail)
				{
					var queryResult = (await connection.QueryAsync<byte[]>(Invariant($"select `{column}` from datatypes_blobs where rowid = {lastInsertId}")).ConfigureAwait(false)).Single();
					TestUtilities.AssertEqual(data, queryResult);

					await connection.ExecuteAsync(Invariant($"delete from datatypes_blobs where rowid = {lastInsertId}")).ConfigureAwait(false);
				}
			}
		}

		[Theory]
		[InlineData("TinyBlob", 255)]
		[InlineData("Blob", 65535)]
		[InlineData("MediumBlob", 16777215)]
		[InlineData("LongBlob", 67108864)]
		public void InsertLargeBlobSync(string column, int size)
		{
			// NOTE: MySQL Server will reset the connection when it receives an oversize packet, so we need to create a test-specific connection here
			using (var connection = new MySqlConnection(AppConfig.CreateConnectionStringBuilder().ConnectionString))
			{
				connection.Open();
				var transaction = connection.BeginTransaction();

				// verify that this amount of data can be sent to MySQL successfully
				var maxAllowedPacket = m_database.Connection.Query<int>("select @@max_allowed_packet").Single();
				var shouldFail = maxAllowedPacket < size + 100;

				var data = CreateByteArray(size);

				long lastInsertId;
				using (var cmd = new MySqlCommand(Invariant($"insert into datatypes_blobs(`{column}`) values(?)"), connection, transaction)
				{
					Parameters = { new MySqlParameter { Value = data } }
				})
				{
					try
					{
						cmd.ExecuteNonQuery();
						lastInsertId = cmd.LastInsertedId;
					}
					catch (MySqlException ex)
					{
						lastInsertId = -1;
						Assert.True(shouldFail);
						Assert.Contains("packet", ex.Message);
					}
				}

				if (!shouldFail)
				{
					var queryResult = connection.Query<byte[]>(Invariant($"select `{column}` from datatypes_blobs where rowid = {lastInsertId}")).Single();
					TestUtilities.AssertEqual(data, queryResult);

					connection.Execute(Invariant($"delete from datatypes_blobs where rowid = {lastInsertId}"));
				}
			}
		}

		private static byte[] CreateByteArray(int size)
		{
			var data = new byte[size];
			Random random = new Random(size);
			random.NextBytes(data);

			// ensure each byte value is used at least once
			for (int i = 0; i < Math.Min(255, size); i++)
				data[i] = (byte) i;

			return data;
		}

		[JsonTheory]
		[InlineData("Value", new[] { null, "NULL", "BOOLEAN", "ARRAY", "ARRAY", "ARRAY", "INTEGER", "INTEGER", "OBJECT", "OBJECT" })]
		public void JsonType(string column, string[] expectedTypes)
		{
			var types = m_database.Connection.Query<string>(@"select JSON_TYPE(value) from datatypes_json_core order by rowid;").ToList();
			Assert.Equal(expectedTypes, types);
		}

		[JsonTheory]
		[InlineData("value", new[] { null, "null", "true", "[]", "[0]", "[1]", "0", "1", "{}", "{\"a\": \"b\"}" })]
		public void QueryJson(string column, string[] expected)
		{
			string dataTypeName = "JSON";
#if BASELINE
			// mysql-connector-net returns "VARCHAR" for "JSON"
			dataTypeName = "VARCHAR";
#endif
			DoQuery("json_core", column, dataTypeName, expected, reader => reader.GetString(0), omitWhereTest: true);
		}

		private static byte[] GetBytes(DbDataReader reader)
		{
			var size = reader.GetBytes(0, 0, null, 0, 0);
			var result = new byte[size];
			reader.GetBytes(0, 0, result, 0, result.Length);
			return result;
		}

		private void DoQuery(
			string table,
			string column,
			string dataTypeName,
			object[] expected,
			Func<DbDataReader, object> getValue,
			object baselineCoercedNullValue = null,
			bool omitWhereTest = false,
			bool matchesDefaultType = true,
			MySqlConnection connection=null)
		{
			DoQuery<GetValueWhenNullException>(table, column, dataTypeName, expected, getValue, baselineCoercedNullValue, omitWhereTest, matchesDefaultType, connection);
		}

		// NOTE: baselineCoercedNullValue is to work around inconsistencies in mysql-connector-net; DBNull.Value will
		// be coerced to 0 by some reader.GetX() methods, but not others.
		private void DoQuery<TException>(
			string table,
			string column,
			string dataTypeName,
			object[] expected,
			Func<DbDataReader, object> getValue,
			object baselineCoercedNullValue = null,
			bool omitWhereTest = false,
			bool matchesDefaultType = true,
			MySqlConnection connection=null)
			where TException : Exception
		{
			connection = connection ?? m_database.Connection;
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = Invariant($"select {column} from datatypes_{table} order by rowid");
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
							Assert.Throws<TException>(() => getValue(reader));
#endif
						}
						else
						{
							Assert.Equal(value, getValue(reader));

							// test `reader.GetValue` and `reader.GetFieldType` if value matches default type
							if (matchesDefaultType)
							{
								Assert.Equal(value, reader.GetValue(0));
								Assert.Equal(value.GetType(), reader.GetFieldType(0));
							}

							// test `reader.GetFieldValue<value.GetType()>`
							var syncMethod = typeof(MySqlDataReader)
								.GetMethod("GetFieldValue")
								.MakeGenericMethod(value.GetType());
							Assert.Equal(value, syncMethod.Invoke(reader, new object[]{ 0 }));

							// test `reader.GetFieldValueAsync<value.GetType()>`
							var asyncMethod = typeof(MySqlDataReader)
								.GetMethod("GetFieldValueAsync", new []{ typeof(int) })
								.MakeGenericMethod(value.GetType());
							var asyncMethodValue = asyncMethod.Invoke(reader, new object[]{ 0 });
							var asyncMethodGetAwaiter = asyncMethodValue.GetType()
								.GetMethod("GetAwaiter");
							var asyncMethodGetAwaiterValue = asyncMethodGetAwaiter.Invoke(asyncMethodValue, new object[]{ });
							var asyncMethodGetResult = asyncMethodGetAwaiterValue.GetType()
								.GetMethod("GetResult");
							var asyncMethodGetResultValue = asyncMethodGetResult.Invoke(asyncMethodGetAwaiterValue, new object[]{ });
							Assert.Equal(value, asyncMethodGetResultValue);
						}
					}
					Assert.False(reader.Read());
					Assert.False(reader.NextResult());
				}

				if (!omitWhereTest)
				{
					cmd.CommandText = Invariant($"select rowid from datatypes_{table} where {column} = @value");
					var p = cmd.CreateParameter();
					p.ParameterName = "@value";
					p.Value = expected.Last();
					cmd.Parameters.Add(p);
					var result = cmd.ExecuteScalar();
					Assert.Equal(Array.IndexOf(expected, p.Value) + 1, result);
				}
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

		private static object[] ConvertToDateTimeOffset(object[] input)
		{
			var output = new object[input.Length];
			var dateTimes = ConvertToDateTime(input);
			for (int i = 0; i < dateTimes.Length; i++)
			{
				if (dateTimes[i] != null)
					output[i] = new DateTimeOffset(DateTime.SpecifyKind((DateTime)dateTimes[i], DateTimeKind.Utc));
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
