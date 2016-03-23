using System;
using MySql.Data.MySqlClient;

namespace SideBySide
{
	public class DatabaseFixture : IDisposable
	{
		public DatabaseFixture()
		{
			Connection = new MySqlConnection(CreateConnectionStringBuilder().ConnectionString);
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

		private static MySqlConnectionStringBuilder CreateConnectionStringBuilder()
		{
			return new MySqlConnectionStringBuilder
			{
				Server = Constants.Server,
				UserID = Constants.UserName,
				Password = Constants.Password,
			};
		}

	}
}
