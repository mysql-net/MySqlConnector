#if MYSQL_DATA
using MySql.Data.MySqlClient;
#endif
using Xunit;

namespace MySqlConnector.Tests;

public class MySqlAttributeTests
{
	[Fact]
	public void Construct()
	{
		var attribute = new MySqlAttribute();
#if MYSQL_DATA
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

	[Fact]
	public void Clone()
	{
		var attribute = new MySqlAttribute("name", "value");
		var clone = attribute.Clone();
		Assert.NotSame(attribute, clone);
		Assert.Equal(attribute.AttributeName, clone.AttributeName);
		Assert.Equal(attribute.Value, clone.Value);
	}
}
