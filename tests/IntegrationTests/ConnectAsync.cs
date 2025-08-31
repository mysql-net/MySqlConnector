using System.Security.Authentication;

namespace IntegrationTests;

public class ConnectAsync : IClassFixture<DatabaseFixture>
{
	public ConnectAsync(DatabaseFixture database)
	{
		m_database = database;
	}

	[Fact]
	public async Task ConnectBadHost()
	{
		var csb = new MySqlConnectionStringBuilder
		{
			Server = "invalid.example.com",
		};
		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(ConnectionState.Closed, connection.State);
		var ex = await Assert.ThrowsAsync<MySqlException>(connection.OpenAsync);
		Assert.Equal((int) MySqlErrorCode.UnableToConnectToHost, ex.Number);
		Assert.Equal((int) MySqlErrorCode.UnableToConnectToHost, ex.Data["Server Error Code"]);
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

	[Fact]
	public async Task ConnectBadPort()
	{
		var csb = new MySqlConnectionStringBuilder
		{
			Server = "localhost",
			Port = 65000,
		};
		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(ConnectionState.Closed, connection.State);
		await Assert.ThrowsAsync<MySqlException>(() => connection.OpenAsync());
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

	[Fact]
	public async Task ConnectBadPassword()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Password = "wrong";
		using var connection = new MySqlConnection(csb.ConnectionString);
		await Assert.ThrowsAsync<MySqlException>(() => connection.OpenAsync());
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

	[SkippableFact(MySqlData = "https://bugs.mysql.com/bug.php?id=110791")]
	public async Task ConnectCanceled()
	{
		using var cts = new CancellationTokenSource();
		cts.Cancel();

		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		await Assert.ThrowsAsync<OperationCanceledException>(async () => await connection.OpenAsync(cts.Token));
		await connection.OpenAsync();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[Fact]
	public async Task State()
	{
		using var connection = new MySqlConnection(m_database.Connection.ConnectionString);
		Assert.Equal(ConnectionState.Closed, connection.State);
		await connection.OpenAsync();
		Assert.Equal(ConnectionState.Open, connection.State);
		await connection.CloseAsync();
		Assert.Equal(ConnectionState.Closed, connection.State);
		await connection.OpenAsync();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[SkippableFact(ConfigSettings.TcpConnection, MySqlData = "https://bugs.mysql.com/bug.php?id=81650")]
	public async Task ConnectMultipleHostNames()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Server = "invalid.example.net," + csb.Server;

		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(ConnectionState.Closed, connection.State);
		await connection.OpenAsync();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[SkippableFact(ConfigSettings.PasswordlessUser)]
	public async Task ConnectNoPassword()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.UserID = AppConfig.PasswordlessUser;
		csb.Password = "";
		csb.Database = "";

		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(ConnectionState.Closed, connection.State);
		await connection.OpenAsync().ConfigureAwait(false);
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[Fact]
	public async Task ConnectKeepAlive()
	{
		// the goal of this test is to ensure that no exceptions are thrown
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Keepalive = 1;
		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();
		await Task.Delay(3000);
	}

	[SkippableFact(ServerFeatures.Timeout)]
	public async Task ConnectTimeoutAsync()
	{
		var csb = new MySqlConnectionStringBuilder
		{
			Server = "www.mysql.com",
			Pooling = false,
			ConnectionTimeout = 2,
		};

		using var connection = new MySqlConnection(csb.ConnectionString);
		var stopwatch = Stopwatch.StartNew();
		var ex = await Assert.ThrowsAsync<MySqlException>(connection.OpenAsync);
		stopwatch.Stop();
		Assert.Equal((int) MySqlErrorCode.UnableToConnectToHost, ex.Number);
		TestUtilities.AssertDuration(stopwatch, 1900, 1500);
	}

	[SkippableFact(ServerFeatures.Timeout, MySqlData = "https://bugs.mysql.com/bug.php?id=94760")]
	public async Task ConnectTimeoutAsyncCancellationToken()
	{
		// skip on Azure Pipelines, as this one seems to consistently time out
		if (Environment.GetEnvironmentVariable("TF_BUILD") == "True")
			return;

		var csb = new MySqlConnectionStringBuilder
		{
			Server = "www.mysql.com",
			Pooling = false,
		};

		using var connection = new MySqlConnection(csb.ConnectionString);
		var stopwatch = Stopwatch.StartNew();
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
		var task = connection.OpenAsync(cts.Token);
		var ex = await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
		stopwatch.Stop();
		Assert.Equal(TaskStatus.Canceled, task.Status);
		Assert.Equal(cts.Token, ex.CancellationToken);
		TestUtilities.AssertDuration(stopwatch, 1900, 1500);
	}

#if !MYSQL_DATA
	[Fact]
	public async Task UsePasswordProvider()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var password = csb.Password;
		csb.Password = null;

		using var connection = new MySqlConnection(csb.ConnectionString);
		await MySqlConnection.ClearPoolAsync(connection);

		var wasCalled = false;
		connection.ProvidePasswordCallback = x =>
		{
			Assert.Equal(csb.Server, x.Server);
			Assert.Equal((int) csb.Port, x.Port);
			Assert.Equal(csb.UserID, x.UserId);
			Assert.Equal(csb.Database, x.Database);
			wasCalled = true;
			return password;
		};

		await connection.OpenAsync();
		Assert.True(wasCalled);
	}

	[SkippableFact(ConfigSettings.UserHasPassword)]
	public async Task UsePasswordProviderPasswordTakesPrecedence()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var password = csb.Password;

		using var connection = new MySqlConnection(csb.ConnectionString);
		await MySqlConnection.ClearPoolAsync(connection);

		var wasCalled = false;
		connection.ProvidePasswordCallback = _ =>
		{
			wasCalled = true;
			return password;
		};

		await connection.OpenAsync();
		Assert.False(wasCalled);
	}

	[Fact]
	public async Task UsePasswordProviderWithBadPassword()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var password = csb.Password;
		csb.Password = null;

		using var connection = new MySqlConnection(csb.ConnectionString);
		await MySqlConnection.ClearPoolAsync(connection);

		connection.ProvidePasswordCallback = _ => $"wrong_{password}";

		var ex = await Assert.ThrowsAsync<MySqlException>(async () => await connection.OpenAsync());
		Assert.Equal(MySqlErrorCode.AccessDenied, ex.ErrorCode);
	}

	[Fact]
	public async Task UsePasswordProviderWithException()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var password = csb.Password;
		csb.Password = null;

		using var connection = new MySqlConnection(csb.ConnectionString);
		await MySqlConnection.ClearPoolAsync(connection);

		var innerException = new NotSupportedException();
		connection.ProvidePasswordCallback = _ => throw innerException;

		var ex = await Assert.ThrowsAsync<MySqlException>(async () => await connection.OpenAsync());
		Assert.Equal(MySqlErrorCode.ProvidePasswordCallbackFailed, ex.ErrorCode);
		Assert.Same(innerException, ex.InnerException);
	}

	[Fact]
	public async Task UsePasswordProviderClone()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var password = csb.Password;
		csb.Password = null;

		using var connection = new MySqlConnection(csb.ConnectionString);
		MySqlConnection.ClearPool(connection);
		connection.ProvidePasswordCallback = _ => password;

		using var clonedConnection = connection.Clone();
		await clonedConnection.OpenAsync();
		Assert.Equal(ConnectionState.Closed, connection.State);
		Assert.Equal(ConnectionState.Open, clonedConnection.State);
	}

	[SkippableFact(ServerFeatures.ResetConnection)]
	public async Task UsePasswordProviderWithMinimumPoolSize()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var password = csb.Password;
		csb.Password = null;
		csb.MinimumPoolSize = 3;
		csb.MaximumPoolSize = 101;

		using var connection = new MySqlConnection(csb.ConnectionString);
		MySqlConnection.ClearPool(connection);

		var invocationCount = 0;
		connection.ProvidePasswordCallback = _ =>
		{
			invocationCount++;
			return password;
		};

		await connection.OpenAsync();
		Assert.Equal((int) csb.MinimumPoolSize, invocationCount);
	}
#endif

	[Fact]
	public async Task ConnectionDatabase()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(csb.Database, connection.Database);

		await connection.OpenAsync();

		Assert.Equal(csb.Database, connection.Database);
		Assert.Equal(csb.Database, await QueryCurrentDatabaseAsync(connection));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public async Task ChangeDatabase()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();

		Assert.Equal(csb.Database, connection.Database);
		Assert.Equal(csb.Database, await QueryCurrentDatabaseAsync(connection));

		await connection.ChangeDatabaseAsync(AppConfig.SecondaryDatabase);

		Assert.Equal(AppConfig.SecondaryDatabase, connection.Database);
		Assert.Equal(AppConfig.SecondaryDatabase, await QueryCurrentDatabaseAsync(connection));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public async Task ChangeDatabaseNotOpen()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		await Assert.ThrowsAsync<InvalidOperationException>(() => connection.ChangeDatabaseAsync(AppConfig.SecondaryDatabase));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public async Task ChangeDatabaseNull()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		await Assert.ThrowsAsync<ArgumentException>(() => connection.ChangeDatabaseAsync(null));
		await Assert.ThrowsAsync<ArgumentException>(() => connection.ChangeDatabaseAsync(""));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public async Task ChangeDatabaseInvalidName()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		await Assert.ThrowsAsync<MySqlException>(() => connection.ChangeDatabaseAsync($"not_a_real_database_1234"));

		Assert.Equal(ConnectionState.Open, connection.State);
		Assert.Equal(csb.Database, connection.Database);
		Assert.Equal(csb.Database, await QueryCurrentDatabaseAsync(connection));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public async Task ChangeDatabaseConnectionPooling()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Pooling = true;
		csb.MinimumPoolSize = 0;
		csb.MaximumPoolSize = 6;

		for (int i = 0; i < csb.MaximumPoolSize * 2; i++)
		{
			using var connection = new MySqlConnection(csb.ConnectionString);
			await connection.OpenAsync();

			Assert.Equal(csb.Database, connection.Database);
			Assert.Equal(csb.Database, await QueryCurrentDatabaseAsync(connection));

			await connection.ChangeDatabaseAsync(AppConfig.SecondaryDatabase);

			Assert.Equal(AppConfig.SecondaryDatabase, connection.Database);
			Assert.Equal(AppConfig.SecondaryDatabase, await QueryCurrentDatabaseAsync(connection));
		}
	}

	[SkippableFact(ServerFeatures.SessionTrack, ConfigSettings.SecondaryDatabase, MySqlData = "https://bugs.mysql.com/bug.php?id=89085")]
	public async Task UseDatabase()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		Assert.Equal(csb.Database, connection.Database);
		Assert.Equal(csb.Database, await QueryCurrentDatabaseAsync(connection));

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = $"USE {AppConfig.SecondaryDatabase};";
			await cmd.ExecuteNonQueryAsync();
		}

		Assert.Equal(AppConfig.SecondaryDatabase, connection.Database);
		Assert.Equal(AppConfig.SecondaryDatabase, await QueryCurrentDatabaseAsync(connection));
	}

	private static async Task<string> QueryCurrentDatabaseAsync(MySqlConnection connection)
	{
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT DATABASE()";
		return (string) await cmd.ExecuteScalarAsync();
	}

	[SkippableFact(ServerFeatures.Sha256Password, ConfigSettings.RequiresSsl)]
	public async Task Sha256WithSecureConnection()
	{
		var csb = AppConfig.CreateSha256ConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();
	}

	[SkippableFact(ServerFeatures.Sha256Password)]
	public async Task Sha256WithoutSecureConnection()
	{
		var csb = AppConfig.CreateSha256ConnectionStringBuilder();
		csb.SslMode = MySqlSslMode.Disabled;
		csb.AllowPublicKeyRetrieval = true;
		using var connection = new MySqlConnection(csb.ConnectionString);
		if (AppConfig.SupportedFeatures.HasFlag(ServerFeatures.RsaEncryption))
			await connection.OpenAsync();
		else
			await Assert.ThrowsAsync<MySqlException>(() => connection.OpenAsync());
	}

	[SkippableFact(ServerFeatures.CachingSha2Password, ConfigSettings.RequiresSsl)]
	public async Task CachingSha2WithSecureConnection()
	{
		var csb = AppConfig.CreateCachingSha2ConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();
	}

	[SkippableFact(ServerFeatures.CachingSha2Password)]
	public async Task CachingSha2WithoutSecureConnection()
	{
		var csb = AppConfig.CreateCachingSha2ConnectionStringBuilder();
		csb.SslMode = MySqlSslMode.Disabled;
		csb.AllowPublicKeyRetrieval = true;
		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();
	}

#if !MYSQL_DATA
	[SkippableFact(ServerFeatures.Ed25519)]
	public async Task Ed25519Authentication()
	{
		MySqlConnector.Authentication.Ed25519.Ed25519AuthenticationPlugin.Install();

		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.UserID = "ed25519user";
		csb.Password = "Ed255!9";
		csb.Database = null;
		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();
	}

	[SkippableFact(ServerFeatures.Ed25519)]
	public async Task MultiAuthentication()
	{
		MySqlConnector.Authentication.Ed25519.Ed25519AuthenticationPlugin.Install();
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.UserID = "multiAuthUser";
		csb.Password = "secret";
		csb.Database = null;
		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();
	}

	[SkippableFact(ServerFeatures.ParsecAuthentication)]
	public async Task Parsec()
	{
		MySqlConnector.Authentication.Ed25519.ParsecAuthenticationPlugin.Install();
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.UserID = "parsec-user";
		csb.Password = "P@rs3c-Pa55";
		csb.Database = null;
		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();
	}
#endif

	// To create a MariaDB GSSAPI user for a current user
	// - install plugin if not already done , e.g mysql -uroot -e "INSTALL SONAME 'auth_gssapi'"
	// create MariaDB's gssapi user
	// a) Windows, easy way
	//    mysql -uroot -e "CREATE USER  %USERNAME% IDENTIFIED WITH gssapi"
	// b) more involved , Windows, outside of domain
	//    mysql -uroot -e "CREATE USER gssapi_user IDENTIFIED WITH gssapi as '%USERDOMAIN%\%USERNAME%'"
	// c) Windows, inside domain
	//    mysql -uroot -e "CREATE USER gssapi_user IDENTIFIED WITH gssapi as '%USERNAME%@%USERDNSDOMAIN%'"
	// d) Linux, domain (or Kerberos Realm) user
	//    NAME=`klist|grep 'Default principal' |awk '{print $3}'` mysql -uroot -e "CREATE USER gssapi_user IDENTIFIED WITH gssapi AS '$NAME'"
	[SkippableFact(ConfigSettings.GSSAPIUser)]
	public async Task AuthGSSAPI()
	{
		var csb = AppConfig.CreateGSSAPIConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();
	}

#if !MYSQL_DATA
	[SkippableFact(ConfigSettings.GSSAPIUser | ConfigSettings.HasKerberos)]
	public async Task GoodServerSPN()
	{
		var csb = AppConfig.CreateGSSAPIConnectionStringBuilder();
		string serverSPN;

		// Use server's variable gssapi_principal_name as SPN
		using (var connection = new MySqlConnection(csb.ConnectionString))
		{
			await connection.OpenAsync();
			using var cmd = connection.CreateCommand();
			cmd.CommandText = "select @@gssapi_principal_name";
			serverSPN = (string) await cmd.ExecuteScalarAsync();
		}
		csb.ServerSPN = serverSPN;
		using (var connection = new MySqlConnection(csb.ConnectionString))
		{
			await connection.OpenAsync();
		}
	}

	[SkippableFact(ConfigSettings.GSSAPIUser)]
	public async Task BadServerSPN()
	{
		var csb = AppConfig.CreateGSSAPIConnectionStringBuilder();
		csb.ServerSPN = "BadServerSPN";
		using var connection = new MySqlConnection(csb.ConnectionString);
		await Assert.ThrowsAsync<AuthenticationException>(() => connection.OpenAsync());
	}

	[Fact]
	public async Task PingNoConnection()
	{
		using var connection = new MySqlConnection();
		Assert.False(await connection.PingAsync());
	}

	[Fact]
	public async Task PingBeforeConnecting()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		Assert.False(await connection.PingAsync());
	}

	[Fact]
	public async Task PingConnection()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		await connection.OpenAsync();
		Assert.True(await connection.PingAsync());
	}
#endif

	[SkippableFact(ServerFeatures.UnixDomainSocket)]
	public async Task UnixDomainSocket()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Server = AppConfig.SocketPath;
		csb.ConnectionProtocol = MySqlConnectionProtocol.Unix;
		using var connection = new MySqlConnection(csb.ConnectionString);
		await connection.OpenAsync();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[SkippableFact(MySqlData = "Not raised")]
	public async Task DisposeAsyncRaisesDisposed()
	{
		var disposedCount = 0;
		var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Disposed += (sender, args) => disposedCount++;
		await connection.OpenAsync().ConfigureAwait(false);
		await connection.DisposeAsync().ConfigureAwait(false);
		Assert.Equal(1, disposedCount);
	}

	private readonly DatabaseFixture m_database;
}
