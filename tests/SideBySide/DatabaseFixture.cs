using System;
#if NETCOREAPP1_1_2
using System.Reflection;
#endif
using System.Threading;
using MySql.Data.MySqlClient;

namespace SideBySide
{
	public class DatabaseFixture : IDisposable
	{
		public DatabaseFixture()
		{
			lock (s_lock)
			{
				if (!s_isInitialized)
				{
					// increase the number of worker threads to reduce number of spurious failures from threadpool starvation
#if NETCOREAPP1_1_2
					// from https://stackoverflow.com/a/42982698
					typeof(ThreadPool).GetMethod("SetMinThreads", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { 64, 64 });
#else
					ThreadPool.SetMinThreads(64, 64);
#endif

					var csb = AppConfig.CreateConnectionStringBuilder();
					var database = csb.Database;
					csb.Database = "";
					using (var db = new MySqlConnection(csb.ConnectionString))
					{
						db.Open();
						using (var cmd = db.CreateCommand())
						{
							cmd.CommandText = $"create schema if not exists {database};";
							cmd.ExecuteNonQuery();

							if (!string.IsNullOrEmpty(AppConfig.SecondaryDatabase))
							{
								cmd.CommandText = $"create schema if not exists {AppConfig.SecondaryDatabase};";
								cmd.ExecuteNonQuery();
							}
						}
						db.Close();
					}

					s_isInitialized = true;
				}
			}

			Connection = new MySqlConnection(AppConfig.ConnectionString);
		}

		public MySqlConnection Connection { get; }

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				Connection.Dispose();
			}
		}

		static object s_lock = new object();
		static bool s_isInitialized;
	}
}
