namespace MySqlConnector.DependencyInjection.Tests;

public class DependencyInjectionTests
{
	[Fact]
	public async Task MySqlDataSourceIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddMySqlDataSource(c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredService<MySqlDataSource>();
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task MySqlConnectionIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddMySqlDataSource(c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var connection = serviceProvider.GetRequiredService<MySqlConnection>();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task DbConnectionIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddMySqlDataSource(c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var connection = serviceProvider.GetRequiredService<DbConnection>();
		Assert.IsAssignableFrom<MySqlConnection>(connection);
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task DbDataSourceIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddMySqlDataSource(c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var dataSource = serviceProvider.GetRequiredService<DbDataSource>();
		Assert.IsAssignableFrom<MySqlDataSource>(dataSource);
		await using var connection = dataSource.CreateConnection();
		Assert.IsAssignableFrom<MySqlConnection>(connection);
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task MySqlDataSourceCanSetName()
	{
		var serviceCollection = new ServiceCollection();

		serviceCollection.AddMySqlDataSource(c_connectionString, builder =>
		{
			builder.UseName("MyName");
		});

		await using var serviceProvider = serviceCollection.BuildServiceProvider();
		var dataSource = serviceProvider.GetRequiredService<MySqlDataSource>();
		Assert.Equal("MyName", dataSource.Name);
	}

	[Fact]
	public async Task MySqlDataSourceCanSetNameFromServiceProvider()
	{
		var serviceCollection = new ServiceCollection();

		serviceCollection.AddSingleton("MyName");
		serviceCollection.AddMySqlDataSource(c_connectionString, (sp, builder) =>
		{
			builder.UseName(sp.GetRequiredService<string>());
		});

		await using var serviceProvider = serviceCollection.BuildServiceProvider();
		var dataSource = serviceProvider.GetRequiredService<MySqlDataSource>();
		Assert.Equal("MyName", dataSource.Name);
	}

	[Fact]
	public async Task KeyedMySqlDataSourceIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedMySqlDataSource(KeyedService.AnyKey, c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredKeyedService<MySqlDataSource>(new object());
		Assert.Null(dataSource.Name);
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task StringKeyedMySqlDataSourceHasNameSet()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedMySqlDataSource("key", c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredKeyedService<MySqlDataSource>("key");
		Assert.Equal("key", dataSource.Name);
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task KeyedMySqlDataSourceCanSetName()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedMySqlDataSource("key", c_connectionString, builder => builder.UseName("MyName"));

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredKeyedService<MySqlDataSource>("key");
		Assert.Equal("MyName", dataSource.Name);
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task KeyedMySqlDataSourceCanSetNameFromServiceProvider()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddSingleton("MyName");
		serviceCollection.AddKeyedMySqlDataSource("key", c_connectionString, (sp, builder) => builder.UseName(sp.GetRequiredService<string>()));

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredKeyedService<MySqlDataSource>("key");
		Assert.Equal("MyName", dataSource.Name);
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task KeyedMySqlDataSourceRetrievedWithStringKeyHasName()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedMySqlDataSource(KeyedService.AnyKey, c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		var dataSource = serviceProvider.GetRequiredKeyedService<MySqlDataSource>("key");
		Assert.Equal("key", dataSource.Name);
		await using var connection = dataSource.CreateConnection();
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task KeyedMySqlConnectionIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedMySqlDataSource("key", c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var connection = serviceProvider.GetRequiredKeyedService<MySqlConnection>("key");
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task TwoKeyedMySqlDataConnectionsAreRegistered()
	{
		const string c_connectionString2 = c_connectionString + ";Database=test";

		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedMySqlDataSource(KeyedService.AnyKey, c_connectionString);
		serviceCollection.AddKeyedMySqlDataSource("key2", c_connectionString2);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var connection1 = serviceProvider.GetRequiredKeyedService<MySqlConnection>("key");
		Assert.Equal(c_connectionString, connection1.ConnectionString);

		await using var connection2 = serviceProvider.GetRequiredKeyedService<MySqlConnection>("key2");
		Assert.Equal(c_connectionString2, connection2.ConnectionString);
	}

	[Fact]
	public async Task KeyedDbConnectionIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedMySqlDataSource("key", c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var connection = serviceProvider.GetRequiredKeyedService<DbConnection>("key");
		Assert.IsAssignableFrom<MySqlConnection>(connection);
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	[Fact]
	public async Task KeyedDbDataSourceIsRegistered()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddKeyedMySqlDataSource("key", c_connectionString);

		await using var serviceProvider = serviceCollection.BuildServiceProvider();

		await using var dataSource = serviceProvider.GetRequiredKeyedService<DbDataSource>("key");
		Assert.IsAssignableFrom<MySqlDataSource>(dataSource);
		await using var connection = dataSource.CreateConnection();
		Assert.IsAssignableFrom<MySqlConnection>(connection);
		Assert.Equal(c_connectionString, connection.ConnectionString);
	}

	const string c_connectionString = "Server=localhost;User ID=root;Password=pass";
}
