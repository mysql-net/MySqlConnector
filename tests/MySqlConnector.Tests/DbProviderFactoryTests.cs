using MySql.Data.MySqlClient;
using Xunit;

namespace MySqlConnector.Tests
{
	public class DbProviderFactoryTests
	{
		[Fact]
		public void CreatesExpectedTypes()
		{
			Assert.IsType<MySqlConnection>(MySqlClientFactory.Instance.CreateConnection());
			Assert.IsType<MySqlConnectionStringBuilder>(MySqlClientFactory.Instance.CreateConnectionStringBuilder());
			Assert.IsType<MySqlCommand>(MySqlClientFactory.Instance.CreateCommand());
			Assert.IsType<MySqlParameter>(MySqlClientFactory.Instance.CreateParameter());
		}

		[Fact]
		public void Singleton()
		{
			var factory1 = MySqlClientFactory.Instance;
			var factory2 = MySqlClientFactory.Instance;
			Assert.True(object.ReferenceEquals(factory1, factory2));
		}
	}
}
