#if BASELINE
using MySql.Data.MySqlClient;
using MySqlConnectorFactory = MySql.Data.MySqlClient.MySqlClientFactory;
#endif
using Xunit;

namespace MySqlConnector.Tests
{
	public class DbProviderFactoryTests
	{
		[Fact]
		public void CreatesExpectedTypes()
		{
			Assert.IsType<MySqlConnection>(MySqlConnectorFactory.Instance.CreateConnection());
			Assert.IsType<MySqlConnectionStringBuilder>(MySqlConnectorFactory.Instance.CreateConnectionStringBuilder());
			Assert.IsType<MySqlCommand>(MySqlConnectorFactory.Instance.CreateCommand());
			Assert.IsType<MySqlParameter>(MySqlConnectorFactory.Instance.CreateParameter());
		}

		[Fact]
		public void Singleton()
		{
			var factory1 = MySqlConnectorFactory.Instance;
			var factory2 = MySqlConnectorFactory.Instance;
			Assert.True(object.ReferenceEquals(factory1, factory2));
		}
	}
}
