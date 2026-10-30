using System.Globalization;
using System.IO;
using System.Text;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Tests;

public class StatementPreparerTests
{
	[Theory]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2\nAND column2 = @param")]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2  -- mycomment\nAND column2 = @param")]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2 -- mycomment\nAND column2 = @param")]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2 -- mycomment\n  AND column2 = @param")]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2 /* mycomment */\n  AND column2 = @param")]
	[InlineData("SELECT Id\nFROM mytable\nWHERE column1 = 2 /* mycomment */ AND column2 = @param")]
	public void Bug429(string sql)
	{
		var parameters = new MySqlParameterCollection();
		parameters.AddWithValue("@param", 123);
		var parsedSql = GetParsedSql(sql, parameters);
		Assert.Equal(sql.Replace("@param", "123"), parsedSql);
	}

	[Theory]
	[InlineData("UPDATE table SET a=a-@b;")]
	[InlineData("UPDATE table SET a=a-/* subtract b */@b;")]
	[InlineData("UPDATE table SET a=a+@b;")]
	[InlineData("UPDATE table SET a=a/@b;")]
	[InlineData("UPDATE table SET a=a-- \n-@b;")]
	[InlineData("UPDATE table SET a = a-@b;")]
	[InlineData("UPDATE table SET a = a+@b;")]
	[InlineData("UPDATE table SET a = a - @b;")]
	[InlineData("UPDATE table SET a=@b-a;")]
	[InlineData("UPDATE table SET a=@b+a;")]
	[InlineData("UPDATE table SET a = @b-a;")]
	[InlineData("UPDATE table SET a = @b - a;")]
	public void Bug563(string sql)
	{
		var parameters = new MySqlParameterCollection();
		parameters.AddWithValue("@b", 123);
		var parsedSql = GetParsedSql(sql, parameters);
		Assert.Equal(sql.Replace("@b", "123"), parsedSql);
	}

	[Theory]
	[InlineData(@"SELECT /* * / @param */ 1;")]
	[InlineData("SELECT # @param \n1;")]
	[InlineData("SELECT -- @param \n1;")]
	[InlineData("SELECT --\t@param \n1;")]
	[InlineData("SELECT --\x7F@param \n1;")]
	public void ParametersIgnoredInComments(string sql)
	{
		Assert.Equal(sql, GetParsedSql(sql));
	}

	[Theory]
	[InlineData(MySqlDbType.String, DummyEnum.FirstValue, "'FirstValue'")]
	[InlineData(MySqlDbType.VarChar, DummyEnum.FirstValue, "'FirstValue'")]
	[InlineData(null, DummyEnum.FirstValue, "0")]
	public void EnumParametersAreParsedCorrectly(MySqlDbType? type, object value, string replacedValue)
	{
		const string sql = "SELECT @param;";
		var parameters = new MySqlParameterCollection();
		var parameter = new MySqlParameter("@param", value);

		if (type is not null)
		{
			parameter.MySqlDbType = type.Value;
		}

		parameters.Add(parameter);

		var parsedSql = GetParsedSql(sql, parameters);
		Assert.Equal(sql.Replace("@param", replacedValue), parsedSql);
	}

	[Theory]
	[InlineData("SELECT '@param';")]
	[InlineData("SELECT \"@param\";")]
	[InlineData("SELECT `@param`;")]
	[InlineData("SELECT 'test\\'@param';")]
	[InlineData("SELECT \"test\\\"@param\";")]
	[InlineData("SELECT 'test''@param';")]
	[InlineData("SELECT \"test\"\"@param\";")]
	[InlineData("SELECT `test``@param`;")]
	public void ParametersIgnoredInStrings(string sql)
	{
		Assert.Equal(sql, GetParsedSql(sql));
	}

	[Theory]
	[InlineData("SELECT @var;", "var")]
	[InlineData("SELECT @var;", "@var")]
	[InlineData("SELECT @var;", "@`var`")]
	[InlineData("SELECT @var;", "@'var'")]
	[InlineData("SELECT @`var`;", "var")]
	[InlineData("SELECT @`var`;", "@var")]
	[InlineData("SELECT @`var`;", "@`var`")]
	[InlineData("SELECT @`v``ar`;", "v`ar")]
	[InlineData("SELECT @`v``ar`;", "@`v``ar`")]
	[InlineData("SELECT @'var';", "var")]
	[InlineData("SELECT @'var';", "@var")]
	[InlineData("SELECT @'var';", "@'var'")]
	[InlineData("SELECT @'v''ar';", "v'ar")]
	[InlineData("SELECT @'v''ar';", "@'v''ar'")]
	[InlineData("SELECT @\"var\";", "var")]
	[InlineData("SELECT @\"var\";", "@var")]
	[InlineData("SELECT @\"var\";", "@\"var\"")]
	[InlineData("SELECT @\"v\"\"ar\";", "v\"ar")]
	[InlineData("SELECT @\"v\"\"ar\";", "@\"v\"\"ar\"")]
	public void QuotedParameters(string sql, string parameterName)
	{
		var parameters = new MySqlParameterCollection();
		parameters.AddWithValue(parameterName, 123);
		var parsedSql = GetParsedSql(sql, parameters);
		Assert.Equal("SELECT 123;", parsedSql);
	}

	[Theory]
	[InlineData("SELECT @var, 'test';")]
	[InlineData("SELECT @`var`, 'test';")]
	[InlineData("SELECT @'var', 'test';")]
	[InlineData("SELECT @\"var\", 'test';")]
	public void QuotedParametersFollowedByString(string sql)
	{
		var parameters = new MySqlParameterCollection();
		parameters.AddWithValue("@var", 123);
		var parsedSql = GetParsedSql(sql, parameters);
		Assert.Equal("SELECT 123, 'test';", parsedSql);
	}

	[Theory]
	[InlineData("SELECT @var", false, "SELECT 123")]
	[InlineData("SELECT @var ", false, "SELECT 123 ")]
	[InlineData("SELECT @'var'", false, "SELECT 123")]
	[InlineData("SELECT @'var'\t", false, "SELECT 123\t")]
	[InlineData("SELECT @`var`", false, "SELECT 123")]
	[InlineData("SELECT @\"var\"", false, "SELECT 123")]
	[InlineData("SELECT @var", true, "SELECT 123;")]
	[InlineData("SELECT @'var'", true, "SELECT 123;")]
	[InlineData("SELECT @`var`", true, "SELECT 123;")]
	[InlineData("SELECT @`var` ", true, "SELECT 123 ;")]
	[InlineData("SELECT @\"var\"", true, "SELECT 123;")]
	public void QuotedParametersAtEndOfStatement(string sql, bool appendSemicolon, string expectedSql)
	{
		var parameters = new MySqlParameterCollection();
		parameters.AddWithValue("@var", 123);
		var options = appendSemicolon ? StatementPreparerOptions.AppendSemicolon : StatementPreparerOptions.None;
		var parsedSql = GetParsedSql(sql, parameters, options);
		Assert.Equal(expectedSql, parsedSql);
	}

	[Theory]
	[InlineData(@"SET @'var':=1;
SELECT @foo+@'var' as R")]
	[InlineData(@"SET @'var':=1;
SELECT @foo+1 as R")]
	[InlineData(@"SET @'var':=@foo+1;
SELECT @'var' as R")]
	public void Bug589(string sql)
	{
		var parameters = new MySqlParameterCollection();
		parameters.AddWithValue("@foo", 22);
		var parsedSql = GetParsedSql(sql, parameters, StatementPreparerOptions.AllowUserVariables);
		Assert.Equal(sql.Replace("@foo", "22"), parsedSql);
	}

	[Theory]
	[MemberData(nameof(FormatParameterData))]
	public void FormatParameter(object parameterValue, string replacedValue, bool noBackslashEscapes = false)
	{
		var parameters = new MySqlParameterCollection { new("@param", parameterValue) };
		const string sql = "SELECT @param;";
		var options = noBackslashEscapes ? StatementPreparerOptions.NoBackslashEscapes : StatementPreparerOptions.None;
		var parsedSql = GetParsedSql(sql, parameters, options);
		Assert.Equal(sql.Replace("@param", replacedValue), parsedSql);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void AlterUserHardcoded(bool noBackslashEscapes)
	{
		const string sql = "ALTER USER 'root'@'%' IDENTIFIED BY 'password';";
		var options = noBackslashEscapes ? StatementPreparerOptions.NoBackslashEscapes : StatementPreparerOptions.None;
		var parsedSql = GetParsedSql(sql, options: options);
		Assert.Equal(sql, parsedSql);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void AlterUserWithParameters(bool noBackslashEscapes)
	{
		const string sql = "ALTER USER @user@'localhost' IDENTIFIED BY @password;";
		var parameters = new MySqlParameterCollection { new("@user", "root"), new("@password", "P@ssw0rd") };
		var options = noBackslashEscapes ? StatementPreparerOptions.NoBackslashEscapes : StatementPreparerOptions.None;
		var parsedSql = GetParsedSql(sql, parameters, options);
		Assert.Equal("ALTER USER 'root'@'localhost' IDENTIFIED BY 'P@ssw0rd';", parsedSql);

		// test most common form: just using a parameter for the password
		parsedSql = GetParsedSql("ALTER USER 'test'@'%' IDENTIFIED BY @password;", parameters, options);
		Assert.Equal("ALTER USER 'test'@'%' IDENTIFIED BY 'P@ssw0rd';", parsedSql);
	}

	// MySQL lexes '@' as its own token, so whitespace between a keyword and a parameter is optional
	[Theory]
	[InlineData("SELECT@param;")]
	[InlineData("SELECT * FROM t WHERE@param IS NULL;")]
	[InlineData("SELECT * FROM t WHERE a = 1 AND@param;")]
	[InlineData("SELECT * FROM t WHERE a = 1 OR@param;")]
	[InlineData("SELECT * FROM t WHERE NOT@param;")]
	[InlineData("SELECT * FROM t WHERE name LIKE@param;")]
	[InlineData("SELECT * FROM t LIMIT@param;")]
	[InlineData("SELECT * FROM t LIMIT 10 OFFSET@param;")]
	[InlineData("SELECT CASE WHEN a = 1 THEN@param ELSE@param END FROM t;")]
	[InlineData("SELECT * FROM t WHERE@param IS NULL OR name = @param;")]
	[InlineData("SELECT * FROM t WHERE name = BINARY@param;")]
	[InlineData("SELECT * FROM t WHERE name = _utf8mb4@param COLLATE utf8mb4_bin;")]
	[InlineData("SELECT N@param;")]
	[InlineData("SELECT * FROM t WHERE a IN(@param,@param);")]
	public void ParametersAdjacentToKeywords(string sql) => AssertParametersBound(sql);

	[Theory]
	[InlineData("SELECT @@version;")]
	[InlineData("SELECT@@version;")]
	[InlineData("SELECT@@IDENTITY;")]
	[InlineData("SELECT @@global.max_connections, @@session.sql_mode;")]
	[InlineData("SELECT 1 UNION SELECT@@version;")]
	[InlineData("SET @@session.sql_mode = 'ANSI';")]
	[InlineData("SET@@session.sql_mode='ANSI';")]
	[InlineData("SELECT * FROM t WHERE a = 1 AND@@autocommit = 1;")]
	[InlineData("SELECT 'a', @@version;")]
	[InlineData("SELECT `a`, @@version FROM t;")]
	[InlineData("SELECT @@version, @param;")]
	[InlineData("SELECT@@version,@param;")]
	[InlineData("SET@@session.time_zone=@param;")]
	public void SystemVariablesAreNotParameters(string sql) => AssertParametersBound(sql);

	[Theory]
	[InlineData("SELECT 'a' @param;")]
	[InlineData("SELECT 'a'\n@param;")]
	[InlineData("SELECT 'a'\t@param;")]
	[InlineData("SELECT \"a\" @param;")]
	[InlineData("SELECT * FROM t WHERE name LIKE '%' @param '%';")]
	[InlineData("SELECT `amount` @param FROM t;")]
	[InlineData("SELECT 'a',@param;")]
	[InlineData("SELECT 'a' , @param;")]
	[InlineData("SELECT * FROM t WHERE `a` = @param;")]
	[InlineData("SELECT * FROM t WHERE a = 'x' AND b = @param;")]
	[InlineData("SELECT * FROM t WHERE a IN ('x', @param);")]
	[InlineData("SELECT CONCAT('a', @param, 'b');")]
	public void ParametersAfterQuotedValues(string sql) => AssertParametersBound(sql);

	[Theory]
	[InlineData("SELECT @'param' @param;", "SELECT 'abc' 'abc';")]
	[InlineData("SELECT @\"param\" @param;", "SELECT 'abc' 'abc';")]
	[InlineData("SELECT @`param` @param;", "SELECT 'abc' 'abc';")]
	[InlineData("SELECT @'param'\n@param;", "SELECT 'abc'\n'abc';")]
	[InlineData("SELECT ?param, 'a' ?param, @param;", "SELECT 'abc', 'a' 'abc', 'abc';")]
	public void QuotedAndQuestionMarkParametersFollowedByParameter(string sql, string expectedSql)
	{
		var parameters = new MySqlParameterCollection { new("@param", "abc") };
		Assert.Equal(expectedSql, GetParsedSql(sql, parameters));
	}

	[Theory]
	[InlineData("SELECT @undefined;")]
	[InlineData("SELECT@undefined;")]
	[InlineData("SELECT * FROM t WHERE a = @undefined;")]
	[InlineData("SELECT * FROM t WHERE a = 1 AND@undefined;")]
	[InlineData("SELECT 'a' @undefined;")]
	[InlineData("SELECT 'a'\n@undefined;")]
	[InlineData("SELECT `a` @undefined;")]
	[InlineData("SELECT @'undefined';")]
	[InlineData("SELECT @`undefined`;")]
	[InlineData("SELECT @\"undefined\";")]
	[InlineData("SELECT ?undefined;")]
	public void UndefinedParameter(string sql)
	{
		Assert.Throws<MySqlException>(() => GetParsedSql(sql));
		Assert.Equal(sql, GetParsedSql(sql, options: StatementPreparerOptions.AllowUserVariables));
	}

	[Theory]
	[InlineData("CREATE USER 'test'@'localhost' IDENTIFIED BY 'password';")]
	[InlineData("CREATE USER 'test'@localhost;")]
	[InlineData("CREATE USER 'test'@'%', 'test2'@'%';")]
	[InlineData("CREATE USER 'test'@'192.168.0.%';")]
	[InlineData("CREATE USER `test`@`localhost`;")]
	[InlineData("CREATE USER \"test\"@\"localhost\";")]
	[InlineData("DROP USER 'test'@'%';")]
	[InlineData("RENAME USER 'old'@'%' TO 'new'@'%';")]
	[InlineData("GRANT SELECT ON db.* TO 'test'@'localhost';")]
	[InlineData("REVOKE SELECT ON db.* FROM 'test'@'localhost';")]
	[InlineData("SHOW GRANTS FOR 'test'@'localhost';")]
	[InlineData("SET PASSWORD FOR 'test'@'localhost' = 'password';")]
	[InlineData("CREATE DEFINER='root'@'localhost' PROCEDURE p() SELECT 1;")]
	[InlineData("CREATE DEFINER=`root`@`localhost` VIEW v AS SELECT 1;")]
	[InlineData("ALTER USER test@'%' IDENTIFIED BY 'password';", Skip = "https://github.com/mysql-net/MySqlConnector/issues/1480")]
	[InlineData("ALTER USER test @'%' IDENTIFIED BY 'password';", Skip = "https://github.com/mysql-net/MySqlConnector/issues/1480")]
	[InlineData("ALTER USER 'test' @'%' IDENTIFIED BY 'password';", Skip = "https://github.com/mysql-net/MySqlConnector/issues/1480")]
	[InlineData("ALTER USER test\r\n@'%' IDENTIFIED BY 'password';", Skip = "https://github.com/mysql-net/MySqlConnector/issues/1480")]
	[InlineData("ALTER USER test@localhost IDENTIFIED BY 'password';", Skip = "https://github.com/mysql-net/MySqlConnector/issues/1480")]
	[InlineData("ALTER USER test @localhost IDENTIFIED BY 'password';", Skip = "https://github.com/mysql-net/MySqlConnector/issues/1480")]
	[InlineData("ALTER USER test @'localhost' IDENTIFIED BY 'password';", Skip = "https://github.com/mysql-net/MySqlConnector/issues/1480")]
	[InlineData("ALTER USER 'test' @'localhost' IDENTIFIED BY 'password';", Skip = "https://github.com/mysql-net/MySqlConnector/issues/1480")]
	[InlineData("ALTER USER test\r\n@localhost IDENTIFIED BY 'password';", Skip = "https://github.com/mysql-net/MySqlConnector/issues/1480")]
	[InlineData("ALTER USER 'test' /* comment */ @'%' IDENTIFIED BY 'password';", Skip = "https://github.com/mysql-net/MySqlConnector/issues/1480")]
	public void AccountNames(string sql)
	{
		AssertParametersBound(sql);
		AssertParametersBound(sql, StatementPreparerOptions.NoBackslashEscapes);
	}

	[Theory]
	[InlineData("SELECT 1--@param;", "SELECT 1--123;")]
	[InlineData("SELECT 1 -- @param\n+ @param;", "SELECT 1 -- @param\n+ 123;")]
	[InlineData("SELECT 1 # @param\n+ @param;", "SELECT 1 # @param\n+ 123;")]
	[InlineData("SELECT /* @param */@param;", "SELECT /* @param */123;")]
	[InlineData("SELECT 'a' -- @param\n, @param;", "SELECT 'a' -- @param\n, 123;")]
	[InlineData("SELECT 'a' # @param\n, @param;", "SELECT 'a' # @param\n, 123;")]
	public void ParametersNearComments(string sql, string expectedSql)
	{
		var parameters = new MySqlParameterCollection { new("@param", 123) };
		Assert.Equal(expectedSql, GetParsedSql(sql, parameters));
	}

	[Theory]
	[InlineData("SELECT 'a\\', @param;", false, "SELECT 'a\\', @param;", false)]
	[InlineData("SELECT 'a\\', @param;", true, "SELECT 'a\\', 'abc';", true)]
	[InlineData("SELECT \"a\\\", @param;", false, "SELECT \"a\\\", @param;", false)]
	[InlineData("SELECT \"a\\\", @param;", true, "SELECT \"a\\\", 'abc';", true)]
	[InlineData("SELECT `a\\`, @param;", false, "SELECT `a\\`, 'abc';", true)]
	[InlineData("SELECT `a\\`, @param;", true, "SELECT `a\\`, 'abc';", true)]
	public void BackslashesInQuotedValues(string sql, bool noBackslashEscapes, string expectedSql, bool expectedComplete)
	{
		var parameters = new MySqlParameterCollection { new("@param", "abc") };
		var preparer = new StatementPreparer(sql, parameters, noBackslashEscapes ? StatementPreparerOptions.NoBackslashEscapes : StatementPreparerOptions.None);
		var writer = new ByteBufferWriter();
		var isComplete = preparer.ParseAndBindParameters(writer);
		Assert.Equal(expectedComplete, isComplete);
		using var payload = writer.ToPayloadData();
		Assert.Equal(expectedSql, Encoding.UTF8.GetString(payload.Span));
	}

	// https://github.com/mysql-net/MySqlConnector/issues/1480
	[Theory]
	[InlineData("ALTER USER test@'%' IDENTIFIED BY 'password';")]
	[InlineData("ALTER USER test@localhost IDENTIFIED BY 'password';")]
	[InlineData("CREATE USER test@localhost IDENTIFIED BY 'password';")]
	[InlineData("ALTER USER 'test' @'%' IDENTIFIED BY 'password';")]
	[InlineData("ALTER USER 'test' @'localhost' IDENTIFIED BY 'password';")]
	[InlineData("ALTER USER 'test' @localhost IDENTIFIED BY 'password';")]
	[InlineData("ALTER USER 'test'\n@'%' IDENTIFIED BY 'password';")]
	[InlineData("CREATE USER 'test' @'%' IDENTIFIED BY 'password';")]
	[InlineData("CREATE USER 'test' @localhost IDENTIFIED BY 'password';")]
	[InlineData("create user 'test' @'%' identified by 'password';")]
	[InlineData("/* comment */ CREATE USER 'test' @'%' IDENTIFIED BY 'password';")]
	[InlineData("DROP USER 'test' @'%';")]
	[InlineData("RENAME USER 'test' @'%' TO 'test2' @'%';")]
	[InlineData("GRANT SELECT ON db.* TO 'test' @'%';")]
	[InlineData("REVOKE SELECT ON db.* FROM 'test' @'%';")]
	[InlineData("SET PASSWORD FOR 'test' @'%' = 'password';")]
	[InlineData("SHOW GRANTS FOR 'test' @'%';")]
	[InlineData("SELECT 1; ALTER USER 'test' @'%' IDENTIFIED BY 'password';")]
	public void Bug1480(string sql)
	{
		var parsedSql = GetParsedSql(sql);
		Assert.Equal(sql, parsedSql);
	}

	[Fact]
	public void AccountNameWithWhitespaceAndParameter()
	{
		var parameters = new MySqlParameterCollection();
		parameters.AddWithValue("@password", "secret");
		var parsedSql = GetParsedSql("ALTER USER 'test' @'%' IDENTIFIED BY @password;", parameters);
		Assert.Equal("ALTER USER 'test' @'%' IDENTIFIED BY 'secret';", parsedSql);
	}

	// the account-name handling for CREATE USER, etc. must not change how '@' is parsed in other statements
	[Theory]
	[InlineData("SELECT@@version")]
	[InlineData("SELECT@@IDENTITY")]
	[InlineData("SET@@session.sql_mode='ANSI'")]
	[InlineData("SELECT @@version")]
	[InlineData("SELECT 1;SELECT@@version")]
	public void SystemVariableWithoutSpaceAfterKeyword(string sql)
	{
		Assert.Equal(sql, GetParsedSql(sql));
	}

	[Theory]
	[InlineData("SELECT * FROM t WHERE c LIKE@name")]
	[InlineData("SELECT * FROM t WHERE@name")]
	[InlineData("SELECT _utf8mb4@name")]
	[InlineData("SELECT 1 WHERE 1 AND@name")]
	[InlineData("SELECT * FROM t WHERE c LIKE '%' @name '%'")]
	[InlineData("SELECT `amount` @name")]
	[InlineData("SELECT \"amount\" @name")]
	[InlineData("SELECT 'amount' @name")]
	[InlineData("SELECT 'amount'\n@name")]
	[InlineData("SELECT 1;SELECT 'amount' @name")]
	[InlineData("ALTER USER 'test'@'%' IDENTIFIED BY 'password'; SELECT 'amount' @name")]
	public void ParameterAfterWordOrQuotedStringIsSubstituted(string sql)
	{
		var parameters = new MySqlParameterCollection();
		parameters.AddWithValue("@name", 123);
		var parsedSql = GetParsedSql(sql, parameters);
		Assert.Equal(sql.Replace("@name", "123"), parsedSql);
	}

	[Fact]
	public void UndefinedParameterAfterWordWithoutSpaceThrows()
	{
		var exception = Assert.Throws<MySqlException>(() => GetParsedSql("SELECT * FROM t WHERE c LIKE@name"));
		Assert.Contains("'@name' must be defined", exception.Message);
	}

	public static IEnumerable<object[]> FormatParameterData =>
		new[]
		{
			new object[] { (byte) 200, "200" },
			[(sbyte) -100, "-100"],
			[(short) -12345, "-12345"],
			[(ushort) 45678, "45678"],
			[-1_234_567_890, "-1234567890"],
			[3_456_789_012u, "3456789012"],
			[-12_345_678_901L, "-12345678901"],
			[12_345_678_901UL, "12345678901"],
#if NET481
			[1.0123456f, "1.01234555"],
#else
			[1.0123456f, "1.0123456"],
#endif
			[1.0123456789012346, "1.0123456789012346"],
			[123456789.123456789m, "123456789.123456789"],
			["1234", "'1234'"],
			["it's", "'it''s'"],
			["it's", "'it''s'", true],
			['a', "'a'"],
			['\'', "''''"],
			['\'', "''''", true],
			['\\', "'\\\\'"],
			['\\', "'\\'", true],
			["\\'", "'\\\\'''"],
			["\\'", "'\\'''", true],
			['ﬃ', "'ﬃ'"],
			[new DateTime(1234, 12, 23, 12, 34, 56, 789), "timestamp('1234-12-23 12:34:56.789000')"],
			[new DateTimeOffset(1234, 12, 23, 12, 34, 56, 789, TimeSpan.FromHours(2)), "timestamp('1234-12-23 10:34:56.789000')"],
			[new TimeSpan(2, 3, 4, 5, 6), "time '51:04:05.006000'"],
			[new TimeSpan(-2, -3, -4, -5, -6), "time '-51:04:05.006000'"],
			[new Guid("00112233-4455-6677-8899-AABBCCDDEEFF"), "'00112233-4455-6677-8899-aabbccddeeff'"],
			[new byte[] { 0x41, 0x42, 0x27, 0x61, 0x5c, 0x62 }, @"_binary'AB''a\\b'"],
			[new byte[] { 0x41, 0x42, 0x27, 0x61, 0x5c, 0x62 }, @"_binary'AB''a\b'", true],
			[new MemoryStream([0x41, 0x42, 0x27, 0x61, 0x5c, 0x62]), @"_binary'AB''a\\b'"],
			[new MemoryStream([0x41, 0x42, 0x27, 0x61, 0x5c, 0x62]), @"_binary'AB''a\b'", true],
			[new MemoryStream([0, 0x41, 0x42, 0x27, 0x61, 0x5c, 0x62, 0x63], 1, 6, false, true), @"_binary'AB''a\\b'"],
			[new MemoryStream([0, 0x41, 0x42, 0x27, 0x61, 0x5c, 0x62, 0x63], 1, 6, false, true), @"_binary'AB''a\b'", true],
			["\"AB\\ab'".AsMemory(), @"'""AB\\ab'''"],
			[new StringBuilder("\"AB\\ab'"), @"'""AB\\ab'''"],
		};

	[Theory]
	[InlineData(StatementPreparerOptions.GuidFormatChar36, "'61626364-6566-6768-696a-6b6c6d6e6f70'")]
	[InlineData(StatementPreparerOptions.GuidFormatChar32, "'6162636465666768696a6b6c6d6e6f70'")]
	[InlineData(StatementPreparerOptions.GuidFormatBinary16, "_binary'abcdefghijklmnop'")]
	[InlineData(StatementPreparerOptions.GuidFormatTimeSwapBinary16, "_binary'ghefabcdijklmnop'")]
	[InlineData(StatementPreparerOptions.GuidFormatLittleEndianBinary16, "_binary'dcbafehgijklmnop'")]
	public void GuidFormat(object options, string replacedValue)
	{
		var parameters = new MySqlParameterCollection { new("@param", new Guid("61626364-6566-6768-696a-6b6c6d6e6f70")) };
		const string sql = "SELECT @param;";
		var parsedSql = GetParsedSql(sql, parameters, (StatementPreparerOptions) options);
		Assert.Equal(sql.Replace("@param", replacedValue), parsedSql);
	}

	[Theory]
	[InlineData("SELECT 1;", "SELECT 1;", false, true)]
	[InlineData("SELECT 1;", "SELECT 1;", true, true)]
	[InlineData("SELECT 1", "SELECT 1", false, true)]
	[InlineData("SELECT 1", "SELECT 1;", true, true)]
	[InlineData("SELECT 1 -- comment", "SELECT 1 -- comment\n", false, true)]
	[InlineData("SELECT 1 -- comment", "SELECT 1 -- comment\n;", true, true)]
	[InlineData("SELECT 1 # comment", "SELECT 1 # comment\n", false, true)]
	[InlineData("SELECT 1 # comment", "SELECT 1 # comment\n;", true, true)]
	[InlineData("SELECT '1", "SELECT '1", false, false)]
	[InlineData("SELECT '1", "SELECT '1", true, false)]
	[InlineData("SELECT '1' /* test", "SELECT '1' /* test", false, false)]
	[InlineData("SELECT '1';", "SELECT '1';", false, true)]
	[InlineData("SELECT '1';", "SELECT '1';", true, true)]
	[InlineData("SELECT '1'", "SELECT '1'", false, true)]
	[InlineData("SELECT '1'", "SELECT '1';", true, true)]
	[InlineData("SELECT \"1\";", "SELECT \"1\";", false, true)]
	[InlineData("SELECT \"1\";", "SELECT \"1\";", true, true)]
	[InlineData("SELECT \"1\"", "SELECT \"1\"", false, true)]
	[InlineData("SELECT \"1\"", "SELECT \"1\";", true, true)]
	[InlineData("SELECT * FROM `SELECT`;", "SELECT * FROM `SELECT`;", false, true)]
	[InlineData("SELECT * FROM `SELECT`", "SELECT * FROM `SELECT`", false, true)]
	[InlineData("SELECT * FROM test WHERE id = ?;", "SELECT * FROM test WHERE id = 0;", false, true)]
	[InlineData("SELECT * FROM test WHERE id = ?", "SELECT * FROM test WHERE id = 0", false, true)]
	[InlineData("SELECT 'a' ?", "SELECT 'a' 0;", true, true)]
	[InlineData("SELECT * FROM test LIMIT?", "SELECT * FROM test LIMIT0;", true, true)]
	[InlineData("SELECT 'a' ", "SELECT 'a' ;", true, true)]
	[InlineData("SELECT 'a' -- comment", "SELECT 'a' -- comment\n;", true, true)]
	[InlineData("SELECT @@version # comment", "SELECT @@version # comment\n;", true, true)]
	[InlineData("SELECT@@version#comment", "SELECT@@version#comment\n;", true, true)]
	[InlineData("CREATE USER 'test'@'%'", "CREATE USER 'test'@'%';", true, true)]
	[InlineData("CREATE USER 'test'@localhost", "CREATE USER 'test'@localhost;", true, true)]
	public void CompleteStatements(string sql, string expectedSql, bool appendSemicolon, bool expectedComplete)
	{
		var parameters = new MySqlParameterCollection { new() { Value = 0 } };
		var preparer = new StatementPreparer(sql, parameters, appendSemicolon ? StatementPreparerOptions.AppendSemicolon : StatementPreparerOptions.None);
		var writer = new ByteBufferWriter();
		var isComplete = preparer.ParseAndBindParameters(writer);
		Assert.Equal(expectedComplete, isComplete);
		string parsedSql;
		using (var payload = writer.ToPayloadData())
			parsedSql = Encoding.UTF8.GetString(payload.Span);
		Assert.Equal(expectedSql, parsedSql);
	}

	[Theory]
	[InlineData("SELECT 1", new[] { "SELECT 1" }, "")]
	[InlineData("SELECT 1;", new[] { "SELECT 1" }, "")]
	[InlineData("\r\n-- leading comment\r\nSELECT 1;\r\n\r\n-- trailing comment", new[] { "SELECT 1" }, "")]
	[InlineData("SELECT 1; SELECT 2;", new[] { "SELECT 1", "SELECT 2" }, ";")]
	[InlineData("SELECT ?;", new[] { "SELECT ?" }, "0")]
	[InlineData("SELECT ?, ?;", new[] { "SELECT ?, ?" }, "0,1")]
	[InlineData("SELECT ?, ?; SELECT ?, ?;", new[] { "SELECT ?, ?", "SELECT ?, ?" }, "0,1;2,3")]
	[InlineData("SELECT @one, @two;", new[] { "SELECT ?, ?" }, "@one,@two")]
	[InlineData("SELECT @one, @two; SELECT @zero, @three", new[] { "SELECT ?, ?", "SELECT ?, ?" }, "@one,@two;@zero,@three")]
	[InlineData("SELECT @`one`, 'two';", new[] { "SELECT ?, 'two'" }, "@`one`")]
	[InlineData("SELECT @'one', 'two';", new[] { "SELECT ?, 'two'" }, "@'one'")]
	[InlineData("SELECT @\"one\", 'two';", new[] { "SELECT ?, 'two'" }, "@\"one\"")]
	[InlineData("SELECT ?, ?; SELECT ?, ?", new[] { "SELECT ?, ?", "SELECT ?, ?" }, "0,1;2,3")]
	[InlineData("SELECT '@one' FROM `@three` WHERE `@zero` = @two;", new[] { "SELECT '@one' FROM `@three` WHERE `@zero` = ?" }, "@two")]
	[InlineData("SELECT ?, @one;", new[] { "SELECT ?, ?" }, "0,@one")]
	[InlineData("SELECT@one;", new[] { "SELECT?" }, "@one")]
	[InlineData("SELECT@@version, @one;", new[] { "SELECT@@version, ?" }, "@one")]
	[InlineData("SELECT@@version; SELECT@two", new[] { "SELECT@@version", "SELECT?" }, ";@two")]
	[InlineData("SELECT * FROM t WHERE name LIKE '%' @one '%';", new[] { "SELECT * FROM t WHERE name LIKE '%' ? '%'" }, "@one")]
	[InlineData("SELECT 1 -- @one\n; SELECT @two", new[] { "SELECT 1 -- @one\n", "SELECT ?" }, ";@two")]
	[InlineData("CREATE USER 'test'@'%'; SELECT @two;", new[] { "CREATE USER 'test'@'%'", "SELECT ?" }, ";@two")]
	public void SplitStatement(string sql, string[] expectedStatements, string expectedStatementParametersString)
	{
		// verify InlineData is in the expected format
		var expectedStatementParameters = expectedStatementParametersString.Split(';');
		Assert.Equal(expectedStatements.Length, expectedStatementParameters.Length);

		// make some dummy parameters available to the test input
		var parameters = new MySqlParameterCollection
		{
			new("@zero", 0),
			new("@one", 0),
			new("@two", 0),
			new("@three", 0),
		};

		var preparer = new StatementPreparer(sql, parameters, StatementPreparerOptions.None);
		using var parsedStatements = preparer.SplitStatements();
		var splitStatements = parsedStatements.Statements;
		Assert.Equal(expectedStatements.Length, splitStatements.Count);
		for (var i = 0; i < splitStatements.Count; i++)
		{
			var parsedSql = Encoding.UTF8.GetString(splitStatements[i].StatementBytes.Slice(1));
			Assert.Equal(expectedStatements[i], parsedSql);

			var expectedParameterNamesOrIndexes = expectedStatementParameters[i].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			var expectedParameterIndexes = new int[expectedParameterNamesOrIndexes.Length];
			var expectedParameterNames = new string[expectedParameterNamesOrIndexes.Length];
			for (var j = 0; j < expectedParameterNamesOrIndexes.Length; j++)
			{
				if (expectedParameterNamesOrIndexes[j][0] == '@')
				{
					expectedParameterNames[j] = expectedParameterNamesOrIndexes[j];
					expectedParameterIndexes[j] = -1;
				}
				else
				{
					expectedParameterIndexes[j] = int.Parse(expectedParameterNamesOrIndexes[j], CultureInfo.InvariantCulture);
				}
			}

			Assert.Equal(expectedParameterIndexes, splitStatements[i].ParameterIndexes);
			Assert.Equal(expectedParameterNames, splitStatements[i].ParameterNames);
		}
	}

	// asserts that every '@param' in 'sql' (and nothing else) is treated as a parameter, both when binding parameters
	// client-side and when splitting the SQL into statements for server-side preparation
	private static void AssertParametersBound(string sql, StatementPreparerOptions options = StatementPreparerOptions.None)
	{
		var parameters = new MySqlParameterCollection { new("@param", "abc") };
		Assert.Equal(sql.Replace("@param", "'abc'"), GetParsedSql(sql, parameters, options));

		var preparer = new StatementPreparer(sql, parameters, options);
		using var parsedStatements = preparer.SplitStatements();
		var statement = Assert.Single(parsedStatements.Statements);
		Assert.Equal(sql.Replace("@param", "?").TrimEnd(';'), Encoding.UTF8.GetString(statement.StatementBytes.Slice(1)));
		var parameterCount = sql.Split(new[] { "@param" }, StringSplitOptions.None).Length - 1;
		Assert.Equal(Enumerable.Repeat("@param", parameterCount), statement.ParameterNames);
	}

	private static string GetParsedSql(string input, MySqlParameterCollection parameters = null, StatementPreparerOptions options = StatementPreparerOptions.None)
	{
		var preparer = new StatementPreparer(input, parameters ?? [], options);
		var writer = new ByteBufferWriter();
		preparer.ParseAndBindParameters(writer);
		using var payload = writer.ToPayloadData();
		return Encoding.UTF8.GetString(payload.Span);
	}
}
