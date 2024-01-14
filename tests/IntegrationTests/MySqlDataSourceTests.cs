#if !MYSQL_DATA
namespace IntegrationTests;

public class MySqlDataSourceTests : IClassFixture<DatabaseFixture>
{
	public MySqlDataSourceTests(DatabaseFixture _)
	{
	}

	[Fact]
	public void CreateConnectionConnectionString()
	{
		var connectionString = AppConfig.ConnectionString;
		using var dbSource = new MySqlDataSource(connectionString);
		using var connection = dbSource.CreateConnection();
		Assert.Equal(ConnectionState.Closed, connection.State);
		Assert.Equal(connectionString, connection.ConnectionString);
	}

	[Fact]
	public void OpenConnection()
	{
		using var dbSource = new MySqlDataSource(AppConfig.ConnectionString);
		using var connection = dbSource.OpenConnection();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[Fact]
	public void DoubleDispose()
	{
		using var dbSource = new MySqlDataSource(AppConfig.ConnectionString);
		using (var connection = dbSource.OpenConnection())
		{
			Assert.Equal(ConnectionState.Open, connection.State);
		}
		dbSource.Dispose();
	}

	[Fact]
	public async Task OpenConnectionAsync()
	{
		using var dbSource = new MySqlDataSource(AppConfig.ConnectionString);
		using var connection = await dbSource.OpenConnectionAsync();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[Fact]
	public void OpenConnectionReusesConnection()
	{
		using var dbSource = new MySqlDataSource(AppConfig.ConnectionString);

		int serverThread;
		using (var connection = dbSource.OpenConnection())
		{
			serverThread = connection.ServerThread;
		}

		using (var connection = dbSource.OpenConnection())
		{
			Assert.Equal(serverThread, connection.ServerThread);
		}
	}

	[Fact]
	public void CloneReusesDataSource()
	{
		using var dbSource = new MySqlDataSource(AppConfig.ConnectionString);

		using var connection1 = dbSource.CreateConnection();
		using var connection2 = connection1.Clone();

		connection1.Open();
		var serverThread = connection1.ServerThread;
		connection1.Close();

		connection2.Open();
		Assert.Equal(serverThread, connection2.ServerThread);
	}

	[Fact]
	public void MultipleDataSourcesHaveDifferentPools()
	{
		using var dbSource1 = new MySqlDataSource(AppConfig.ConnectionString);
		using var dbSource2 = new MySqlDataSource(AppConfig.ConnectionString);

		int serverThread;
		using (var connection = dbSource1.OpenConnection())
		{
			serverThread = connection.ServerThread;
		}

		using (var connection = dbSource2.OpenConnection())
		{
			Assert.NotEqual(serverThread, connection.ServerThread);
		}
	}

	[Fact]
	public void NonPoolingDataSourceDoesNotReuseConnections()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Pooling = false;
		using var dbSource = new MySqlDataSource(csb.ConnectionString);

		int serverThread;
		using (var connection = dbSource.OpenConnection())
		{
			serverThread = connection.ServerThread;
		}

		using (var connection = dbSource.OpenConnection())
		{
			Assert.NotEqual(serverThread, connection.ServerThread);
		}
	}

	[Fact]
	public void CreateFromDbFactory()
	{
		using var dbSource = MySqlConnectorFactory.Instance.CreateDataSource(AppConfig.ConnectionString);
		Assert.IsType<MySqlDataSource>(dbSource);
		Assert.Equal(AppConfig.ConnectionString, dbSource.ConnectionString);
	}

	[Fact]
	public void CreateFromDataSourceBuilder()
	{
		var connectionString = AppConfig.CreateConnectionStringBuilder().ConnectionString;
		var builder = new MySqlDataSourceBuilder(connectionString);
		using var dataSource = builder.Build();
		Assert.Equal(connectionString, dataSource.ConnectionString);
		using var connection = dataSource.OpenConnection();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[SkippableTheory(ServerFeatures.KnownCertificateAuthority, ConfigSettings.RequiresSsl)]
	[InlineData(MySqlSslMode.VerifyCA, false, false)]
	[InlineData(MySqlSslMode.VerifyCA, true, false)]
	[InlineData(MySqlSslMode.Required, true, true)]
	public async Task ConnectSslRemoteCertificateValidationCallback(MySqlSslMode sslMode, bool clearCA, bool expectedSuccess)
	{
		var builder = new MySqlDataSourceBuilder(AppConfig.ConnectionString)
			.UseRemoteCertificateValidationCallback((s, c, h, e) => true);
		builder.ConnectionStringBuilder.CertificateFile = Path.Combine(AppConfig.CertsPath, "ssl-client.pfx");
		builder.ConnectionStringBuilder.SslMode = sslMode;
		builder.ConnectionStringBuilder.SslCa = clearCA ? "" : Path.Combine(AppConfig.CertsPath, "non-ca-client-cert.pem");

		using var dataSource = builder.Build();
		using var connection = dataSource.CreateConnection();
		if (expectedSuccess)
			await connection.OpenAsync();
		else
			await Assert.ThrowsAsync<MySqlException>(connection.OpenAsync);
	}

	[Fact]
	public void PasswordPropertyIsUsed()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var password = csb.Password;
		csb.Password = "";

		using var dataSource = new MySqlDataSource(csb.ConnectionString)
		{
			Password = password,
		};
		using var connection = dataSource.OpenConnection();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[Fact]
	public void PasswordProviderCallbackIsInvoked()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var password = csb.Password;
		csb.Password = "";

		using var dataSource = new MySqlDataSourceBuilder(csb.ConnectionString)
			.UsePeriodicPasswordProvider((_, _) => new ValueTask<string>(password), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1))
			.Build();
		using var connection = dataSource.OpenConnection();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[Fact]
	public void PasswordProviderCallbackIsWaitedFor()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var password = csb.Password;
		csb.Password = "";

		using var dataSource = new MySqlDataSourceBuilder(csb.ConnectionString)
			.UsePeriodicPasswordProvider(async (_, _) =>
			{
				await Task.Delay(TimeSpan.FromSeconds(0.5));
				return password;
			}, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1))
			.Build();
		using var connection = dataSource.OpenConnection();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[Fact]
	public void PasswordProviderFailureIsRetried()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var password = csb.Password;
		csb.Password = "";

		using var barrier = new Barrier(2);

		var count = 0;
		using var dataSource = new MySqlDataSourceBuilder(csb.ConnectionString)
			.UsePeriodicPasswordProvider((_, _) =>
			{
				if (count++ == 0)
					throw new ApplicationException("First-time failure");
				barrier.SignalAndWait();
				return new ValueTask<string>(password);
			}, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(0.5))
			.Build();

		// throws the first time
		var exception = Assert.Throws<MySqlException>(dataSource.OpenConnection); // Failed to obtain password via ProvidePasswordCallback
		Assert.IsType<MySqlException>(exception.InnerException); // The periodic password provider failed
		Assert.IsType<ApplicationException>(exception.InnerException.InnerException); // First-time failure

		// succeeds after failure retry
		barrier.SignalAndWait();
		Thread.Sleep(10);
		using var connection = dataSource.OpenConnection();
		Assert.Equal(ConnectionState.Open, connection.State);
	}
}
#endif
