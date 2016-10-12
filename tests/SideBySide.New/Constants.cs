using MySql.Data.MySqlClient;

namespace SideBySide
{
	public static class Constants
	{
		public const string Server = "localhost";
		public const string UserName = "mysqltest";
		public const string Password = "test;key=\"val";
		public const uint Port = 3306;
		public const string Database = "mysqltest";

		public static MySqlConnectionStringBuilder CreateConnectionStringBuilder()
		{
			return new MySqlConnectionStringBuilder
			{
				Server = Server,
				Port = Port,
				UserID = UserName,
				Password = Password,
				ConvertZeroDateTime = true,
				UseAffectedRows = true,
				Database = Database,
			};
		}
	}
}
