using System;
using System.Text;
using MySqlConnector.Core;
using Xunit;

namespace MySqlConnector.Tests
{
	public class ServerVersionTests
	{
		[Theory]
		[InlineData("5.5.5-10.1.38-MariaDB-1~bionic", "5.5.5", "10.1.38")]
		[InlineData("5.5.5-10-MariaDB", "5.5.5", null)]
		[InlineData("5.5.5-10.2-MariaDB", "5.5.5", null)]
		[InlineData("5.5.5-10.2.13-MariaDB", "5.5.5", "10.2.13")]
		[InlineData("5.5.5-10.2.19-MariaDB-1:10.2.19+maria~bionic", "5.5.5", "10.2.19")]
		[InlineData("5.5.5-10.3.13-MariaDB-1:10.3.13+maria~bionic", "5.5.5", "10.3.13")]
		[InlineData("5.7.21-log", "5.7.21", null)]
		[InlineData("8.0.13", "8.0.13", null)]
		[InlineData("5.7.25-28", "5.7.25", null)]
		[InlineData("5.7.25-", "5.7.25", null)]
		[InlineData("5.7.25-10.2.3", "5.7.25", null)]
		[InlineData("5.7.25-MariaDB-10.2.3", "5.7.25", null)]
		[InlineData("5.7.25-10.2.3-10.3.19-MariaDB-1", "5.7.25", null)]
		[InlineData("a.b.c", "0.0.0", null)]
		[InlineData("1", "1.0.0", null)]
		[InlineData("1.", "1.0.0", null)]
		[InlineData("1.2", "1.2.0", null)]
		[InlineData("1.2.", "1.2.0", null)]
		[InlineData("1.2.3", "1.2.3", null)]
		[InlineData("1.2.3.", "1.2.3", null)]
		[InlineData("1.2.3-", "1.2.3", null)]
		public void ParseServerVersion(string input, string expectedString, string expectedMariaDbString)
		{
			var serverVersion = new ServerVersion(Encoding.UTF8.GetBytes(input));
			var expected = Version.Parse(expectedString);
			Assert.Equal(expected, serverVersion.Version);

			if (expectedMariaDbString is null)
			{
				Assert.Equal(default(Version), serverVersion.MariaDbVersion);
			}
			else
			{
				var expectedMariaDb = Version.Parse(expectedMariaDbString);
				Assert.Equal(expectedMariaDb, serverVersion.MariaDbVersion);
			}
		}
	}
}
