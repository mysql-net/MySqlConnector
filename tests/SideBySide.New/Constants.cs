using MySql.Data.MySqlClient;

namespace SideBySide
{
	public static class Constants
	{
		public const string Server = "localhost";
		public const string UserName = "mysqltest";
		public const string Password = "mysqltest";
		public const string ServerVersion = "5.7.11-log";

		public static MySqlConnectionStringBuilder CreateConnectionStringBuilder()
		{
			return new MySqlConnectionStringBuilder
			{
				Server = Server,
				UserID = UserName,
				Password = Password,
			};
		}
	}
}
