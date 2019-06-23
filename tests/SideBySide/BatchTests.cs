#if !BASELINE
using System;
using System.Data;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class BatchTests : IClassFixture<DatabaseFixture>
	{
		public BatchTests(DatabaseFixture database)
		{
		}

		[Fact]
		public void NeedsConnection()
		{
			using (var batch = new MySqlBatch
			{
				BatchCommands =
				{
					new MySqlBatchCommand("SELECT 1;"),
				},
			})
			{
				Assert.Throws<InvalidOperationException>(() => batch.ExecuteNonQuery());
			}
		}

		[Fact]
		public void NeedsOpenConnection()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			using (var batch = new MySqlBatch(connection)
			{
				BatchCommands =
				{
					new MySqlBatchCommand("SELECT 1;"),
				},
			})
			{
				Assert.Throws<InvalidOperationException>(() => batch.ExecuteNonQuery());
			}
		}

		[Fact]
		public void NeedsCommands()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using (var batch = new MySqlBatch(connection))
				{
					Assert.Throws<InvalidOperationException>(() => batch.ExecuteNonQuery());
				}
			}
		}

		[Fact]
		public void NotDisposed()
		{
			using (var batch = new MySqlBatch())
			{
				batch.Dispose();
				Assert.Throws<ObjectDisposedException>(() => batch.ExecuteNonQuery());
			}
		}


		[Fact]
		public void NoCloseConnection()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using (var batch = new MySqlBatch(connection)
				{
					BatchCommands =
					{
						new MySqlBatchCommand("SELECT 1;") { CommandBehavior = CommandBehavior.CloseConnection },
					},
				})
				{
					Assert.Throws<NotSupportedException>(() => batch.ExecuteNonQuery());
				}
			}
		}

		[Fact]
		public void ExecuteBatch()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using (var batch = new MySqlBatch(connection)
				{
					BatchCommands =
					{
						new MySqlBatchCommand("SELECT 1;"),
						new MySqlBatchCommand("SELECT 2;"),
						new MySqlBatchCommand("SELECT 3;"),
					},
				})
				using (var reader = batch.ExecuteReader())
				{
					var total = 0;

					Assert.True(reader.Read());
					total += reader.GetInt32(0);
					Assert.False(reader.Read());
					Assert.True(reader.NextResult());

					Assert.True(reader.Read());
					total += reader.GetInt32(0);
					Assert.False(reader.Read());
					Assert.True(reader.NextResult());

					Assert.True(reader.Read());
					total += reader.GetInt32(0);
					Assert.False(reader.Read());
					Assert.False(reader.NextResult());

					Assert.Equal(6, total);
				}
			}
		}
	}
}
#endif
