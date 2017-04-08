using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Xunit;

namespace SideBySide
{
	public class CancelTests : IClassFixture<DatabaseFixture>, IDisposable
	{
		public CancelTests(DatabaseFixture database)
		{
			m_database = database;
			m_database.Connection.Open();
		}

		public void Dispose()
		{
			m_database.Connection.Close();
		}

		[Fact]
		public void NoCancel()
		{
			using (var cmd = new MySqlCommand("SELECT SLEEP(0.25)", m_database.Connection))
			{
				var stopwatch = Stopwatch.StartNew();
				var result = (long) cmd.ExecuteScalar();
				Assert.Equal(0L, result);
				Assert.InRange(stopwatch.ElapsedMilliseconds, 100, 1000);
			}
		}

		[Fact]
		public void CancelCommand()
		{
			using (var cmd = new MySqlCommand("SELECT SLEEP(5)", m_database.Connection))
			{
				var task = Task.Run(async () =>
				{
					await Task.Delay(TimeSpan.FromSeconds(0.5));
					cmd.Cancel();
				});

				var stopwatch = Stopwatch.StartNew();
				var result = (long) cmd.ExecuteScalar();
				Assert.Equal(1L, result);
				Assert.InRange(stopwatch.ElapsedMilliseconds, 250, 2500);

				task.Wait(); // shouldn't throw
			}
		}

		[UnbufferedResultSetsFact]
		public void CancelReaderAsynchronously()
		{
			using (var cmd = new MySqlCommand(@"drop table if exists integers;
				create table integers(value int not null primary key);
				insert into integers(value) values (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20);", m_database.Connection))
			{
				cmd.ExecuteNonQuery();
			}

			using (var barrier = new Barrier(2))
			using (var cmd = new MySqlCommand("select * from integers a join integers b join integers c join integers d join integers e join integers f join integers g join integers h;", m_database.Connection))
			{
				var task = Task.Run(() =>
				{
					barrier.SignalAndWait();
					cmd.Cancel();
				});

				int rows = 0;
				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read());

					barrier.SignalAndWait();
					try
					{
						while (reader.Read())
							rows++;
					}
					catch (MySqlException ex)
					{
						Assert.Equal((int) MySqlErrorCode.QueryInterrupted, ex.Number);
					}

					// query returns 25 billion rows; we shouldn't have read many of them
					Assert.InRange(rows, 0, 10000000);
				}

				task.Wait(); // shouldn't throw
			}
		}

		[UnbufferedResultSetsFact]
		public void CancelCommandBeforeRead()
		{
			using (var cmd = new MySqlCommand(@"drop table if exists integers;
				create table integers(value int not null primary key);
				insert into integers(value) values (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20);", m_database.Connection))
			{
				cmd.ExecuteNonQuery();
			}

			using (var cmd = new MySqlCommand("select * from integers a join integers b join integers c join integers d join integers e join integers f join integers g join integers h;", m_database.Connection))
			{
				using (var reader = cmd.ExecuteReader())
				{
					cmd.Cancel();

					var stopwatch = Stopwatch.StartNew();
					int rows = 0;
					try
					{
						while (reader.Read())
							rows++;
					}
					catch (MySqlException ex)
					{
						Assert.Equal((int) MySqlErrorCode.QueryInterrupted, ex.Number);
					}
					Assert.False(reader.NextResult());
					Assert.InRange(stopwatch.ElapsedMilliseconds, 0, 1000);
					Assert.InRange(rows, 0, 10000000);
				}
			}
		}

		[UnbufferedResultSetsFact
#if BASELINE
			(Skip = "Hangs in NextResult")
#endif
		]
		public void CancelMultiStatementReader()
		{
			using (var cmd = new MySqlCommand(@"drop table if exists integers;
				create table integers(value int not null primary key);
				insert into integers(value) values (0),(1),(2),(3),(4),(5),(6),(7),(8),(9),(10),(11),(12),(13),(14),(15),(16),(17),(18),(19),(20);", m_database.Connection))
			{
				cmd.ExecuteNonQuery();
			}

			using (var barrier = new Barrier(2))
			using (var cmd = new MySqlCommand(@"select * from integers a join integers b join integers c join integers d join integers e join integers f join integers g join integers h;
				select * from integers a join integers b join integers c join integers d join integers e join integers f join integers g join integers h;
				select * from integers a join integers b join integers c join integers d join integers e join integers f join integers g join integers h;", m_database.Connection))
			{
				var task = Task.Run(() =>
				{
					barrier.SignalAndWait();
					cmd.Cancel();
				});

				int rows = 0;
				using (var reader = cmd.ExecuteReader())
				{
					Assert.True(reader.Read());

					barrier.SignalAndWait();
					try
					{
						while (reader.Read())
							rows++;
					}
					catch (MySqlException ex)
					{
						Assert.Equal((int) MySqlErrorCode.QueryInterrupted, ex.Number);
					}

					// query returns 25 billion rows; we shouldn't have read many of them
					Assert.InRange(rows, 0, 10000000);

					Assert.False(reader.NextResult());
				}

				task.Wait(); // shouldn't throw
			}

			// should be able to reuse the same connection for another query
			using (var cmd = new MySqlCommand(@"select sum(value) from integers;", m_database.Connection))
			using (var reader = cmd.ExecuteReader())
			{
				Assert.True(reader.Read());
				Assert.Equal(210, reader.GetInt32(0));
				Assert.False(reader.Read());
				Assert.False(reader.NextResult());
			}
		}

		readonly DatabaseFixture m_database;
	}
}
