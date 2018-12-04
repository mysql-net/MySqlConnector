using Dapper;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ConnectionTests : IClassFixture<DatabaseFixture>
	{
		public ConnectionTests(DatabaseFixture database)
		{
		}

		[Fact]
		public void GotInfoMessageForNonExistentTable()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();

				var gotEvent = false;
				connection.InfoMessage += (s, a) =>
				{
					gotEvent = true;
					Assert.Single(a.errors);
					Assert.Equal((int) MySqlErrorCode.BadTable, a.errors[0].Code);
				};

				connection.Execute(@"drop table if exists table_does_not_exist;");
				Assert.True(gotEvent);
			}
		}

		[Fact]
		public void NoInfoMessageWhenNotLastStatementInBatch()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();

				var gotEvent = false;
				connection.InfoMessage += (s, a) =>
				{
					gotEvent = true;

					// seeming bug in Connector/NET raises an event with no errors
					Assert.Empty(a.errors);
				};

				connection.Execute(@"drop table if exists table_does_not_exist; select 1;");
#if BASELINE
				Assert.True(gotEvent);
#else
				Assert.False(gotEvent);
#endif
			}
		}
	}
}
