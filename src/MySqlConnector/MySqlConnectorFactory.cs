using System.Data.Common;

namespace MySqlConnector
{
	/// <summary>
	/// An implementation of <see cref="DbProviderFactory"/> that creates MySqlConnector objects.
	/// </summary>
	public sealed class MySqlConnectorFactory : DbProviderFactory
	{
		/// <summary>
		/// Provides an instance of <see cref="DbProviderFactory"/> that can create MySqlConnector objects.
		/// </summary>
		public static readonly MySqlConnectorFactory Instance = new();

		/// <summary>
		/// Creates a new <see cref="MySqlCommand"/> object.
		/// </summary>
		public override DbCommand CreateCommand() => new MySqlCommand();

		/// <summary>
		/// Creates a new <see cref="MySqlConnection"/> object.
		/// </summary>
		public override DbConnection CreateConnection() => new MySqlConnection();

		/// <summary>
		/// Creates a new <see cref="MySqlConnectionStringBuilder"/> object.
		/// </summary>
		public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new MySqlConnectionStringBuilder();

		/// <summary>
		/// Creates a new <see cref="MySqlParameter"/> object.
		/// </summary>
		/// <returns></returns>
		public override DbParameter CreateParameter() => new MySqlParameter();

#if !NETSTANDARD1_3
		/// <summary>
		/// Creates a new <see cref="MySqlCommandBuilder"/> object.
		/// </summary>
		public override DbCommandBuilder CreateCommandBuilder() => new MySqlCommandBuilder();

		/// <summary>
		/// Creates a new <see cref="MySqlDataAdapter"/> object.
		/// </summary>
		public override DbDataAdapter CreateDataAdapter() => new MySqlDataAdapter();

		/// <summary>
		/// Returns <c>false</c>.
		/// </summary>
		/// <remarks><see cref="DbDataSourceEnumerator"/> is not supported by MySqlConnector.</remarks>
		public override bool CanCreateDataSourceEnumerator => false;

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1
		/// <summary>
		/// Returns <c>true</c>.
		/// </summary>
		public override bool CanCreateCommandBuilder => true;

		/// <summary>
		/// Returns <c>true</c>.
		/// </summary>
		public override bool CanCreateDataAdapter => true;
#endif
#endif

#pragma warning disable CA1822 // Mark members as static
		/// <summary>
		/// Creates a new <see cref="MySqlBatch"/> object.
		/// </summary>
		public MySqlBatch CreateBatch() => new MySqlBatch();

		/// <summary>
		/// Creates a new <see cref="MySqlBatchCommand"/> object.
		/// </summary>
		public MySqlBatchCommand CreateBatchCommand() => new MySqlBatchCommand();

		/// <summary>
		/// Returns <c>true</c>.
		/// </summary>
		public bool CanCreateBatch => true;
#pragma warning restore CA1822 // Mark members as static

		private MySqlConnectorFactory()
		{
		}
	}
}
