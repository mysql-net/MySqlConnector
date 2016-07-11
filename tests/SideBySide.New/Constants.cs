using MySql.Data.MySqlClient;

namespace SideBySide
{
	public static class Constants
	{
		public const string Server = "localhost";
		public const string UserName = "mysqltest";
		public const string Password = "test;key=\"val";

		public static MySqlConnectionStringBuilder CreateConnectionStringBuilder()
		{
			return new MySqlConnectionStringBuilder
			{
				Server = Server,
				UserID = UserName,
				Password = Password,
				ConvertZeroDateTime = true,
			};
		}
	}
}
