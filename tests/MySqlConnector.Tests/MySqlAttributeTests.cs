#if BASELINE
using MySql.Data.MySqlClient;
#endif
using System;
using Xunit;

namespace MySqlConnector.Tests;

public class MySqlAttributeTests
{
	[Fact]
	public void Construct()
	{
		var attribute = new MySqlAttribute();
#if BASELINE
		Assert.Null(attribute.AttributeName);
#else
		Assert.Equal("", attribute.AttributeName);
#endif
		Assert.Null(attribute.Value);
	}

	[Fact]
	public void ConstructWithArguments()
	{
		var attribute = new MySqlAttribute("name", "value");
		Assert.Equal("name", attribute.AttributeName);
		Assert.Equal("value", attribute.Value);
	}
}
