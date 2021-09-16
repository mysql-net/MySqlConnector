using System;
using System.Globalization;
#if BASELINE
using MySql.Data.Types;
#endif
using Xunit;

namespace MySqlConnector.Tests;

public class MySqlDateTimeTests
{
	[Fact]
	public void NewMySqlDateTimeIsNotValidDateTime()
	{
		var msdt = new MySqlDateTime();
		Assert.False(msdt.IsValidDateTime);
	}

	[Fact]
	public void ZeroMySqlDateTimeIsNotValidDateTime()
	{
		var msdt = new MySqlDateTime(0, 0, 0, 0, 0, 0, 0);
		Assert.False(msdt.IsValidDateTime);
	}

	[Fact]
	public void NonZeroMySqlDateTimeIsValidDateTime()
	{
		var msdt = new MySqlDateTime(2018, 6, 9, 0, 0, 0, 0);
		Assert.True(msdt.IsValidDateTime);
	}

	[Fact]
	public void CreateFromDateTime()
	{
		var msdt = new MySqlDateTime(s_dateTime);
		Assert.True(msdt.IsValidDateTime);
		Assert.Equal(2018, msdt.Year);
		Assert.Equal(6, msdt.Month);
		Assert.Equal(9, msdt.Day);
		Assert.Equal(12, msdt.Hour);
		Assert.Equal(34, msdt.Minute);
		Assert.Equal(56, msdt.Second);
		Assert.Equal(123, msdt.Millisecond);
		Assert.Equal(123456, msdt.Microsecond);
	}

	[Fact]
	public void GetDateTime()
	{
		var msdt = s_mySqlDateTime;
		Assert.True(msdt.IsValidDateTime);
		var dt = msdt.GetDateTime();
		Assert.Equal(s_dateTime, dt);
	}

	[Fact]
	public void GetDateTimeForInvalidDate()
	{
		var msdt = new MySqlDateTime();
		Assert.False(msdt.IsValidDateTime);
		Assert.Throws<MySqlConversionException>(() => msdt.GetDateTime());
	}

	[Fact]
	public void SetMicrosecond()
	{
		var msdt = new MySqlDateTime();
		Assert.Equal(0, msdt.Microsecond);
		msdt.Microsecond = 123456;
		Assert.Equal(123, msdt.Millisecond);
	}

	[Fact]
	public void ConvertibleToDateTime()
	{
		IConvertible convertible = s_mySqlDateTime;
		var dt = convertible.ToDateTime(CultureInfo.InvariantCulture);
		Assert.Equal(s_dateTime, dt);
	}

	[Fact]
	public void ConvertToDateTime()
	{
		object obj = s_mySqlDateTime;
		var dt = Convert.ToDateTime(obj);
		Assert.Equal(s_dateTime, dt);
	}

	[Fact]
	public void ChangeTypeToDateTime()
	{
		object obj = s_mySqlDateTime;
		var dt = Convert.ChangeType(obj, TypeCode.DateTime);
		Assert.Equal(s_dateTime, dt);
	}

	[Fact]
	public void NotConvertibleToDateTime()
	{
		IConvertible convertible = new MySqlDateTime();
#if !BASELINE
		Assert.Throws<InvalidCastException>(() => convertible.ToDateTime(CultureInfo.InvariantCulture));
#else
		Assert.Throws<MySqlConversionException>(() => convertible.ToDateTime(CultureInfo.InvariantCulture));
#endif
	}

	[Fact]
	public void NotConvertToDateTime()
	{
		object obj = new MySqlDateTime();
#if !BASELINE
		Assert.Throws<InvalidCastException>(() => Convert.ToDateTime(obj));
#else
		Assert.Throws<MySqlConversionException>(() => Convert.ToDateTime(obj));
#endif
	}

	[Fact]
	public void NotChangeTypeToDateTime()
	{
		object obj = new MySqlDateTime();
#if !BASELINE
		Assert.Throws<InvalidCastException>(() => Convert.ChangeType(obj, TypeCode.DateTime));
#else
		Assert.Throws<MySqlConversionException>(() => Convert.ChangeType(obj, TypeCode.DateTime));
#endif
	}

#if !BASELINE
	[Fact]
	public void ValidDateTimeConvertibleToString()
	{
		IConvertible convertible = s_mySqlDateTime;
		Assert.Equal("06/09/2018 12:34:56", convertible.ToString(CultureInfo.InvariantCulture));
	}

	[Fact]
	public void InvalidDateTimeConvertibleToString()
	{
		IConvertible convertible = new MySqlDateTime();
		Assert.Equal("0000-00-00", convertible.ToString(CultureInfo.InvariantCulture));
	}
#endif

	[Fact]
	public void CompareInvalidObject()
	{
		IComparable left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
#if BASELINE
		Assert.Throws<InvalidCastException>(() => left.CompareTo(new object()));
#else
		Assert.Throws<ArgumentException>(() => left.CompareTo(new object()));
#endif
	}

	[Fact]
	public void CompareYear()
	{
		IComparable left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new MySqlDateTime(2001, 1, 1, 1, 1, 1, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareMonth()
	{
		IComparable left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new MySqlDateTime(2000, 2, 1, 1, 1, 1, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareDay()
	{
		IComparable left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new MySqlDateTime(2000, 1, 2, 1, 1, 1, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareHour()
	{
		IComparable left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new MySqlDateTime(2000, 1, 1, 2, 1, 1, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareMinute()
	{
		IComparable left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new MySqlDateTime(2000, 1, 1, 1, 2, 1, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareSecond()
	{
		IComparable left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new MySqlDateTime(2000, 1, 1, 1, 1, 2, 1);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareMicrosecond()
	{
		IComparable left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 2);
		Assert.True(left.CompareTo(right) < 0);
		Assert.True(right.CompareTo(left) > 0);
	}

	[Fact]
	public void CompareEqual()
	{
		IComparable left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		IComparable right = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		Assert.Equal(0, left.CompareTo(right));
	}

#if !BASELINE
	[Fact]
	public void Operators()
	{
		var left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		var same = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		var right = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 2);
		Assert.True(left < right);
		Assert.True(left <= same);
		Assert.True(left <= right);
		Assert.False(right < left);
		Assert.False(right <= left);
		Assert.True(left == same);
		Assert.True(left != right);
		Assert.False(left > right);
		Assert.False(left >= right);
		Assert.True(right > left);
		Assert.True(right >= left);
		Assert.True(same >= left);
	}

	[Fact]
	public void Equal()
	{
		IEquatable<MySqlDateTime> left = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		var same = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 1);
		var right = new MySqlDateTime(2000, 1, 1, 1, 1, 1, 2);
		Assert.True(left.Equals(same));
		Assert.True(same.Equals(left));
		Assert.False(left.Equals(right));
		Assert.False(left.Equals(new object()));
	}
#endif

	static readonly MySqlDateTime s_mySqlDateTime = new(2018, 6, 9, 12, 34, 56, 123456);
	static readonly DateTime s_dateTime = new DateTime(2018, 6, 9, 12, 34, 56, 123).AddTicks(4560);
}
