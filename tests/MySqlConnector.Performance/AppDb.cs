using System;
using MySql.Data.MySqlClient;

namespace MySqlConnector.Performance
{
	public class AppDb : IDisposable
	{
		public static void Initialize()
		{
			using (var db = new AppDb())
			{
				db.Connection.Open();
				var cmd = db.Connection.CreateCommand();
				cmd.CommandText = @"
DROP TABLE IF EXISTS `BlogPost`;
CREATE TABLE IF NOT EXISTS `BlogPost` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Content` longtext,
  `Title` longtext,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB;
			";
				cmd.ExecuteNonQuery();
			}
		}

		public MySqlConnection Connection;

		public AppDb()
		{
			Connection = new MySqlConnection(AppConfig.Config["Data:ConnectionString"]);
		}

		public void Dispose()
		{
			Connection.Close();
		}
	}
}
