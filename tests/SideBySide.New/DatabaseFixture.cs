using System;
using MySql.Data.MySqlClient;

namespace SideBySide
{
	public class DatabaseFixture : IDisposable
	{
		public DatabaseFixture()
		{
			Connection = new MySqlConnection(Constants.CreateConnectionStringBuilder().ConnectionString);
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
#if !BASELINE
				MySqlHelper.ClearConnectionPools();
#endif
			}
		}
	}
}
