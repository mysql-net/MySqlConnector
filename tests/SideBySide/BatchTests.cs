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
		public void CreateBatchDoesNotSetTransaction()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using (connection.BeginTransaction())
				using (var batch = connection.CreateBatch())
				{
					Assert.Null(batch.Transaction);
				}
			}
		}

		[Fact]
		public void BatchTransactionMustBeSet()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using (var transaction = connection.BeginTransaction())
				using (var batch = connection.CreateBatch())
				{
					batch.BatchCommands.Add(new MySqlBatchCommand("SELECT 1;"));
					Assert.Throws<InvalidOperationException>(() => batch.ExecuteScalar());

					batch.Transaction = transaction;
					TestUtilities.AssertIsOne(batch.ExecuteScalar());
				}
			}
		}

		[Fact]
		public void IgnoreBatchTransactionIgnoresNull()
		{
			using (var connection = new MySqlConnection(GetIgnoreCommandTransactionConnectionString()))
			{
				connection.Open();
				using (connection.BeginTransaction())
				using (var batch = connection.CreateBatch())
				{
					batch.BatchCommands.Add(new MySqlBatchCommand("SELECT 1;"));
					TestUtilities.AssertIsOne(batch.ExecuteScalar());
				}
			}
		}

		[Fact]
		public void IgnoreCommandTransactionIgnoresDisposedTransaction()
		{
			using (var connection = new MySqlConnection(GetIgnoreCommandTransactionConnectionString()))
			{
				connection.Open();

				var transaction = connection.BeginTransaction();
				transaction.Commit();
				transaction.Dispose();

				using (var batch = connection.CreateBatch())
				{
					batch.BatchCommands.Add(new MySqlBatchCommand("SELECT 1;"));
					batch.Transaction = transaction;
					TestUtilities.AssertIsOne(batch.ExecuteScalar());
				}
			}
		}

		[Fact]
		public void IgnoreCommandTransactionIgnoresDifferentTransaction()
		{
			using (var connection1 = new MySqlConnection(AppConfig.ConnectionString))
			using (var connection2 = new MySqlConnection(GetIgnoreCommandTransactionConnectionString()))
			{
				connection1.Open();
				connection2.Open();
				using (var transaction1 = connection1.BeginTransaction())
				using (var batch2 = connection2.CreateBatch())
				{
					batch2.Transaction = transaction1;
					batch2.BatchCommands.Add(new MySqlBatchCommand("SELECT 1;"));
					TestUtilities.AssertIsOne(batch2.ExecuteScalar());
				}
			}
		}

		[Theory]
		[InlineData("")]
		[InlineData("\n")]
		[InlineData(";")]
		[InlineData(";\n")]
		[InlineData("; -- ")]
		[InlineData(" -- ")]
		[InlineData(" # ")]
		public void ExecuteBatch(string suffix)
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using (var batch = new MySqlBatch(connection)
				{
					BatchCommands =
					{
						new MySqlBatchCommand("SELECT 1" + suffix),
						new MySqlBatchCommand("SELECT 2" + suffix),
						new MySqlBatchCommand("SELECT 3" + suffix),
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

		[Fact(Skip = "COM_MULTI")]
		public void ExecuteInvalidSqlBatch()
		{
			using (var connection = new MySqlConnection(AppConfig.ConnectionString))
			{
				connection.Open();
				using (var batch = new MySqlBatch(connection)
				{
					BatchCommands =
					{
						new MySqlBatchCommand("SELECT 1;"),
						new MySqlBatchCommand("SELECT 2 /* incomplete"),
						new MySqlBatchCommand("SELECT 3;"),
					},
				})
				using (var reader = batch.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.Equal(1, reader.GetInt32(0));
					Assert.False(reader.Read());

					try
					{
						reader.NextResult();
						Assert.True(false, "Shouldn't get here");
					}
					catch (MySqlException ex)
					{
						Assert.Equal(MySqlErrorCode.ParseError, (MySqlErrorCode) ex.Number);
					}
				}
			}
		}

		private static string GetIgnoreCommandTransactionConnectionString() =>
			new MySqlConnectionStringBuilder(AppConfig.ConnectionString)
			{
				IgnoreCommandTransaction = true
			}.ConnectionString;
	}
}
#endif
