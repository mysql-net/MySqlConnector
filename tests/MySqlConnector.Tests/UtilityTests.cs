using System;
using System.Globalization;
using System.Text;
using MySqlConnector.Utilities;
using Xunit;

namespace MySqlConnector.Tests
{
	public class UtilityTests
	{
		[Theory]
		[InlineData("00:00:00", "00:00:00")]
		[InlineData("00:00:01", "00:00:01")]
		[InlineData("00:01:00", "00:01:00")]
		[InlineData("00:12:34", "00:12:34")]
		[InlineData("01:00:00", "01:00:00")]
		[InlineData("12:34:56", "12:34:56")]
		[InlineData("-00:00:01", "-00:00:01")]
		[InlineData("-00:01:00", "-00:01:00")]
		[InlineData("-00:12:34", "-00:12:34")]
		[InlineData("-01:00:00", "-01:00:00")]
		[InlineData("-12:34:56", "-12:34:56")]
		[InlineData("00:00:00.1", "00:00:00.1")]
		[InlineData("00:00:00.12", "00:00:00.12")]
		[InlineData("00:00:00.123", "00:00:00.123")]
		[InlineData("00:00:00.1234", "00:00:00.1234")]
		[InlineData("00:00:00.12345", "00:00:00.12345")]
		[InlineData("00:00:00.123456", "00:00:00.123456")]
		[InlineData("-00:00:00.1", "-00:00:00.1")]
		[InlineData("-00:00:00.12", "-00:00:00.12")]
		[InlineData("-00:00:00.123", "-00:00:00.123")]
		[InlineData("-00:00:00.1234", "-00:00:00.1234")]
		[InlineData("-00:00:00.12345", "-00:00:00.12345")]
		[InlineData("-00:00:00.123456", "-00:00:00.123456")]
		[InlineData("838:59:59", "34.22:59:59")]
		[InlineData("838:59:59.999999", "34.22:59:59.999999")]
		[InlineData("-838:59:59", "-34.22:59:59")]
		[InlineData("-838:59:59.999999", "-34.22:59:59.999999")]
		public void ParseTimeSpan(string input, string expectedString)
		{
			var expected = TimeSpan.ParseExact(expectedString, "c", CultureInfo.InvariantCulture);
			var actual = Utility.ParseTimeSpan(Encoding.ASCII.GetBytes(input));
			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData("0")]
		[InlineData("0:0:0")]
		[InlineData("--01:00:00")]
		[InlineData("00-00-00")]
		[InlineData("00:00:60")]
		[InlineData("00:60:00")]
		[InlineData("999:00:00")]
		[InlineData("00:00:00.1234567")]
		public void ParseTimeSpanFails(string input)
		{
			Assert.Throws<FormatException>(() => Utility.ParseTimeSpan(Encoding.ASCII.GetBytes(input)));
		}
	}
}
