#if BASELINE
using MySqlConnectorFactory = MySql.Data.MySqlClient.MySqlClientFactory;
#endif

namespace SideBySide;

public class ClientFactoryTests
{
	[Fact]
	public void CreateCommand()
	{
		Assert.IsType<MySqlCommand>(MySqlConnectorFactory.Instance.CreateCommand());
	}

	[Fact]
	public void CreateConnection()
	{
		Assert.IsType<MySqlConnection>(MySqlConnectorFactory.Instance.CreateConnection());
	}

	[Fact]
	public void CreateConnectionStringBuilder()
	{
		Assert.IsType<MySqlConnectionStringBuilder>(MySqlConnectorFactory.Instance.CreateConnectionStringBuilder());
	}


	[Fact]
	public void CreateParameter()
	{
		Assert.IsType<MySqlParameter>(MySqlConnectorFactory.Instance.CreateParameter());
	}

	[Fact]
	public void CreateCommandBuilder()
	{
		Assert.IsType<MySqlCommandBuilder>(MySqlConnectorFactory.Instance.CreateCommandBuilder());
	}

	[Fact]
	public void CreateDataAdapter()
	{
		Assert.IsType<MySqlDataAdapter>(MySqlConnectorFactory.Instance.CreateDataAdapter());
	}

	[Fact]
	public void DbProviderFactoriesGetFactory()
	{
#if !NET452 && !NET461 && !NET472
		DbProviderFactories.RegisterFactory("MySqlConnector", MySqlConnectorFactory.Instance);
#endif
#if BASELINE
		var providerInvariantName = "MySql.Data.MySqlClient";
#else
		var providerInvariantName = "MySqlConnector";
#endif
		var factory = DbProviderFactories.GetFactory(providerInvariantName);
		Assert.NotNull(factory);
		Assert.Same(MySqlConnectorFactory.Instance, factory);

		using (var connection = new MySqlConnection())
		{
			factory = System.Data.Common.DbProviderFactories.GetFactory(connection);
			Assert.NotNull(factory);
			Assert.Same(MySqlConnectorFactory.Instance, factory);
		}
	}
}
