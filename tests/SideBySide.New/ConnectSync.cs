using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
		public void ConnectBadPassword()
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.Password = "wrong";
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Throws<MySqlException>(() => connection.Open());
				Assert.Equal(ConnectionState.Closed, connection.State);
			}
		}

		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void PersistSecurityInfo(bool persistSecurityInfo)
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.PersistSecurityInfo = persistSecurityInfo;
			var connectionStringWithoutPassword = csb.ConnectionString.Replace("Password", "password").Replace(";password='" + Constants.Password + "'", "");

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

#if BASELINE
		[Fact(Skip = "https://bugs.mysql.com/bug.php?id=81650")]
#else
		[Fact]
#endif
		public void ConnectMultipleHostNames()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = "invalid.example.net,localhost",
				Port = 3306,
				UserID = Constants.UserName,
				Password = Constants.Password,
			};
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				connection.Open();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

		[Fact]
		public void ConnectNoPassword()
		{
			var csb = new MySqlConnectionStringBuilder
			{
				Server = Constants.Server,
				UserID = "no_password",
			};
			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				Assert.Equal(ConnectionState.Closed, connection.State);
				connection.Open();
				Assert.Equal(ConnectionState.Open, connection.State);
			}
		}

		[Fact(Skip = "Not yet implemented")]
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
				Assert.InRange(stopwatch.ElapsedMilliseconds, 2800, 3500);
			}
		}

		readonly DatabaseFixture m_database;
	}
}
