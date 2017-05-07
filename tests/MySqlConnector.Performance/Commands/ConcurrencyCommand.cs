using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Performance.Models;

namespace MySqlConnector.Performance.Commands
{
	public static class ConcurrencyCommand
	{
		public static void Run(int iterations, int concurrency, int ops)
		{

			var recordNum = 0;
			async Task InsertOne(AppDb db)
			{
				var blog = new BlogPost(db)
				{
					Title = "Title " + Interlocked.Increment(ref recordNum),
					Content = "content"
				};
				await blog.InsertAsync();
			}

			var selected = new ConcurrentQueue<string>();
			async Task SelectTen(AppDb db)
			{
				var blogPosts = await (new BlogPostQuery(db)).LatestPostsAsync();
				selected.Enqueue(blogPosts.FirstOrDefault().Title);
			}

			var sleepNum = 0;
			async Task SleepMillisecond(AppDb db)
			{
				using (var cmd = db.Connection.CreateCommand())
				{
					cmd.CommandText = "SELECT SLEEP(0.001)";
					await cmd.ExecuteNonQueryAsync();
				}
				Interlocked.Increment(ref sleepNum);
			}

			using (var db = new AppDb())
			{
				db.Connection.Open();
				using (var cmd = db.Connection.CreateCommand())
				{
					cmd.CommandText = "DELETE FROM `BlogPost`";
					cmd.ExecuteNonQuery();
				}
			}

			PerfTest(InsertOne, "Insert One", iterations, concurrency, ops).GetAwaiter().GetResult();
			using (var db = new AppDb())
			{
				db.Connection.Open();
				using (var cmd = db.Connection.CreateCommand())
				{
					cmd.CommandText = "SELECT COUNT(*) FROM `BlogPost`";
					Console.WriteLine("Records Inserted: " + cmd.ExecuteScalar());
					Console.WriteLine();
				}
			}

			PerfTest(SelectTen, "Select Ten", iterations, concurrency, ops).GetAwaiter().GetResult();
			Console.WriteLine("Records Selected: " + selected.Count * 10);
			string firstRecord;
			if (selected.TryDequeue(out firstRecord))
				Console.WriteLine("First Record: " + firstRecord);
			Console.WriteLine();

			PerfTest(SleepMillisecond, "Sleep 1ms", iterations, concurrency, ops).GetAwaiter().GetResult();
			Console.WriteLine("Total Sleep Commands: " + sleepNum);
			Console.WriteLine();
		}

		public static async Task PerfTest(Func<AppDb, Task> test, string testName, int iterations, int concurrency, int ops)
		{
			var timers = new List<TimeSpan>();
			for (var iteration = 0; iteration < iterations; iteration++)
			{
				var tasks = new List<Task>();
				var start = DateTime.UtcNow;
				for (var connection = 0; connection < concurrency; connection++)
				{
					tasks.Add(ConnectionTask(test, ops));
				}
				await Task.WhenAll(tasks);
				timers.Add(DateTime.UtcNow - start);
			}
			Console.WriteLine("Test                     " + testName);
			Console.WriteLine("Iterations:              " + iterations);
			Console.WriteLine("Concurrency:             " + concurrency);
			Console.WriteLine("Operations:              " + ops);
			Console.WriteLine("Times (Min, Average, Max) "
							  + timers.Min() + ", "
							  + TimeSpan.FromTicks(timers.Sum(timer => timer.Ticks) / timers.Count) + ", "
							  + timers.Max());
			Console.WriteLine();
		}

		private static async Task ConnectionTask(Func<AppDb, Task> cb, int ops)
		{
			using (var db = new AppDb())
			{
				await db.Connection.OpenAsync();
				for (var op = 0; op < ops; op++)
				{
					await cb(db);
				}
			}
		}

	}
}
