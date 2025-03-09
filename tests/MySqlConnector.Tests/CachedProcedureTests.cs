namespace MySqlConnector.Tests;

public class CachedProcedureTests
{
	[Theory]
	[MemberData(nameof(CreateParseableParameters))]
	public void ParseParameters(string sql, MySqlGuidFormat guidFormat, object[] expected)
	{
		var actual = CachedProcedure.ParseParameters(sql, guidFormat);
		Assert.Equal(expected.Length, actual.Count);
		for (int i = 0; i < expected.Length; i++)
		{
			var expectedParameter = (CachedParameter) expected[i];
			var actualParameter = actual[i];
			Assert.Equal(expectedParameter.Position, actualParameter.Position);
			Assert.Equal(expectedParameter.Direction, actualParameter.Direction);
			Assert.Equal(expectedParameter.Name, actualParameter.Name);
			Assert.Equal(expectedParameter.MySqlDbType, actualParameter.MySqlDbType);
		}
	}

	public static IEnumerable<object[]> CreateParseableParameters()
	{
		return new[]
		{
			new object[]
			{
				"", MySqlGuidFormat.Binary16, new object[0],
			},
			[
				"/* no, parameters */", MySqlGuidFormat.Binary16, new object[0],
			],
			[
				"IN test INT", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "test", "INT", false, 0, MySqlGuidFormat.Binary16),
				}
			],
			[
				"IN test INT UNSIGNED", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "test", "INT", true, 0, MySqlGuidFormat.Binary16),
				}
			],
			[
				"-- IN ignored INT UNSIGNED,\r\nIN notignored INT", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "notignored", "INT", false, 0, MySqlGuidFormat.Binary16),
				}
			],
			[
				"IN param1 INT,\r\nIN param2 INT", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "param1", "INT", false, 0, MySqlGuidFormat.Binary16),
					new CachedParameter(2, "IN", "param2", "INT", false, 0, MySqlGuidFormat.Binary16),
				}
			],
			[
				"IN /* ignored BIGINT,\r\nIN*/ param1 INT", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "param1", "INT", false, 0, MySqlGuidFormat.Binary16),
				}
			],
			[
				"IN param1 INT(11)", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "param1", "INT", false, 11, MySqlGuidFormat.Binary16),
				}
			],
			[
				"param1 BIGINT(21) UNSIGNED ZEROFILL", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "param1", "BIGINT", true, 21, MySqlGuidFormat.Binary16),
				}
			],
			[
				"param1 VARCHAR(63)", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "param1", "VARCHAR", false, 63, MySqlGuidFormat.Binary16),
				}
			],
			[
				"param1 VARCHAR(63) CHARSET latin1", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "param1", "VARCHAR", false, 63, MySqlGuidFormat.Binary16),
				}
			],
			[
				"param1 VARCHAR(63) COLLATE utf8bin", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "param1", "VARCHAR", false, 63, MySqlGuidFormat.Binary16),
				}
			],
			[
				"param1 VARCHAR(63) CHARACTER SET latin1 COLLATE latin1_bin", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "param1", "VARCHAR", false, 63, MySqlGuidFormat.Binary16),
				}
			],
			[
				"`par``am` INT", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "par`am", "INT", false, 0, MySqlGuidFormat.Binary16),
				}
			],
			[
				"IN input enum ('One', 'Two', 'Three')", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "input", "ENUM", false, 0, MySqlGuidFormat.Binary16),
				}
			],
			[
				"OUT param DECIMAL(10,5)", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "OUT", "param", "DECIMAL", false, 0, MySqlGuidFormat.Binary16),
				}
			],
			[
				"INOUT param LONGTEXT", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "INOUT", "param", "LONGTEXT", false, 0, MySqlGuidFormat.Binary16),
				}
			],
			[
				"OUT param1 BINARY(16)", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "OUT", "param1", "BINARY", false, 16, MySqlGuidFormat.Binary16),
				}
			],
			[
				"OUT param1 CHAR(36)", MySqlGuidFormat.Char36, new object[]
				{
					new CachedParameter(1, "OUT", "param1", "CHAR", false, 36, MySqlGuidFormat.Char36),
				}
			],
			[
				"ColSet set('set1','set2','set3')", MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "ColSet", "SET", false, 0, MySqlGuidFormat.Binary16),
				}
			],
			[
				@"IN param1 DATETIME(6),
-- ignored1
OUT param2 /* ignore */ INT,
param3 DECIMAL(20,10),
inout param4 VARCHAR(63) CHARSET latin1,
param5 bigint(20) unsigned zerofill,
out param6 bool",
				MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "param1", "DATETIME", false, 6, MySqlGuidFormat.Binary16),
					new CachedParameter(2, "OUT", "param2", "INT", false, 0, MySqlGuidFormat.Binary16),
					new CachedParameter(3, "IN", "param3", "DECIMAL", false, 0, MySqlGuidFormat.Binary16),
					new CachedParameter(4, "INOUT", "param4", "VARCHAR", false, 63, MySqlGuidFormat.Binary16),
					new CachedParameter(5, "IN", "param5", "BIGINT", true, 20, MySqlGuidFormat.Binary16),
					new CachedParameter(6, "OUT", "param6", "TINYINT", false, 1, MySqlGuidFormat.Binary16),
				}
			],
			[
				@"
param1 boolean,
param2 nvarchar,
param3 real(20,10),
-- ignored INT
param4 INTEGER(3)
",
				MySqlGuidFormat.Binary16, new object[]
				{
					new CachedParameter(1, "IN", "param1", "TINYINT", false, 1, MySqlGuidFormat.Binary16),
					new CachedParameter(2, "IN", "param2", "VARCHAR", false, 0, MySqlGuidFormat.Binary16),
					new CachedParameter(3, "IN", "param3", "DOUBLE", false, 20, MySqlGuidFormat.Binary16),
					new CachedParameter(4, "IN", "param4", "INT", false, 3, MySqlGuidFormat.Binary16),
				}
			],
		};
	}

	[Theory]
	[InlineData("INT", "INT", false, 0)]
	[InlineData("INTEGER", "INT", false, 0)]
	[InlineData("INTEGER UNSIGNED", "INT", true, 0)]
	[InlineData("INT(11)", "INT", false, 11)]
	[InlineData("INTEGER(11)", "INT", false, 11)]
	[InlineData("INT(11) UNSIGNED", "INT", true, 11)]
	[InlineData("INT(11) UNSIGNED NOT NULL", "INT", true, 11)]
	[InlineData("INT(11) UNSIGNED NULL", "INT", true, 11)]
	[InlineData("INT(11) UNSIGNED NULL DEFAULT NULL", "INT", true, 11)]
	[InlineData("INT(11) ZEROFILL", "INT", false, 11)]
	[InlineData("INT(11) UNSIGNED ZEROFILL", "INT", true, 11)]
	[InlineData("BIGINT(20)", "BIGINT", false, 20)]
	[InlineData("TINYINT(1) UNSIGNED", "TINYINT", true, 1)]
	[InlineData("BOOL", "TINYINT", false, 1)]
	[InlineData("Bool", "TINYINT", false, 1)]
	[InlineData("NUMERIC(30,20)", "DECIMAL", false, 30)]
	[InlineData("VARCHAR(300)", "VARCHAR", false, 300)]
	[InlineData("VARCHAR(300) CHARSET utf8mb4", "VARCHAR", false, 300)]
	[InlineData("VARCHAR(300) COLLATE ascii_general_ci", "VARCHAR", false, 300)]
	[InlineData("VARCHAR(300) COLLATE ascii_general_ci NOT NULL DEFAULT 'test'", "VARCHAR", false, 300)]
	[InlineData("CHARACTER VARYING(300) COLLATE ascii_general_ci NOT NULL DEFAULT 'test'", "VARCHAR", false, 300)]
	[InlineData("NATIONAL VARCHAR(50) COLLATE ascii_general_ci NOT NULL DEFAULT 'test'", "VARCHAR", false, 50)]
	[InlineData("BINARY(16)", "BINARY", false, 16)]
	[InlineData("CHAR BYTE(16)", "BINARY", false, 16)]
	[InlineData("CHAR(36)", "CHAR", false, 36)]
	[InlineData("REAL", "DOUBLE", false, 0)]
	[InlineData("REAL NOT NULL DEFAULT 0", "DOUBLE", false, 0)]
	[InlineData("NUMERIC(12)", "DECIMAL", false, 12)]
	[InlineData("FIXED(12)", "DECIMAL", false, 12)]
	[InlineData("ENUM('a','b','c')", "ENUM", false, 0)]
	[InlineData("SET('a','b','c')", "SET", false, 0)]
	public void ParseDataType(string sql, string expectedDataType, bool expectedUnsigned, int expectedLength)
	{
		var dataType = CachedProcedure.ParseDataType(sql, out var unsigned, out var length);
		Assert.Equal((expectedDataType, expectedUnsigned, expectedLength), (dataType, unsigned, length));
	}
}
