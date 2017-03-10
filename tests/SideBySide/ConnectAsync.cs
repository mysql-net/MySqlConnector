using System.Data;
using System.IO;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
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
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				await Assert.ThrowsAsync<MySqlException>(() => connection.OpenAsync());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public async Task ConnectBadPort()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = "localhost",
				Port = 65000,
			};
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				await Assert.ThrowsAsync<MySqlException>(() => connection.OpenAsync());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public async Task ConnectBadPassword()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.Password = "wrong";
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				await Assert.ThrowsAsync<MySqlException>(() => connection.OpenAsync());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public async Task State()
		{
			using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				await connection.OpenAsync();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

#if BASELINE
		[Fact(Skip = "https://bugs.mysql.com/bug.php?id=81650")]
#else
		[TcpConnectionFact]
#endif
		public async Task ConnectMultipleHostNames()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.Server = "invalid.example.net," + csb.Server;

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				await connection.OpenAsync();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

		[PasswordlessUserFact]
		public async Task ConnectNoPassword()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.UserID = AppConfig.PasswordlessUser;
			csb.Password = "";
			csb.Database = "";

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				await connection.OpenAsync().ConfigureAwait(false);
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

		[Fact]
		public async Task ConnectKeepAlive()
		{
			// the goal of this test is to ensure that no exceptions are thrown
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.Keepalive = 1;
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				await connection.OpenAsync();
				await Task.Delay(3000);
			}
		}

		[SslRequiredConnectionFact]
		public async Task ConnectSslPreferred()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			string requiredSslVersion;
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				using (var cmd = connection.CreateCommand())
				{
					await connection.OpenAsync();
					cmd.CommandText = "SHOW SESSION STATUS LIKE 'Ssl_version'";
					requiredSslVersion = (string)await cmd.ExecuteScalarAsync();
				}
			}
			Assert.False(string.IsNullOrWhiteSpace(requiredSslVersion));

			csb.SslMode = MySqlSslMode.Preferred;
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				using (var cmd = connection.CreateCommand())
				{
					await connection.OpenAsync();
					cmd.CommandText = "SHOW SESSION STATUS LIKE 'Ssl_version'";
					var preferredSslVersion = (string)await cmd.ExecuteScalarAsync();
					Assert.Equal(requiredSslVersion, preferredSslVersion);
				}
			}
		}

		[SslRequiredConnectionFact]
		public async Task ConnectSslClientCertificate()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.CertificateFile = Path.Combine(AppConfig.CertsPath, "ssl-client.pfx");
			csb.CertificatePassword = "";
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				using (var cmd = connection.CreateCommand())
				{
					await connection.OpenAsync();
					cmd.CommandText = "SHOW SESSION STATUS LIKE 'Ssl_version'";
					var sslVersion = (string)await cmd.ExecuteScalarAsync();
					Assert.False(string.IsNullOrWhiteSpace(sslVersion));
				}
			}
		}

		[SslRequiredConnectionFact]
		public async Task ConnectSslBadClientCertificate()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.CertificateFile = Path.Combine(AppConfig.CertsPath, "non-ca-client.pfx");
			csb.CertificatePassword = "";
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
#if BASELINE
				var exType = typeof(IOException);
#else
				var exType = typeof(MySqlException);
#endif
				await Assert.ThrowsAsync(exType, async () => await connection.OpenAsync());
			}
		}

		readonly DatabaseFixture m_database;
	}
}
