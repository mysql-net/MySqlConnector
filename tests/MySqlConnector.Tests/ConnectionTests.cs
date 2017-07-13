using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;

namespace MySqlConnector.Tests
{
    public class ConnectionTests : IDisposable
	{
		public ConnectionTests()
		{
			m_server = new FakeMySqlServer();
			m_server.Start();

			m_csb = new MySqlConnectionStringBuilder
			{
				Server = "localhost",
				Port = (uint) m_server.Port,
			};
		}

		public void Dispose()
		{
			m_server.Stop();
		}

		[Fact]
		public void PooledConnectionIsReturnedToPool()
		{
			Assert.Equal(0, m_server.ActiveConnections);

			m_csb.Pooling = true;
			using (var connection = new MySqlConnection(m_csb.ConnectionString))
			{
				connection.Open();
				Assert.Equal(1, m_server.ActiveConnections);

				Assert.Equal(m_server.ServerVersion, connection.ServerVersion);
				connection.Close();
				Assert.Equal(1, m_server.ActiveConnections);
			}

			Assert.Equal(1, m_server.ActiveConnections);
		}

		[Fact]
		public async Task UnpooledConnectionIsClosed()
		{
			Assert.Equal(0, m_server.ActiveConnections);

			m_csb.Pooling = false;
			using (var connection = new MySqlConnection(m_csb.ConnectionString))
			{
				await connection.OpenAsync();
				Assert.Equal(1, m_server.ActiveConnections);

				Assert.Equal(m_server.ServerVersion, connection.ServerVersion);
				connection.Close();

				await WaitForConditionAsync(0, () => m_server.ActiveConnections);
			}
		}

		private static async Task WaitForConditionAsync<T>(T expected, Func<T> getValue)
		{
			var sw = Stopwatch.StartNew();
			while (sw.ElapsedMilliseconds < 1000 && !expected.Equals(getValue()))
				await Task.Delay(50);
			Assert.Equal(expected, getValue());
		}

		readonly FakeMySqlServer m_server;
		readonly MySqlConnectionStringBuilder m_csb;
	}
}
