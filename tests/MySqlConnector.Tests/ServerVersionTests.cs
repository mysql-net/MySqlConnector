using System;
using System.Text;
using MySqlConnector.Core;
using Xunit;

namespace MySqlConnector.Tests;

public class ServerVersionTests
{
	[Fact]
	public void Empty()
	{
		var empty = ServerVersion.Empty;
		Assert.Equal("", empty.OriginalString);
		Assert.Equal(new Version(0, 0), empty.Version);
		Assert.False(empty.IsMariaDb);
	}

	[Theory]
	[InlineData("5.5.5-10.1.38-MariaDB-1~bionic", "10.1.38", true)]
	[InlineData("5.5.5-10.2.13-MariaDB", "10.2.13", true)]
	[InlineData("5.5.5-10.2.19-MariaDB-1:10.2.19+maria~bionic", "10.2.19", true)]
	[InlineData("5.5.5-10.3.13-MariaDB-1:10.3.13+maria~bionic", "10.3.13", true)]
	[InlineData("11.0.1-MariaDB-1:11.0.1+maria~bionic", "11.0.1", true)]
	[InlineData("10.3.13-MariaDB-1:10.3.13+maria~bionic", "10.3.13", true)]
	[InlineData("5.7.21-log", "5.7.21", false)]
	[InlineData("8.0.13", "8.0.13", false)]
	[InlineData("5.7.25-28", "5.7.25", false)]
	[InlineData("5.7.25-", "5.7.25", false)]
	[InlineData("5.7.25-10.2.3", "5.7.25", false)]
	[InlineData("5.7.25-MariaDB-10.2.3", "5.7.25", true)]
	[InlineData("5.7.25-10.2.3-10.3.19-MariaDB-1", "5.7.25", true)]
	[InlineData("a.b.c", "0.0.0", false)]
	[InlineData("1", "1.0.0", false)]
	[InlineData("1.", "1.0.0", false)]
	[InlineData("1.2", "1.2.0", false)]
	[InlineData("1.2.", "1.2.0", false)]
	[InlineData("1.2.3", "1.2.3", false)]
	[InlineData("1.2.3.", "1.2.3", false)]
	[InlineData("1.2.3-", "1.2.3", false)]
	public void ParseServerVersion(string input, string expectedString, bool expectedMariaDb)
	{
		var serverVersion = new ServerVersion(Encoding.UTF8.GetBytes(input));
		var expected = Version.Parse(expectedString);
		Assert.Equal(expected, serverVersion.Version);
		Assert.Equal(expectedMariaDb, serverVersion.IsMariaDb);
	}
}
