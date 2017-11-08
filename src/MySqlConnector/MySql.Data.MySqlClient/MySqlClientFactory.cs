using System.Data.Common;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlClientFactory : DbProviderFactory
	{
		public static readonly MySqlClientFactory Instance = new MySqlClientFactory();

		private MySqlClientFactory()
		{
		}

		public override DbCommand CreateCommand()
			=> new MySqlCommand();

		public override DbConnection CreateConnection()
			=> new MySqlConnection();

		public override DbConnectionStringBuilder CreateConnectionStringBuilder()
			=> new MySqlConnectionStringBuilder();

		public override DbParameter CreateParameter()
			=> new MySqlParameter();
	}
}
