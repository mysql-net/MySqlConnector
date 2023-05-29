namespace MySqlConnector.Tests;

public class NormalizeTests
{
	[Theory]
	[InlineData("`mysql`.`data`", "mysql", "data")]
	[InlineData(" `mysql` . `data` ", "mysql", "data")]
	[InlineData("mysql.data", "mysql", "data")]
	[InlineData(" mysql . data ", "mysql", "data")]
	[InlineData("`mysql`.data", "mysql", "data")]
	[InlineData("mysql.`data`", "mysql", "data")]
	[InlineData("`my``sql`.`da``ta`", "my`sql", "da`ta")]
	[InlineData("`mysql.data`", null, "mysql.data")]
	[InlineData("mysqldata", null, "mysqldata")]
	[InlineData("my`sql.data", null, null)]
	[InlineData("mysql.da`ta", null, null)]
	[InlineData("sproc_name", null, "sproc_name")]
	[InlineData("`sproc name`", null, "sproc name")]
	[InlineData("db_name.sproc_name", "db_name", "sproc_name")]
	[InlineData("`db name`.sproc_name", "db name", "sproc_name")]
	[InlineData("db_name.`sproc name`", "db_name", "sproc name")]
	[InlineData("`db name`.`sproc name`", "db name", "sproc name")]
	[InlineData("`db``name`.sproc_name", "db`name", "sproc_name")]
	[InlineData("db_name.`sproc``name`", "db_name", "sproc`name")]
	public void NormalizeSchema(string input, string expectedSchema, string expectedComponent)
	{
		var normalized = new NormalizedSchema(input);
		Assert.Equal(expectedSchema, normalized.Schema);
		Assert.Equal(expectedComponent, normalized.Component);
	}

	[Theory]
	[InlineData("param1", "param1")]
	[InlineData("@param1", "param1")]
	[InlineData("?param1", "param1")]
	[InlineData("@`param1`", "param1")]
	[InlineData("?`param1`", "param1")]
	[InlineData("@`param``1`", "param`1")]
	[InlineData("@'param1'", "param1")]
	[InlineData("?'param1'", "param1")]
	[InlineData("@'param''1'", "param'1")]
	[InlineData("@\"param1\"", "param1")]
	[InlineData("?\"param1\"", "param1")]
	[InlineData("@\"param\"\"1\"", "param\"1")]
	[InlineData(" @PaRaM1 ", "PaRaM1")]
	public void NormalizeParameterName(string input, string expectedName)
	{
		var normalized = MySqlParameter.NormalizeParameterName(input);
		Assert.Equal(expectedName, normalized);
	}
}
