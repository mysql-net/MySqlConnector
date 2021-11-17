using System;
using Xunit;

namespace MySqlConnector.Tests;
public class MySqlDecimalTests
{
#if !BASELINE
	[Fact]
	public void TestMySqlDecimalToString()
	{
		var stringVal = "1.23";
		MySqlDecimal decimalVal = new MySqlDecimal(stringVal);
		Assert.Equal(stringVal, decimalVal.ToString());
	}

	[Fact]
	public void TestToDouble()
	{
		var doubleVal = 1.23;
		string stringVal = "1.23";
		MySqlDecimal mySqlDecimal = new MySqlDecimal(stringVal);
		Assert.Equal(doubleVal, mySqlDecimal.ToDouble());
	}

	[Fact]
	public void TestToDecimal()
	{
		decimal doubleVal = 1.2M ;
		string stringVal = "1.2";
		MySqlDecimal mySqlDecimal = new MySqlDecimal(stringVal);
		Assert.Equal(doubleVal, mySqlDecimal.Value);
	}

	[Fact]
	public void TestInvalidFormatWithDecimalPostive()
	{
		var invalidValue = "0323.323";
		Assert.Throws<FormatException>(() => new MySqlDecimal(invalidValue));
	}

	[Fact]
	public void TestInvalidFormatWithDecimalNegative()
	{
		var invalidValue = "-0323.323";
		Assert.Throws<FormatException>(() => new MySqlDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithDecimalNegative68Length()
	{
		// If its valid negative value with . then length should be less than 68
		var invalidValue = "-123456789012345678901234567890123456.012345678901234567890123456789";
		Assert.Throws<FormatException>(() => new MySqlDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithDecimalPostive67Length()
	{
		// If its valid positive value with . then length should be less than 67
		var invalidValue = "123456789012345678901234567890123456.012345678901234567890123456789";
		Assert.Throws<FormatException>(() => new MySqlDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithOutDecimalNegative67Length()
	{
		// If its valid negative value without . then length should be less than 67
		var invalidValue = "-123456789012345678901234567890123456012345678901234567890123456789";
		Assert.Throws<FormatException>(() => new MySqlDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithOutDecimalPositive66Length()
	{
		// If its valid positive value without . then length should be less than 66
		var invalidValue = "123456789012345678901234567890123456012345678901234567890123456789";
		Assert.Throws<FormatException>(() => new MySqlDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithOutDecimalNegativeZero()
	{
		// If its valid positive value without . then length should be less than 66
		var invalidValue = "-0";
		Assert.Throws<FormatException>(() => new MySqlDecimal(invalidValue));
	}

	[Fact]
	public void TestValidFormatWithDecimalNegativeZero()
	{
		// If its valid positive value without . then length should be less than 66
		var validValue = "-0.2342323";
		MySqlDecimal decimalVal = new MySqlDecimal(validValue);
		Assert.Equal(validValue, decimalVal.ToString());
	}

	[Fact]
	public void TestValidFormatWithDecimalNegative67Length()
	{
		// valid value with negative and decimal
		var invalidValue = "-12345678901234567890123456789012345.012345678901234567890123456789";
		MySqlDecimal decimalVal = new MySqlDecimal(invalidValue);
		Assert.Equal(invalidValue, decimalVal.ToString());
	}
#endif
}
