using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;
using MySqlConnector.Core;
using MySqlConnector.Utilities;
using Xunit;

namespace MySqlConnector.Tests
{
	public class StatementPreparerTests
	{
		[Theory]
		[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2\nAND column2 = @param")]
		[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2  -- mycomment\nAND column2 = @param")]
		[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2 -- mycomment\nAND column2 = @param")]
		[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2 -- mycomment\n  AND column2 = @param")]
		public void Bug429(string sql)
		{
			var parameters = new MySqlParameterCollection();
			parameters.AddWithValue("@param", 123);
			var parsedSql = GetParsedSql(sql, parameters);
			Assert.Equal(sql.Replace("@param", "123"), parsedSql);
		}

		[Theory]
		[InlineData(@"SELECT /* * / @param */ 1;")]
		[InlineData("SELECT # @param \n1;")]
		[InlineData("SELECT -- @param \n1;")]
		public void ParametersIgnoredInComments(string sql)
		{
			Assert.Equal(sql, GetParsedSql(sql));
		}

		[Theory]
		[InlineData(MySqlDbType.String, DummyEnum.FirstValue, "'FirstValue'")]
		[InlineData(MySqlDbType.VarChar, DummyEnum.FirstValue, "'FirstValue'")]
		[InlineData(null, DummyEnum.FirstValue, "0")]
		public void EnumParametersAreParsedCorrectly(MySqlDbType? type, object value, string replacedValue)
		{
			const string sql = "SELECT @param";
			var parameters = new MySqlParameterCollection();
			var parameter = new MySqlParameter("@param", value);

			if (type != null)
			{
				parameter.MySqlDbType = type.Value;
			}

			parameters.Add(parameter);

			var parsedSql = GetParsedSql(sql, parameters);
			Assert.Equal(sql.Replace("@param", replacedValue), parsedSql);
		}

		[Theory]
		[InlineData("SELECT '@param';")]
		[InlineData("SELECT \"@param\";")]
		[InlineData("SELECT `@param`;")]
		[InlineData("SELECT 'test\\'@param';")]
		[InlineData("SELECT \"test\\\"@param\";")]
		[InlineData("SELECT 'test''@param';")]
		[InlineData("SELECT \"test\"\"@param\";")]
		[InlineData("SELECT `test``@param`;")]
		public void ParametersIgnoredInStrings(string sql)
		{
			Assert.Equal(sql, GetParsedSql(sql));
		}

		[Theory]
		[MemberData(nameof(FormatParameterData))]
		public void FormatParameter(object parameterValue, string replacedValue)
		{
			var parameters = new MySqlParameterCollection { new MySqlParameter("@param", parameterValue) };
			const string sql = "SELECT @param";
			var parsedSql = GetParsedSql(sql, parameters);
			Assert.Equal(sql.Replace("@param", replacedValue), parsedSql);
		}

		public static IEnumerable<object[]> FormatParameterData =>
			new[]
			{
				new object[] { (byte) 200, "200" },
				new object[] { (sbyte) -100, "-100" },
				new object[] { (short) -12345, "-12345" },
				new object[] { (ushort) 45678, "45678" },
				new object[] { -1_234_567_890, "-1234567890" },
				new object[] { 3_456_789_012u, "3456789012" },
				new object[] { -12_345_678_901L, "-12345678901" },
				new object[] { 12_345_678_901UL, "12345678901" },
				new object[] { 1.01234567f, "1.01234567" },
				new object[] { 1.0123456789012346, "1.0123456789012346" },
				new object[] { 123456789.123456789m, "123456789.123456789" },
				new object[] { "1234", "'1234'" },
				new object[] { "it's", "'it\\'s'" },
				new object[] { 'a', "'a'" },
				new object[] { '\'', "'\\''" },
				new object[] { '\\', "'\\\\'" },
				new object[] { 'ﬃ', "'ﬃ'" },
				new object[] { new DateTime(1234, 12, 23, 12, 34, 56, 789), "timestamp('1234-12-23 12:34:56.789000')" },
				new object[] { new DateTimeOffset(1234, 12, 23, 12, 34, 56, 789, TimeSpan.FromHours(2)), "timestamp('1234-12-23 10:34:56.789000')" },
				new object[] { new TimeSpan(2, 3, 4, 5, 6), "time '51:04:05.006000'" },
				new object[] { new TimeSpan(-2, -3, -4, -5, -6), "time '-51:04:05.006000'" },
				new object[] { new Guid("00112233-4455-6677-8899-AABBCCDDEEFF"), "'00112233-4455-6677-8899-aabbccddeeff'" },
			};

		[Theory]
		[InlineData(StatementPreparerOptions.GuidFormatChar36, "'61626364-6566-6768-696a-6b6c6d6e6f70'")]
		[InlineData(StatementPreparerOptions.GuidFormatChar32, "'6162636465666768696a6b6c6d6e6f70'")]
		[InlineData(StatementPreparerOptions.GuidFormatBinary16, "_binary'abcdefghijklmnop'")]
		[InlineData(StatementPreparerOptions.GuidFormatTimeSwapBinary16, "_binary'ghefabcdijklmnop'")]
		[InlineData(StatementPreparerOptions.GuidFormatLittleEndianBinary16, "_binary'dcbafehgijklmnop'")]
		public void GuidFormat(object options, string replacedValue)
		{
			var parameters = new MySqlParameterCollection { new MySqlParameter("@param", new Guid("61626364-6566-6768-696a-6b6c6d6e6f70")) };
			const string sql = "SELECT @param";
			var parsedSql = GetParsedSql(sql, parameters, (StatementPreparerOptions) options);
			Assert.Equal(sql.Replace("@param", replacedValue), parsedSql);
		}

		private static string GetParsedSql(string input, MySqlParameterCollection parameters = null, StatementPreparerOptions options = StatementPreparerOptions.None) =>
			Encoding.UTF8.GetString(new StatementPreparer(input, parameters ?? new MySqlParameterCollection(), options).ParseAndBindParameters().Slice(1));
	}
}
