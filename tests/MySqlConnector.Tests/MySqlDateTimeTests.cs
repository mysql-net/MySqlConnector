using System;
using MySql.Data.Types;
using Xunit;

namespace MySqlConnector.Tests
{
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
			var msdt = new MySqlDateTime(new DateTime(2018, 6, 9, 12, 34, 56, 123).AddTicks(4560));
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
			var msdt = new MySqlDateTime(2018, 6, 9, 12, 34, 56, 123456);
			Assert.True(msdt.IsValidDateTime);
			var dt = msdt.GetDateTime();
			Assert.Equal(new DateTime(2018, 6, 9, 12, 34, 56, 123).AddTicks(4560), dt);
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
	}
}
