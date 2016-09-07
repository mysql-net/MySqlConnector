using System;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class ConnectionPool : IDisposable
	{
		[Theory]
		[InlineData(false, 11, 0L)]
		[InlineData(true, 12, 1L)]
#if BASELINE
		// baseline default behaviour is to not reset the connection, which trades correctness for speed
		// see bug report at http://bugs.mysql.com/bug.php?id=77421
		[InlineData(null, 13, 0L)]
#else
		[InlineData(null, 13, 1L)]
#endif
		public void ResetConnection(object connectionReset, int poolSize, long expected)
		{
			var csb = Constants.CreateConnectionStringBuilder();
			csb.Pooling = true;
			csb.MaximumPoolSize = (uint) poolSize; // use a different pool size to create a unique connection string to force a unique pool to be created

#if BASELINE
			if (true.Equals(connectionReset))
			{
				using (var connection = new MySqlConnection(csb.ConnectionString))
				{
					connection.Open();
					using (var command = connection.CreateCommand())
					{
						command.CommandText = "create schema if not exists test;";
						command.ExecuteNonQuery();
					}
				}

				// baseline connector needs to have a database specified in the connection string to reauth properly (which is how connection reset is implemented prior to MySQL 5.7.3)
				csb.Database = "test";
			}
#endif

			if (connectionReset != null)
				csb.ConnectionReset = (bool) connectionReset;

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select @@autocommit;";
					Assert.Equal(1L, command.ExecuteScalar());
				}
			}

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "SET autocommit=0;";
					command.ExecuteNonQuery();
				}
			}

			using (var connection = new MySqlConnection(csb.ConnectionString))
			{
				connection.Open();
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "select @@autocommit;";
					Assert.Equal(expected, command.ExecuteScalar());
				}
			}
		}

		public void Dispose()
		{
			MySqlConnection.ClearAllPools();
		}
	}
}
