using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace SideBySide;

public class ConnectSync : IClassFixture<DatabaseFixture>
{
	public ConnectSync(DatabaseFixture database)
	{
		m_database = database;
	}

	[Fact]
	public void ConnectBadHost()
	{
		var csb = new MySqlConnectionStringBuilder
		{
			Server = "invalid.example.com",
		};
		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(ConnectionState.Closed, connection.State);
		var ex = Assert.Throws<MySqlException>(connection.Open);
#if !BASELINE
		Assert.True(ex.IsTransient);
#endif
		Assert.Equal((int) MySqlErrorCode.UnableToConnectToHost, ex.Number);
		Assert.Equal((int) MySqlErrorCode.UnableToConnectToHost, ex.Data["Server Error Code"]);
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

	[Fact]
	public void ConnectBadPort()
	{
		var csb = new MySqlConnectionStringBuilder
		{
			Server = "localhost",
			Port = 65000,
		};
		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(ConnectionState.Closed, connection.State);
		Assert.Throws<MySqlException>(() => connection.Open());
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

	[Fact]
	public void ConnectInvalidPort()
	{
		var csb = new MySqlConnectionStringBuilder
		{
			Server = "localhost",
			Port = 1000000,
		};
		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Throws<MySqlException>(() => connection.Open());
	}

	[Fact]
	public void ConnectBadDatabase()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Database = "wrong_database";
		using var connection = new MySqlConnection(csb.ConnectionString);
		var ex = Assert.Throws<MySqlException>(connection.Open);
#if !BASELINE // https://bugs.mysql.com/bug.php?id=78426
		if (AppConfig.SupportedFeatures.HasFlag(ServerFeatures.ErrorCodes) || ex.ErrorCode != default)
			Assert.Equal(MySqlErrorCode.UnknownDatabase, ex.ErrorCode);
#endif
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

	[Fact]
	public void ConnectBadPassword()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Password = "wrong";
		using var connection = new MySqlConnection(csb.ConnectionString);
		var ex = Assert.Throws<MySqlException>(connection.Open);
#if !BASELINE // https://bugs.mysql.com/bug.php?id=78426
		if (AppConfig.SupportedFeatures.HasFlag(ServerFeatures.ErrorCodes) || ex.ErrorCode != default)
			Assert.Equal(MySqlErrorCode.AccessDenied, ex.ErrorCode);
#endif
		Assert.Equal(ConnectionState.Closed, connection.State);
	}

#if !BASELINE
	[Theory]
	[InlineData("server=mysqld.sock;Protocol=Unix;LoadBalance=Failover")]
	[InlineData("server=pipename;Protocol=Pipe;LoadBalance=Failover")]
	public void LoadBalanceNotSupported(string connectionString)
	{
		using var connection = new MySqlConnection(connectionString);
		Assert.Throws<NotSupportedException>(() => connection.Open());
	}
#endif

	[Fact]
	public void NonExistentPipe()
	{
		var csb = new MySqlConnectionStringBuilder
		{
			PipeName = "nonexistingpipe",
			ConnectionProtocol = MySqlConnectionProtocol.NamedPipe,
			Server = ".",
			ConnectionTimeout = 1
		};

		var sw = Stopwatch.StartNew();
		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Throws<MySqlException>(connection.Open);
#if !BASELINE
		TestUtilities.AssertDuration(sw, 900, 500);
#else
		TestUtilities.AssertDuration(sw, 0, 500);
#endif
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	[InlineData(true, true)]
	public void PersistSecurityInfo(bool persistSecurityInfo, bool closeConnection)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.PersistSecurityInfo = persistSecurityInfo;
		var connectionStringWithoutPassword = Regex.Replace(csb.ConnectionString, @"(?i)password='?" + Regex.Escape(csb.Password) + "'?;?", "");

		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(csb.ConnectionString, connection.ConnectionString);
		connection.Open();
		if (persistSecurityInfo)
			Assert.Equal(csb.ConnectionString, connection.ConnectionString);
		else
			Assert.Equal(connectionStringWithoutPassword, connection.ConnectionString);

		if (closeConnection)
		{
			connection.Close();
			if (persistSecurityInfo)
				Assert.Equal(csb.ConnectionString, connection.ConnectionString);
			else
				Assert.Equal(connectionStringWithoutPassword, connection.ConnectionString);
		}
	}

	[Fact]
	public void State()
	{
		using var connection = new MySqlConnection(m_database.Connection.ConnectionString);
		Assert.Equal(ConnectionState.Closed, connection.State);
		connection.Open();
		Assert.Equal(ConnectionState.Open, connection.State);
		connection.Close();
		Assert.Equal(ConnectionState.Closed, connection.State);
		connection.Open();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[Fact]
	public void DataSource()
	{
		using (var connection = new MySqlConnection())
		{
			Assert.Equal("", connection.DataSource);
		}
		using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
		{
			Assert.NotNull(connection.DataSource);
		}
	}

	[SkippableFact(ConfigSettings.TcpConnection, Baseline = "https://bugs.mysql.com/bug.php?id=81650")]
	public void ConnectMultipleHostNames()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Server = "invalid.example.net," + csb.Server;

		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(ConnectionState.Closed, connection.State);
		connection.Open();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[SkippableFact(ConfigSettings.PasswordlessUser)]
	public void ConnectNoPassword()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.UserID = AppConfig.PasswordlessUser;
		csb.Password = "";
		csb.Database = "";

		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(ConnectionState.Closed, connection.State);
		connection.Open();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	[SkippableFact(ConfigSettings.PasswordlessUser)]
	public void ConnectionPoolNoPassword()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.UserID = AppConfig.PasswordlessUser;
		csb.Password = "";
		csb.Database = "";
		csb.Pooling = true;
		csb.MinimumPoolSize = 0;
		csb.MaximumPoolSize = 5;

		for (int i = 0; i < 3; i++)
		{
			using var connection = new MySqlConnection(csb.ConnectionString);
			Assert.Equal(ConnectionState.Closed, connection.State);
			connection.Open();
			Assert.Equal(ConnectionState.Open, connection.State);
		}
	}

	[SkippableFact(ServerFeatures.Timeout)]
	public void ConnectTimeout()
	{
		var csb = new MySqlConnectionStringBuilder
		{
			Server = "www.mysql.com",
			Pooling = false,
			ConnectionTimeout = 3,
		};

		using var connection = new MySqlConnection(csb.ConnectionString);
		var stopwatch = Stopwatch.StartNew();
		Assert.Throws<MySqlException>(() => connection.Open());
		stopwatch.Stop();
		TestUtilities.AssertDuration(stopwatch, 2900, 1500);
	}

	[Fact]
	public void ConnectionDatabase()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Equal(csb.Database, connection.Database);

		connection.Open();

		Assert.Equal(csb.Database, connection.Database);
		Assert.Equal(csb.Database, QueryCurrentDatabase(connection));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public void ChangeDatabase()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		Assert.Equal(csb.Database, connection.Database);
		Assert.Equal(csb.Database, QueryCurrentDatabase(connection));

		connection.ChangeDatabase(AppConfig.SecondaryDatabase);

		Assert.Equal(AppConfig.SecondaryDatabase, connection.Database);
		Assert.Equal(AppConfig.SecondaryDatabase, QueryCurrentDatabase(connection));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public void ChangeDatabaseNotOpen()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Throws<InvalidOperationException>(() => connection.ChangeDatabase(AppConfig.SecondaryDatabase));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public void ChangeDatabaseNull()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		Assert.Throws<ArgumentException>(() => connection.ChangeDatabase(null));
		Assert.Throws<ArgumentException>(() => connection.ChangeDatabase(""));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public void ChangeDatabaseInvalidName()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		Assert.Throws<MySqlException>(() => connection.ChangeDatabase($"not_a_real_database_1234"));

		Assert.Equal(ConnectionState.Open, connection.State);
		Assert.Equal(csb.Database, connection.Database);
		Assert.Equal(csb.Database, QueryCurrentDatabase(connection));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public void ChangeDatabaseConnectionPooling()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Pooling = true;
		csb.MinimumPoolSize = 0;
		csb.MaximumPoolSize = 6;

		for (int i = 0; i < csb.MaximumPoolSize * 2; i++)
		{
			using var connection = new MySqlConnection(csb.ConnectionString);
			connection.Open();

			Assert.Equal(csb.Database, connection.Database);
			Assert.Equal(csb.Database, QueryCurrentDatabase(connection));

			connection.ChangeDatabase(AppConfig.SecondaryDatabase);

			Assert.Equal(AppConfig.SecondaryDatabase, connection.Database);
			Assert.Equal(AppConfig.SecondaryDatabase, QueryCurrentDatabase(connection));
		}
	}

	[SkippableFact(ServerFeatures.SessionTrack, ConfigSettings.SecondaryDatabase, Baseline = "https://bugs.mysql.com/bug.php?id=89085")]
	public void UseDatabase()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();

		Assert.Equal(csb.Database, connection.Database);
		Assert.Equal(csb.Database, QueryCurrentDatabase(connection));

		using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = $"USE {AppConfig.SecondaryDatabase};";
			cmd.ExecuteNonQuery();
		}

		Assert.Equal(AppConfig.SecondaryDatabase, connection.Database);
		Assert.Equal(AppConfig.SecondaryDatabase, QueryCurrentDatabase(connection));
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public void ChangeDatabaseInTransaction()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();
		connection.Execute($@"drop table if exists changedb1;
create table changedb1(value int not null);
drop table if exists `{AppConfig.SecondaryDatabase}`.changedb2;
create table `{AppConfig.SecondaryDatabase}`.changedb2(value int not null);");

		using var transaction = connection.BeginTransaction();

#if !BASELINE
		Assert.Equal(transaction, connection.CurrentTransaction);
#endif
		using (var command = new MySqlCommand("SELECT 'abc';", connection, transaction))
			Assert.Equal("abc", command.ExecuteScalar());
		using (var command = new MySqlCommand("INSERT INTO changedb1(value) values(1),(2);", connection, transaction))
			command.ExecuteNonQuery();

		connection.ChangeDatabase(AppConfig.SecondaryDatabase);

#if !BASELINE
		Assert.Equal(transaction, connection.CurrentTransaction);
#endif

		using (var command = new MySqlCommand("SELECT 'abc';", connection, transaction))
			Assert.Equal("abc", command.ExecuteScalar());
		using (var command = new MySqlCommand("INSERT INTO changedb2(value) values(3),(4);", connection, transaction))
			command.ExecuteNonQuery();

		transaction.Commit();

		using var connection2 = new MySqlConnection(csb.ConnectionString);
		connection2.Open();
		var values = connection2.Query<int>($@"SELECT value FROM changedb1 UNION SELECT value FROM `{AppConfig.SecondaryDatabase}`.changedb2", connection2).OrderBy(x => x).ToList();
		Assert.Equal(new[] { 1, 2, 3, 4 }, values);
	}

	private static string QueryCurrentDatabase(MySqlConnection connection)
	{
		using var cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT DATABASE()";
		return (string) cmd.ExecuteScalar();
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public void ChangeConnectionStringWhenOpen()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();
		Assert.Equal(csb.Database, connection.Database);

		csb.Database = AppConfig.SecondaryDatabase;
#if BASELINE
		Assert.Throws<MySqlException>(() =>
#else
		Assert.Throws<InvalidOperationException>(() =>
#endif
		{
			connection.ConnectionString = csb.ConnectionString;
		});
	}

	[SkippableFact(ConfigSettings.SecondaryDatabase)]
	public void ChangeConnectionStringAfterClose()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();
		Assert.Equal(csb.Database, connection.Database);
		connection.Close();

		csb.Database = AppConfig.SecondaryDatabase;
		connection.ConnectionString = csb.ConnectionString;

		connection.Open();
		Assert.Equal(csb.Database, connection.Database);
		connection.Close();
	}

	[SkippableFact(ServerFeatures.Sha256Password, ConfigSettings.RequiresSsl)]
	public void Sha256WithSecureConnection()
	{
		var csb = AppConfig.CreateSha256ConnectionStringBuilder();
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();
	}

	[SkippableFact(ServerFeatures.Sha256Password)]
	public void Sha256WithoutSecureConnection()
	{
		var csb = AppConfig.CreateSha256ConnectionStringBuilder();
		csb.SslMode = MySqlSslMode.None;
		csb.AllowPublicKeyRetrieval = true;
		using var connection = new MySqlConnection(csb.ConnectionString);
#if NET45
		Assert.Throws<NotImplementedException>(() => connection.Open());
#else
		if (AppConfig.SupportedFeatures.HasFlag(ServerFeatures.RsaEncryption))
			connection.Open();
		else
			Assert.Throws<MySqlException>(() => connection.Open());
#endif
	}

	[Fact]
	public void PingNoConnection()
	{
		using var connection = new MySqlConnection();
		Assert.False(connection.Ping());
	}

	[Fact]
	public void PingBeforeConnecting()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		Assert.False(connection.Ping());
	}

	[Fact]
	public void PingConnection()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		Assert.True(connection.Ping());
	}

	[SkippableFact(ServerFeatures.UnixDomainSocket)]
	public void UnixDomainSocket()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.Server = AppConfig.SocketPath;
		csb.ConnectionProtocol = MySqlConnectionProtocol.Unix;
		using var connection = new MySqlConnection(csb.ConnectionString);
		connection.Open();
		Assert.Equal(ConnectionState.Open, connection.State);
	}

	readonly DatabaseFixture m_database;
}
