using System;
using MySqlConnector.Core;
using Xunit;

namespace MySqlConnector.Tests
{
	public class ServerVersionTests
	{
		[Theory]
		[InlineData("5.5.5-10.1.38-MariaDB-1~bionic", "5.5.5")]
		[InlineData("5.5.5-10.2.19-MariaDB-1:10.2.19+maria~bionic", "5.5.5")]
		[InlineData("5.5.5-10.3.13-MariaDB-1:10.3.13+maria~bionic", "5.5.5")]
		[InlineData("5.7.21-log", "5.7.21")]
		[InlineData("8.0.13", "8.0.13")]
		[InlineData("5.7.25-28", "5.7.25")]
		public void ParseServerVersion(string serverVersion, string expectedString)
		{
			var expected = Version.Parse(expectedString);
			Assert.Equal(expected, new ServerVersion(serverVersion).Version);
		}
	}
}
