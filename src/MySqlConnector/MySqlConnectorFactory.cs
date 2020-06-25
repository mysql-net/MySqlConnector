using System.Data.Common;

namespace MySqlConnector
{
	public sealed class MySqlConnectorFactory : DbProviderFactory
	{
		public static readonly MySqlConnectorFactory Instance = new();

		public override DbCommand CreateCommand() => new MySqlCommand();
		public override DbConnection CreateConnection() => new MySqlConnection();
		public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new MySqlConnectionStringBuilder();
		public override DbParameter CreateParameter() => new MySqlParameter();

#if !NETSTANDARD1_3
		public override DbCommandBuilder CreateCommandBuilder() => new MySqlCommandBuilder();
		public override DbDataAdapter CreateDataAdapter() => new MySqlDataAdapter();

#if !NET45 && !NET461 && !NET471 && !NETSTANDARD2_0 && !NETCOREAPP2_1
		public override bool CanCreateCommandBuilder => true;
		public override bool CanCreateDataAdapter => true;
		public override bool CanCreateDataSourceEnumerator => false;
#endif
#endif

		public MySqlBatch CreateBatch() => new MySqlBatch();
		public MySqlBatchCommand CreateBatchCommand() => new MySqlBatchCommand();
		public bool CanCreateBatch => true;

		private MySqlConnectorFactory()
		{
		}
	}
}
