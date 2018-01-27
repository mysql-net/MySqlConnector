using System.Text;
using MySql.Data.MySqlClient;
using MySqlConnector.Core;
using MySqlConnector.Utilities;
using Xunit;

namespace MySqlConnector.Tests
{
	public class StatementPreparerTests
	{
		[Theory]
		[InlineData(GoodSqlText)]
		[InlineData(AnotherGoodSqlText)]
		[InlineData(AnotherGoodSqlText2)]
		[InlineData(BadSqlText)]
		public void PrepareQuery(string sql)
		{
			var parameters = new MySqlParameterCollection();
			parameters.AddWithValue("c2", 3);

			var parsedRequest1 = Encoding.UTF8.GetString(new StatementPreparer(sql, parameters, StatementPreparerOptions.None).ParseAndBindParameters().Slice(1));

			Assert.Matches("column2 = 3", parsedRequest1);
		}

		private const string BadSqlText = @"SELECT Id
FROM mytable
WHERE column1 = 2 -- mycomment
  AND column2 = @c2";

		private const string GoodSqlText = @"SELECT Id
FROM mytable
WHERE column1 = 2
AND column2 = @c2";

		private const string AnotherGoodSqlText = @"SELECT Id
FROM mytable
WHERE column1 = 2  -- mycomment
AND column2 = @c2";

		private const string AnotherGoodSqlText2 = @"SELECT Id
FROM mytable
WHERE column1 = 2 -- mycomment
AND column2 = @c2";
	}
}
