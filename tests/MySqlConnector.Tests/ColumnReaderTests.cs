using System.Text;
using MySqlConnector.ColumnReaders;

namespace MySqlConnector.Tests;

public class ColumnReaderTests
{
	[Theory]
	[InlineData("0001-01-01", "0001-01-01T00:00:00.0000000")]
	[InlineData("2345-12-31", "2345-12-31T00:00:00.0000000")]
	[InlineData("2345-12-31 12:34:56", "2345-12-31T12:34:56.0000000")]
	[InlineData("2345-12-31 12:34:56.1", "2345-12-31T12:34:56.1000000")]
	[InlineData("2345-12-31 12:34:56.12", "2345-12-31T12:34:56.1200000")]
	[InlineData("2345-12-31 12:34:56.123", "2345-12-31T12:34:56.1230000")]
	[InlineData("2345-12-31 12:34:56.1234", "2345-12-31T12:34:56.1234000")]
	[InlineData("2345-12-31 12:34:56.12345", "2345-12-31T12:34:56.1234500")]
	[InlineData("2345-12-31 12:34:56.123456", "2345-12-31T12:34:56.1234560")]
	[InlineData("2345-12-31 12:34:56.01", "2345-12-31T12:34:56.0100000")]
	[InlineData("2345-12-31 12:34:56.001", "2345-12-31T12:34:56.0010000")]
	[InlineData("2345-12-31 12:34:56.0001", "2345-12-31T12:34:56.0001000")]
	[InlineData("2345-12-31 12:34:56.00001", "2345-12-31T12:34:56.0000100")]
	[InlineData("2345-12-31 12:34:56.000001", "2345-12-31T12:34:56.0000010")]
	[InlineData("9999-12-31 23:59:59.999999", "9999-12-31T23:59:59.9999990")]
	public void ParseDateTime(string input, string expected)
	{
		var dateTime = (DateTime) TextDateTimeColumnReader.ParseDateTime(Encoding.UTF8.GetBytes(input), convertZeroDateTime: false, allowZeroDateTime: false, dateTimeKind: DateTimeKind.Unspecified);
		Assert.Equal(expected, dateTime.ToString("O"));

		var mySqlDateTime = (MySqlDateTime) TextDateTimeColumnReader.ParseDateTime(Encoding.UTF8.GetBytes(input), convertZeroDateTime: false, allowZeroDateTime: true, dateTimeKind: DateTimeKind.Unspecified);
		Assert.Equal(expected, mySqlDateTime.GetDateTime().ToString("O"));
	}

	[Fact]
	public void ConvertZeroDateTime()
	{
		var dateTime = (DateTime) TextDateTimeColumnReader.ParseDateTime("0000-00-00"u8, convertZeroDateTime: true, allowZeroDateTime: false, DateTimeKind.Utc);
		Assert.Equal(DateTime.MinValue, dateTime);
	}

	[Fact]
	public void AllowZeroDateTime()
	{
		var mySqlDateTime = (MySqlDateTime) TextDateTimeColumnReader.ParseDateTime("0000-00-00"u8, convertZeroDateTime: false, allowZeroDateTime: true, DateTimeKind.Utc);
		Assert.Equal(default, mySqlDateTime);
	}

	[Theory]
	[InlineData("2")]
	[InlineData("20")]
	[InlineData("200")]
	[InlineData("2000")]
	[InlineData("2000-1")]
	[InlineData("2000-1-1")]
	[InlineData("2000-00-01")]
	[InlineData("2000-13-01")]
	[InlineData("2000-01-00")]
	[InlineData("2000-01-32")]
	[InlineData("10000-01-01")]
	[InlineData("2000-01-01 01")]
	[InlineData("2000-01-01 01:01")]
	[InlineData("2000-01-01 01:01:60")]
	[InlineData("2000-01-01T01:01:01")]
	[InlineData("2000-01-01 01-01-01")]
	[InlineData("2000-01-01 01:01:01.1234567")]
	[InlineData("2000-01-01 01:01:01.12345678")]
	[InlineData("2000-01-01 01:01:01.123456789")]
	public void InvalidDateTime(string input)
	{
		Assert.Throws<FormatException>(() => TextDateTimeColumnReader.ParseDateTime(Encoding.UTF8.GetBytes(input), convertZeroDateTime: false, allowZeroDateTime: false, dateTimeKind: DateTimeKind.Unspecified));
	}
}
