using System;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace MySqlConnector.Performance
{
    public class AppDb : IDisposable
    {
        public static void Initialize()
        {
            using (var db = new AppDb())
            {
                db.Open();
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

        public void Open()
        {
            Connection.Open();
        }

        public async Task OpenAsync()
        {
            await Connection.OpenAsync();
        }

        public void Dispose()
        {
            Connection.Close();
        }
    }
}
