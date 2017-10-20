using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient.Caches;
using MySql.Data.Protocol.Serialization;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlConnection : DbConnection
	{
		public MySqlConnection()
			: this("")
		{
		}

		public MySqlConnection(string connectionString) => ConnectionString = connectionString;

		public new MySqlTransaction BeginTransaction() => (MySqlTransaction) base.BeginTransaction();

		public new MySqlTransaction BeginTransaction(IsolationLevel isolationLevel) => (MySqlTransaction) base.BeginTransaction(isolationLevel);

		public Task<MySqlTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default(CancellationToken)) =>
			BeginDbTransactionAsync(IsolationLevel.Unspecified, AsyncIOBehavior, cancellationToken);

		public Task<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken)) =>
			BeginDbTransactionAsync(isolationLevel, AsyncIOBehavior, cancellationToken);

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
			BeginDbTransactionAsync(isolationLevel, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		private async Task<MySqlTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (State != ConnectionState.Open)
				throw new InvalidOperationException("Connection is not open.");
			if (CurrentTransaction != null)
				throw new InvalidOperationException("Transactions may not be nested.");
#if !NETSTANDARD1_3
			if (m_xaTransaction != null)
				throw new InvalidOperationException("Cannot begin a transaction when already enlisted in a transaction.");
#endif

			string isolationLevelValue;
			switch (isolationLevel)
			{
			case IsolationLevel.ReadUncommitted:
				isolationLevelValue = "read uncommitted";
				break;

			case IsolationLevel.ReadCommitted:
				isolationLevelValue = "read committed";
				break;

			case IsolationLevel.Unspecified:
			// "In terms of the SQL:1992 transaction isolation levels, the default InnoDB level is REPEATABLE READ." - http://dev.mysql.com/doc/refman/5.7/en/innodb-transaction-model.html
			case IsolationLevel.RepeatableRead:
				isolationLevelValue = "repeatable read";
				break;

			case IsolationLevel.Serializable:
				isolationLevelValue = "serializable";
				break;

			case IsolationLevel.Chaos:
			case IsolationLevel.Snapshot:
			default:
				throw new NotSupportedException("IsolationLevel.{0} is not supported.".FormatInvariant(isolationLevel));
			}

			using (var cmd = new MySqlCommand("set transaction isolation level " + isolationLevelValue + "; start transaction;", this))
				await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			var transaction = new MySqlTransaction(this, isolationLevel);
			CurrentTransaction = transaction;
			return transaction;
		}

#if !NETSTANDARD1_3
		public override void EnlistTransaction(System.Transactions.Transaction transaction)
		{
			if (m_xaTransaction != null)
				throw new MySqlException("Already enlisted in a Transaction.");
			if (CurrentTransaction != null)
				throw new InvalidOperationException("Can't enlist in a Transaction when there is an active MySqlTransaction.");

			if (transaction != null)
			{
				m_xaTransaction = new MySqlXaTransaction(this);
				m_xaTransaction.Start(transaction);
			}
		}

		internal void UnenlistTransaction(MySqlXaTransaction xaTransaction)
		{
			if (!object.ReferenceEquals(xaTransaction, m_xaTransaction))
				throw new InvalidOperationException("Active transaction is not the one being unenlisted from.");
			m_xaTransaction = null;

			if (m_shouldCloseWhenUnenlisted)
			{
				m_shouldCloseWhenUnenlisted = false;
				Close();
			}
		}

		MySqlXaTransaction m_xaTransaction;
#endif

		public override void Close() => DoClose();

		public override void ChangeDatabase(string databaseName) => ChangeDatabaseAsync(IOBehavior.Synchronous, databaseName, CancellationToken.None).GetAwaiter().GetResult();
		public Task ChangeDatabaseAsync(string databaseName) => ChangeDatabaseAsync(IOBehavior.Asynchronous, databaseName, CancellationToken.None);
		public Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken) => ChangeDatabaseAsync(IOBehavior.Asynchronous, databaseName, cancellationToken);

		private async Task ChangeDatabaseAsync(IOBehavior ioBehavior, string databaseName, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(databaseName))
				throw new ArgumentException("Database name is not valid.", nameof(databaseName));
			if (State != ConnectionState.Open)
				throw new InvalidOperationException("Connection is not open.");

			CloseDatabase();

			await m_session.SendAsync(InitDatabasePayload.Create(databaseName), ioBehavior, cancellationToken).ConfigureAwait(false);
			var payload = await m_session.ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			OkPayload.Create(payload);
			m_session.DatabaseOverride = databaseName;
		}

		public new MySqlCommand CreateCommand() => (MySqlCommand) base.CreateCommand();

		public override void Open() => OpenAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public override Task OpenAsync(CancellationToken cancellationToken) =>
			OpenAsync(AsyncIOBehavior, cancellationToken);

		private async Task OpenAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyNotDisposed();
			if (State != ConnectionState.Closed)
				throw new InvalidOperationException("Cannot Open when State is {0}.".FormatInvariant(State));

			SetState(ConnectionState.Connecting);

			try
			{
				m_session = await CreateSessionAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

				m_hasBeenOpened = true;
				SetState(ConnectionState.Open);
			}
			catch (MySqlException)
			{
				SetState(ConnectionState.Closed);
				throw;
			}
			catch (SocketException ex)
			{
				SetState(ConnectionState.Closed);
				throw new MySqlException("Unable to connect to any of the specified MySQL hosts.", ex);
			}

#if !NETSTANDARD1_3
			if (m_connectionSettings.AutoEnlist && System.Transactions.Transaction.Current != null)
				EnlistTransaction(System.Transactions.Transaction.Current);
#endif
		}

		public override string ConnectionString
		{
			get => m_connectionStringBuilder.GetConnectionString(!m_hasBeenOpened || m_connectionSettings.PersistSecurityInfo);
			set
			{
				if (m_hasBeenOpened)
					throw new MySqlException("Cannot change connection string on a connection that has already been opened.");
				m_connectionStringBuilder = new MySqlConnectionStringBuilder(value);
				m_connectionSettings = new ConnectionSettings(m_connectionStringBuilder);
			}
		}

		public override string Database => m_session?.DatabaseOverride ?? m_connectionSettings.Database;

		public override ConnectionState State => m_connectionState;

		public override string DataSource => (m_connectionSettings.ConnectionType == ConnectionType.Tcp
			? string.Join(",", m_connectionSettings.Hostnames)
			: m_connectionSettings.UnixSocket) ?? "";

		public override string ServerVersion => m_session.ServerVersion.OriginalString;

		public int ServerThread => m_session.ConnectionId;

		public static void ClearPool(MySqlConnection connection) => ClearPoolAsync(connection, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		public static Task ClearPoolAsync(MySqlConnection connection) => ClearPoolAsync(connection, connection.AsyncIOBehavior, CancellationToken.None);
		public static Task ClearPoolAsync(MySqlConnection connection, CancellationToken cancellationToken) => ClearPoolAsync(connection, connection.AsyncIOBehavior, cancellationToken);
		public static void ClearAllPools() => ConnectionPool.ClearPoolsAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		public static Task ClearAllPoolsAsync() => ConnectionPool.ClearPoolsAsync(IOBehavior.Asynchronous, CancellationToken.None);
		public static Task ClearAllPoolsAsync(CancellationToken cancellationToken) => ConnectionPool.ClearPoolsAsync(IOBehavior.Asynchronous, cancellationToken);

		private static async Task ClearPoolAsync(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			var pool = ConnectionPool.GetPool(connection.m_connectionSettings);
			if (pool != null)
				await pool.ClearAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}

		protected override DbCommand CreateDbCommand() => new MySqlCommand(this, CurrentTransaction);

#if !NETSTANDARD1_3
		protected override DbProviderFactory DbProviderFactory => MySqlClientFactory.Instance;

		/// <summary>
		/// System.Data.Common, initial implementation of API DBConnection.GetSchema(String)
		/// Returns schema information for the data source of this DbConnection using the specified string for the schema name.
		/// </summary>
		/// <param name="collectionName">Specifies the name of the schema to return.</param>
		/// <returns>A DataTable that contains schema information.</returns>
		public override System.Data.DataTable GetSchema(string collectionName)
		{
			return GetSchema(collectionName, null);
		}

		
		/// <summary>
		/// System.Data.Common, initial implementation of API DBConnection.GetSchema(String)
		/// Returns schema information for the data source of this DbConnection using the specified string for the schema name.
		/// </summary>
		/// <param name="collectionName">Specifies the name of the schema to return.</param>
		/// <param name="restrictions">Restrictions not supported yet.</param>
		/// <returns>A DataTable that contains schema information.</returns>
		public override System.Data.DataTable GetSchema(string collectionName, string[] restrictions)
		{
			var dt = new DataTable(collectionName);
			switch (collectionName)
			{
			case "DataTypes":
				dt.Columns.AddRange(new[] { // The names come from DbMetaDataColumnNames
					new DataColumn("DataType", typeof(string)),
					new DataColumn("TypeName", typeof(string)),
					new DataColumn("ProviderDbType", typeof(int)),
					new DataColumn("IsUnsigned", typeof(bool))
				});

				// Column type mappings:
				var colTypes = Types.TypeMapper.Mapper.GetColumnMappings();
				foreach (var map in colTypes)
				{
					var dbTypeMap = map.DbTypeMapping;
					var dbType = dbTypeMap.DbTypes.FirstOrDefault();
					dt.Rows.Add(new object[] {
							dbTypeMap.ClrType.FullName,
							map.ColumnTypeName,
							(int)dbType,
							map.Unsigned
						});
				}

				// Data type mappings:
				foreach (MySqlDbType mapItem in Enum.GetValues(typeof(MySqlDbType)))
				{
					var typeName = Enum.GetName(typeof(MySqlDbType), mapItem);
					var dbType = Types.TypeMapper.ConvertFromMySqlDbType(mapItem);
					var map = Types.TypeMapper.Mapper.GetDbTypeMapping(dbType);
					if (map != null) // MySqlDbType.Set is not supported by the mapper.
					{
						dt.Rows.Add(new object[] {
							map.ClrType.FullName,
							Enum.GetName(typeof(MySqlDbType), mapItem).ToLower(),
							(int)dbType,
							typeName.Contains("UInt") || typeName.Contains("UByte")
						});
					}
				}
				return dt;

			case "Procedures":
				dt.Columns.AddRange(new[] {
					new DataColumn("ROUTINE_TYPE"),
					new DataColumn("ROUTINE_SCHEMA"),
					new DataColumn("SPECIFIC_NAME")
				});
				var procsQuery = "SELECT ROUTINE_TYPE, ROUTINE_SCHEMA, SPECIFIC_NAME FROM INFORMATION_SCHEMA.ROUTINES;";
				if (m_connectionState != ConnectionState.Open)
				{
					Open();
				}
				using (var com = new MySqlCommand(procsQuery, this))
				using (var reader = com.ExecuteReader())
				{
					while (reader.Read())
					{
						dt.Rows.Add(new object[] { reader.GetString(0), reader.GetString(1), reader.GetString(2) });
					}
				}
				return dt;

			default:
				throw new NotImplementedException("Not yet supported: GetSchema(\"" + collectionName + "\"). Please send a PR.");
			}
		}

#endif

		public override int ConnectionTimeout => m_connectionSettings.ConnectionTimeout;

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
					DoClose();
			}
			finally
			{
				m_isDisposed = !m_shouldCloseWhenUnenlisted;
				base.Dispose(disposing);
			}
		}

		internal MySqlSession Session
		{
			get
			{
				VerifyNotDisposed();
				return m_session;
			}
		}

		internal void Cancel(MySqlCommand command)
		{
			var session = Session;
			if (!session.TryStartCancel(command))
				return;

			try
			{
				// open a dedicated connection to the server to kill the active query
				var csb = new MySqlConnectionStringBuilder(m_connectionStringBuilder.GetConnectionString(includePassword: true));
				csb.Pooling = false;
				if (m_session.IPAddress != null)
					csb.Server = m_session.IPAddress.ToString();
				csb.ConnectionTimeout = 3u;

				using (var connection = new MySqlConnection(csb.ConnectionString))
				{
					connection.Open();
					using (var killCommand = new MySqlCommand("KILL QUERY {0}".FormatInvariant(command.Connection.ServerThread), connection))
					{
						session.DoCancel(command, killCommand);
					}
				}
			}
			catch (MySqlException)
			{
				// cancelling the query failed; setting the state back to 'Querying' will allow another call to 'Cancel' to try again
				session.AbortCancel(command);
			}
		}

		internal async Task<CachedProcedure> GetCachedProcedure(IOBehavior ioBehavior, string name, CancellationToken cancellationToken)
		{
			if (State != ConnectionState.Open)
				throw new InvalidOperationException("Connection is not open.");

			if (m_session.ServerVersion.Version < ServerVersions.SupportsProcedureCache)
				return null;

			if (m_cachedProcedures == null)
				m_cachedProcedures = new Dictionary<string, CachedProcedure>();

			var normalized = NormalizedSchema.MustNormalize(name, Database);
			if (!m_cachedProcedures.TryGetValue(normalized.FullyQualified, out var cachedProcedure))
			{
				cachedProcedure = await CachedProcedure.FillAsync(ioBehavior, this, normalized.Schema, normalized.Component, cancellationToken).ConfigureAwait(false);
				m_cachedProcedures[normalized.FullyQualified] = cachedProcedure;
			}
			return cachedProcedure;
		}

		internal MySqlTransaction CurrentTransaction { get; set; }
		internal bool AllowUserVariables => m_connectionSettings.AllowUserVariables;
		internal bool BufferResultSets => m_connectionSettings.BufferResultSets;
		internal bool ConvertZeroDateTime => m_connectionSettings.ConvertZeroDateTime;
		internal int DefaultCommandTimeout => m_connectionSettings.DefaultCommandTimeout;
		internal bool OldGuids => m_connectionSettings.OldGuids;
		internal bool TreatTinyAsBoolean => m_connectionSettings.TreatTinyAsBoolean;
		internal IOBehavior AsyncIOBehavior => m_connectionSettings.ForceSynchronous ? IOBehavior.Synchronous : IOBehavior.Asynchronous;

		internal MySqlSslMode SslMode => m_connectionSettings.SslMode;

		internal void SetActiveReader(MySqlDataReader dataReader)
		{
			if (dataReader == null)
				throw new ArgumentNullException(nameof(dataReader));
			if (m_activeReader != null)
				throw new InvalidOperationException("Can't replace active reader.");
			m_activeReader = dataReader;
		}

		internal void FinishQuerying()
		{
			m_session.FinishQuerying();
			m_activeReader = null;
		}

		private async Task<MySqlSession> CreateSessionAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			var connectTimeout = m_connectionSettings.ConnectionTimeout == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(m_connectionSettings.ConnectionTimeoutMilliseconds);
			using (var timeoutSource = new CancellationTokenSource(connectTimeout))
			using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token))
			{
				try
				{
					// get existing session from the pool if possible
					if (m_connectionSettings.Pooling)
					{
						var pool = ConnectionPool.GetPool(m_connectionSettings);
						// this returns an open session
						return await pool.GetSessionAsync(this, ioBehavior, linkedSource.Token).ConfigureAwait(false);
					}
					else
					{
						var session = new MySqlSession();
						await session.ConnectAsync(m_connectionSettings, ioBehavior, linkedSource.Token).ConfigureAwait(false);
						return session;
					}
				}
				catch (OperationCanceledException ex) when (timeoutSource.IsCancellationRequested)
				{
					throw new MySqlException("Connect Timeout expired.", ex);
				}
			}
		}

		internal void SetState(ConnectionState newState)
		{
			if (m_connectionState != newState)
			{
				var previousState = m_connectionState;
				m_connectionState = newState;
				OnStateChange(new StateChangeEventArgs(previousState, newState));
			}
		}

		private void VerifyNotDisposed()
		{
			if (m_isDisposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		private void DoClose()
		{
#if !NETSTANDARD1_3
			// If participating in a distributed transaction, keep the connection open so we can commit or rollback.
			// This handles the common pattern of disposing a connection before disposing a TransactionScope (e.g., nested using blocks)
			if (m_xaTransaction != null)
			{
				m_shouldCloseWhenUnenlisted = true;
				return;
			}
#else
			// fix "field is never assigned" compiler error
			m_shouldCloseWhenUnenlisted = false;
#endif

			if (m_connectionState != ConnectionState.Closed)
			{
				try
				{
					CloseDatabase();
				}
				finally
				{
					if (m_session != null)
					{
						if (m_connectionSettings.Pooling)
							m_session.ReturnToPool();
						else
							m_session.DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
						m_session = null;
					}
					SetState(ConnectionState.Closed);
				}
			}
		}

		private void CloseDatabase()
		{
			m_cachedProcedures = null;
			m_activeReader?.Dispose();
			if (CurrentTransaction != null && m_session.IsConnected)
			{
				CurrentTransaction.Dispose();
				CurrentTransaction = null;
			}
		}

		MySqlConnectionStringBuilder m_connectionStringBuilder;
		ConnectionSettings m_connectionSettings;
		MySqlSession m_session;
		ConnectionState m_connectionState;
		bool m_hasBeenOpened;
		bool m_isDisposed;
		bool m_shouldCloseWhenUnenlisted;
		Dictionary<string, CachedProcedure> m_cachedProcedures;
		MySqlDataReader m_activeReader;
	}
}
