using System.Data.Common;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlClientFactory : DbProviderFactory
	{
		public static readonly MySqlClientFactory Instance = new MySqlClientFactory();

		public override DbCommand CreateCommand() => new MySqlCommand();
		public override DbConnection CreateConnection() => new MySqlConnection();
		public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new MySqlConnectionStringBuilder();
		public override DbParameter CreateParameter() => new MySqlParameter();

#if !NETSTANDARD1_3
		public override DbCommandBuilder CreateCommandBuilder() => new MySqlCommandBuilder();
		public override DbDataAdapter CreateDataAdapter() => new MySqlDataAdapter();
#endif

		private MySqlClientFactory()
		{
		}
	}
}
