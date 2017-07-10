using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
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
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				Assert.Throws<MySqlException>(() => connection.Open());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public void ConnectBadPort()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = "localhost",
				Port = 65000,
			};
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				Assert.Throws<MySqlException>(() => connection.Open());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public void ConnectBadDatabase()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.Database = "wrong_database";
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				try
				{
					connection.Open();
					Assert.True(false);
				}
				catch (MySqlException ex)
				{
#if BASELINE
					// https://bugs.mysql.com/bug.php?id=78426
					Assert.NotNull(ex);
#else
					Assert.Equal((int) MySqlErrorCode.UnknownDatabase, ex.Number);
#endif
				}
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Fact]
		public void ConnectBadPassword()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.Password = "wrong";
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				try
				{
					connection.Open();
					Assert.True(false);
				}
				catch (MySqlException ex)
				{
#if BASELINE
					// https://bugs.mysql.com/bug.php?id=73610
					Assert.NotNull(ex);
#else
					Assert.Equal((int) MySqlErrorCode.AccessDenied, ex.Number);
#endif
				}
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void PersistSecurityInfo(bool persistSecurityInfo)
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.PersistSecurityInfo = persistSecurityInfo;
			var connectionStringWithoutPassword = Regex.Replace(csb.ConnectionString, @"(?i)password='?" + Regex.Escape(csb.Password) + "'?;?", "");

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(csb.ConnectionString, connection.ConnectionString);
				connection.Open();
				if (persistSecurityInfo)
					Assert.Equal(csb.ConnectionString, connection.ConnectionString);
				else
					Assert.Equal(connectionStringWithoutPassword, connection.ConnectionString);
			}
		}

		[Fact]
		public void State()
		{
			using (var connection = new MySqlConnection(m_database.Connection.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				connection.Open();
				Assert.Equal(ConnectionState.Open, connection.State);
				connection.Close();
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
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

#if BASELINE
		[Fact(Skip = "https://bugs.mysql.com/bug.php?id=81650")]
#else
		[TcpConnectionFact]
#endif
		public void ConnectMultipleHostNames()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.Server = "invalid.example.net," + csb.Server;

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				connection.Open();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

		[PasswordlessUserFact]
		public void ConnectNoPassword()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.UserID = AppConfig.PasswordlessUser;
			csb.Password = "";
			csb.Database = "";

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				connection.Open();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

		[PasswordlessUserFact]
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
				using (var connection = new MySqlConnection(csb.ConnectionString))
				{
					Assert.Equal(ConnectionState.Closed, connection.State);
					connection.Open();
					Assert.Equal(ConnectionState.Open, connection.State);
				}
			}
		}

		[Fact]
		public void ConnectTimeout()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = "www.mysql.com",
				Pooling = false,
				ConnectionTimeout = 3,
			};

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				var stopwatch = Stopwatch.StartNew();
				Assert.Throws<MySqlException>(() => connection.Open());
				stopwatch.Stop();
				TestUtilities.AssertDuration(stopwatch, 2900, 200);
			}
		}

		[Fact]
		public void ConnectionDatabase()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(csb.Database, connection.Database);

				connection.Open();

				Assert.Equal(csb.Database, connection.Database);
				Assert.Equal(csb.Database, QueryCurrentDatabase(connection));
			}
		}

		[SecondaryDatabaseRequiredFact]
		public void ChangeDatabase()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();

				Assert.Equal(csb.Database, connection.Database);
				Assert.Equal(csb.Database, QueryCurrentDatabase(connection));

				connection.ChangeDatabase(AppConfig.SecondaryDatabase);

				Assert.Equal(AppConfig.SecondaryDatabase, connection.Database);
				Assert.Equal(AppConfig.SecondaryDatabase, QueryCurrentDatabase(connection));
			}
		}

		[SecondaryDatabaseRequiredFact]
		public void ChangeDatabaseNotOpen()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Throws<InvalidOperationException>(() => connection.ChangeDatabase(AppConfig.SecondaryDatabase));
			}
		}

		[SecondaryDatabaseRequiredFact]
		public void ChangeDatabaseNull()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Throws<ArgumentException>(() => connection.ChangeDatabase(null));
				Assert.Throws<ArgumentException>(() => connection.ChangeDatabase(""));
			}
		}

		[SecondaryDatabaseRequiredFact]
		public void ChangeDatabaseInvalidName()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();

				Assert.Throws<MySqlException>(() => connection.ChangeDatabase($"not_a_real_database_1234"));

				Assert.Equal(ConnectionState.Open, connection.State);
				Assert.Equal(csb.Database, connection.Database);
				Assert.Equal(csb.Database, QueryCurrentDatabase(connection));
			}
		}

		[SecondaryDatabaseRequiredFact]
		public void ChangeDatabaseConnectionPooling()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			csb.Pooling = true;
			csb.MinimumPoolSize = 0;
			csb.MaximumPoolSize = 6;

			for (int i = 0; i < csb.MaximumPoolSize * 2; i++)
			{
				using (var connection = new MySqlConnection(csb.ConnectionString))
				{
					connection.Open();

					Assert.Equal(csb.Database, connection.Database);
					Assert.Equal(csb.Database, QueryCurrentDatabase(connection));

					connection.ChangeDatabase(AppConfig.SecondaryDatabase);

					Assert.Equal(AppConfig.SecondaryDatabase, connection.Database);
					Assert.Equal(AppConfig.SecondaryDatabase, QueryCurrentDatabase(connection));
				}
			}
		}

		private static string QueryCurrentDatabase(MySqlConnection connection)
		{
			using (var cmd = connection.CreateCommand())
			{
				cmd.CommandText = "SELECT DATABASE()";
				return (string) cmd.ExecuteScalar();
			}
		}

		[RequiresFeatureFact(ServerFeatures.Sha256Password, RequiresSsl = true)]
		public void Sha256WithSecureConnection()
		{
			var csb = AppConfig.CreateSha256ConnectionStringBuilder();
			using (var connection = new MySqlConnection(csb.ConnectionString))
				connection.Open();
		}

		[RequiresFeatureFact(ServerFeatures.Sha256Password)]
		public void Sha256WithoutSecureConnection()
		{
			var csb = AppConfig.CreateSha256ConnectionStringBuilder();
			csb.SslMode = MySqlSslMode.None;
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
#if BASELINE || NET45
				Assert.Throws<NotImplementedException>(() => connection.Open());
#else
				if (AppConfig.SupportedFeatures.HasFlag(ServerFeatures.OpenSsl))
					connection.Open();
				else
					Assert.Throws<MySqlException>(() => connection.Open());
#endif
			}
		}

		readonly DatabaseFixture m_database;
	}
}
