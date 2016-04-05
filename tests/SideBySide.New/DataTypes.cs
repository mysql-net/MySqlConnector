using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;
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
		[InlineData("Int16", new object[] { null, default(short), short.MinValue, short.MaxValue, (short) 12345 })]
		public void QueryInt16(string column, object[] expected)
		{
			DoQuery("numbers", column, expected, reader => reader.GetInt16(0));
		}

		[Theory]
		[InlineData("UInt16", new object[] { null, default(ushort), ushort.MinValue, ushort.MaxValue, (ushort) 12345 })]
		public void QueryUInt16(string column, object[] expected)
		{
			DoQuery<InvalidCastException>("numbers", column, expected, reader => reader.GetFieldValue<ushort>(0));
		}

		[Theory]
		[InlineData("Int24", new object[] { null, default(int), -8388608, 8388607, 1234567 })]
		[InlineData("Int32", new object[] { null, default(int), int.MinValue, int.MaxValue, 123456789 })]
		public void QueryInt32(string column, object[] expected)
		{
			DoQuery("numbers", column, expected, reader => reader.GetInt32(0));
		}

		[Theory]
		[InlineData("UInt24", new object[] { null, default(uint), 0u, 16777215u, 1234567u })]
		[InlineData("UInt32", new object[] { null, default(uint), uint.MinValue, uint.MaxValue, 123456789u })]
		public void QueryUInt32(string column, object[] expected)
		{
			DoQuery<InvalidCastException>("numbers", column, expected, reader => reader.GetFieldValue<uint>(0));
		}

		[Theory]
		[InlineData("Int64", new object[] { null, default(long), long.MinValue, long.MaxValue, 1234567890123456789 })]
		public void QueryInt64(string column, object[] expected)
		{
			DoQuery("numbers", column, expected, reader => reader.GetInt64(0));
		}

		[Theory]
		[InlineData("UInt64", new object[] { null, default(ulong), ulong.MinValue, ulong.MaxValue, 1234567890123456789u })]
		public void QueryUInt64(string column, object[] expected)
		{
			DoQuery<InvalidCastException>("numbers", column, expected, reader => reader.GetFieldValue<ulong>(0));
		}

		[Theory]
		[InlineData("utf8", new[] { null, "", "ASCII", "Ũńıċōđĕ" })]
		[InlineData("utf8bin", new[] { null, "", "ASCII", "Ũńıċōđĕ" })]
		[InlineData("latin1", new[] { null, "", "ASCII", "Lãtïñ" })]
		[InlineData("latin1bin", new[] { null, "", "ASCII", "Lãtïñ" })]
		[InlineData("cp1251", new[] { null, "", "ASCII", "АБВГабвг" })]
		public void QueryString(string column, string[] expected)
		{
			DoQuery("strings", column, expected, reader => reader.GetString(0));
		}

		[Theory]
		[InlineData("`Date`", new object[] { null, "1000 01 01", "9999 12 31", "0001 01 01", "2016 04 05" })]
		[InlineData("`DateTime`", new object[] { null, "1000 01 01 0 0 0", "9999 12 31 23 59 59 999999", "0001 01 01 0 0 0", "2016 4 5 14 3 4 567890" })]
		[InlineData("`Timestamp`", new object[] { null, "1970 01 01 0 0 1", "2038 1 18 3 14 7 999999", "0001 01 01 0 0 0", "2016 4 5 14 3 4 567890" })]
		public void QueryDate(string column, object[] expected)
		{
			DoQuery("times", column, ConvertToDateTime(expected), reader => reader.GetDateTime(0));
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
		[InlineData("`Time`", new object[] { null, "-838 -59 -59", "838 59 59", "0 0 0", "0 14 3 4 567890" })]
		public void QueryTime(string column, object[] expected)
		{
			DoQuery<InvalidCastException>("times", column, ConvertToTimeSpan(expected), reader => reader.GetFieldValue<TimeSpan>(0));
		}

		[Theory]
		[InlineData("`Year`", new object[] { null, 1901, 2155, 0, 2016 })]
		public void QueryYear(string column, object[] expected)
		{
			DoQuery("times", column, expected, reader => reader.GetInt32(0));
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

		[Theory]
		[InlineData("TinyBlob", 255)]
		[InlineData("Blob", 65535)]
#if false
		// MySQL has a default max_allowed_packet size of 4MB; without changing the server configuration, it's impossible
		// to send more than 4MB of data.
		[InlineData("MediumBlob", 16777216)]
		[InlineData("LargeBlob", 67108864)]
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

			foreach (var queryResult in m_database.Connection.Query<byte[]>(Invariant($"select `{column}` from datatypes.blobs where rowid = @lastInsertId"), new { lastInsertId }))
			{
				Assert.Equal(data, queryResult);
				break;
			}

			m_database.Connection.Execute(Invariant($"delete from datatypes.blobs where rowid = @lastInsertId"), new { lastInsertId });
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
				cmd.CommandText = Invariant($"select {column} from datatypes.{table} order by rowid");
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
