using System;
using MySql.Data.MySqlClient;

namespace SideBySide
{
	public class DatabaseFixture : IDisposable
	{
		public DatabaseFixture()
		{
			var csb = AppConfig.CreateConnectionStringBuilder();
			var connectionString = csb.ConnectionString;
			var database = csb.Database;
			csb.Database = "";
			using (var db = new MySqlConnection(csb.ConnectionString))
			{
				db.Open();
				var cmd = db.CreateCommand();
				cmd.CommandText = "create schema if not exists " + database;
				cmd.ExecuteNonQuery();
				db.Close();
			}

			Connection = new MySqlConnection(connectionString);
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
	}
}
