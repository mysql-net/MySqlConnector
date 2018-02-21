using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Core;
using MySqlConnector.Logging;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

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
		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => BeginDbTransactionAsync(isolationLevel, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public Task<MySqlTransaction> BeginTransactionAsync() => BeginDbTransactionAsync(IsolationLevel.Unspecified, AsyncIOBehavior, CancellationToken.None);
		public Task<MySqlTransaction> BeginTransactionAsync(CancellationToken cancellationToken) => BeginDbTransactionAsync(IsolationLevel.Unspecified, AsyncIOBehavior, cancellationToken);
		public Task<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel) => BeginDbTransactionAsync(isolationLevel, AsyncIOBehavior, CancellationToken.None);
		public Task<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) => BeginDbTransactionAsync(isolationLevel, AsyncIOBehavior, cancellationToken);

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

		public bool Ping() => PingAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		public Task<bool> PingAsync() => PingAsync((m_connectionSettings?.ForceSynchronous ?? false) ? IOBehavior.Synchronous : IOBehavior.Asynchronous, CancellationToken.None).AsTask();
		public Task<bool> PingAsync(CancellationToken cancellationToken) => PingAsync((m_connectionSettings?.ForceSynchronous ?? false) ? IOBehavior.Synchronous : IOBehavior.Asynchronous, cancellationToken).AsTask();

		private async ValueTask<bool> PingAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (m_session == null)
				return false;
			try
			{
				if (await m_session.TryPingAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
					return true;
			}
			catch (InvalidOperationException)
			{
			}

			SetState(ConnectionState.Closed);
			return false;
		}

		public override void Open() => OpenAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public override Task OpenAsync(CancellationToken cancellationToken) => OpenAsync(default, cancellationToken);

		private async Task OpenAsync(IOBehavior? ioBehavior, CancellationToken cancellationToken)
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
			get
			{
				if (!m_hasBeenOpened)
					return m_connectionString;
				var connectionStringBuilder = GetConnectionSettings().ConnectionStringBuilder;
				return connectionStringBuilder.GetConnectionString(connectionStringBuilder.PersistSecurityInfo);
			}
			set
			{
				if (m_hasBeenOpened)
					throw new InvalidOperationException("Cannot change connection string on a connection that has already been opened.");
				m_connectionString = value;
			}
		}

		public override string Database => m_session?.DatabaseOverride ?? GetConnectionSettings().Database;

		public override ConnectionState State => m_connectionState;

		public override string DataSource => (GetConnectionSettings().ConnectionType == ConnectionType.Tcp
			? string.Join(",", GetConnectionSettings().HostNames)
			: GetConnectionSettings().UnixSocket) ?? "";

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

			var pool = ConnectionPool.GetPool(connection.m_connectionString);
			if (pool != null)
				await pool.ClearAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}

		protected override DbCommand CreateDbCommand() => new MySqlCommand(this, null);

#if !NETSTANDARD1_3
		protected override DbProviderFactory DbProviderFactory => MySqlClientFactory.Instance;

		/// <inheritdoc cref="DbConnection.GetSchema()"/>
		public override DataTable GetSchema() => GetSchemaProvider().GetSchema();

		/// <inheritdoc cref="DbConnection.GetSchema(string)"/>
		public override DataTable GetSchema(string collectionName) => GetSchemaProvider().GetSchema(collectionName);

		/// <inheritdoc cref="DbConnection.GetSchema(string)"/>
		public override DataTable GetSchema(string collectionName, string[] restrictions) => GetSchemaProvider().GetSchema(collectionName);

		private SchemaProvider GetSchemaProvider()
		{
			if (m_schemaProvider == null)
				m_schemaProvider = new SchemaProvider(this);
			return m_schemaProvider;
		}

		SchemaProvider m_schemaProvider;
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

		internal ServerSession Session
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
				var csb = new MySqlConnectionStringBuilder(m_connectionString);
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
			catch (MySqlException ex)
			{
				// cancelling the query failed; setting the state back to 'Querying' will allow another call to 'Cancel' to try again
				Log.Warn(ex, "Session{0} cancelling command {1} failed", m_session.Id, command.CommandId);
				session.AbortCancel(command);
			}
		}

		internal async Task<CachedProcedure> GetCachedProcedure(IOBehavior ioBehavior, string name, CancellationToken cancellationToken)
		{
			if (State != ConnectionState.Open)
				throw new InvalidOperationException("Connection is not open.");

			if (m_session.ServerVersion.Version < ServerVersions.SupportsProcedureCache)
				return null;

			var cachedProcedures = m_session.Pool?.GetProcedureCache() ?? m_cachedProcedures;
			if (cachedProcedures == null)
				cachedProcedures = m_cachedProcedures = new Dictionary<string, CachedProcedure>();

			var normalized = NormalizedSchema.MustNormalize(name, Database);
			if (string.IsNullOrEmpty(normalized.Schema))
				return null;

			CachedProcedure cachedProcedure;
			bool foundProcedure;
			lock (cachedProcedures)
				foundProcedure = cachedProcedures.TryGetValue(normalized.FullyQualified, out cachedProcedure);
			if (!foundProcedure)
			{
				cachedProcedure = await CachedProcedure.FillAsync(ioBehavior, this, normalized.Schema, normalized.Component, cancellationToken).ConfigureAwait(false);
				lock (cachedProcedures)
					cachedProcedures[normalized.FullyQualified] = cachedProcedure;
			}
			return cachedProcedure;
		}

		internal MySqlTransaction CurrentTransaction { get; set; }
		internal bool AllowUserVariables => m_connectionSettings.AllowUserVariables;
		internal bool ConvertZeroDateTime => m_connectionSettings.ConvertZeroDateTime;
		internal int DefaultCommandTimeout => GetConnectionSettings().DefaultCommandTimeout;
		internal bool OldGuids => m_connectionSettings.OldGuids;
		internal bool TreatTinyAsBoolean => m_connectionSettings.TreatTinyAsBoolean;
		internal IOBehavior AsyncIOBehavior => GetConnectionSettings().ForceSynchronous ? IOBehavior.Synchronous : IOBehavior.Asynchronous;

		internal MySqlSslMode SslMode => m_connectionSettings.SslMode;

		internal bool HasActiveReader => m_activeReader != null;

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

		private async ValueTask<ServerSession> CreateSessionAsync(IOBehavior? ioBehavior, CancellationToken cancellationToken)
		{
			var pool = ConnectionPool.GetPool(m_connectionString);
			m_connectionSettings = pool?.ConnectionSettings ?? new ConnectionSettings(new MySqlConnectionStringBuilder(m_connectionString));
			var actualIOBehavior = ioBehavior ?? (m_connectionSettings.ForceSynchronous ? IOBehavior.Synchronous : IOBehavior.Asynchronous);

			var connectTimeout = m_connectionSettings.ConnectionTimeout == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(m_connectionSettings.ConnectionTimeoutMilliseconds);
			using (var timeoutSource = new CancellationTokenSource(connectTimeout))
			using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token))
			{
				try
				{
					// get existing session from the pool if possible
					if (pool != null)
					{
						// this returns an open session
						return await pool.GetSessionAsync(this, actualIOBehavior, linkedSource.Token).ConfigureAwait(false);
					}
					else
					{
						// only "fail over" and "random" load balancers supported without connection pooling
						var loadBalancer = m_connectionSettings.LoadBalance == MySqlLoadBalance.Random && m_connectionSettings.HostNames.Count > 1 ?
							RandomLoadBalancer.Instance : FailOverLoadBalancer.Instance;

						var session = new ServerSession();
						Log.Info("Created new non-pooled Session{0}", session.Id);
						await session.ConnectAsync(m_connectionSettings, loadBalancer, actualIOBehavior, linkedSource.Token).ConfigureAwait(false);
						return session;
					}
				}
				catch (OperationCanceledException ex) when (timeoutSource.IsCancellationRequested)
				{
					throw new MySqlException("Connect Timeout expired.", ex);
				}
			}
		}

		internal bool SslIsEncrypted => m_session.SslIsEncrypted;

		internal bool SslIsSigned => m_session.SslIsSigned;

		internal bool SslIsAuthenticated => m_session.SslIsAuthenticated;

		internal bool SslIsMutuallyAuthenticated => m_session.SslIsMutuallyAuthenticated;

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

		private ConnectionSettings GetConnectionSettings()
		{
			if (m_connectionSettings == null)
				m_connectionSettings = new ConnectionSettings(new MySqlConnectionStringBuilder(m_connectionString));
			return m_connectionSettings;
		}

		static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(MySqlConnection));

		string m_connectionString;
		ConnectionSettings m_connectionSettings;
		ServerSession m_session;
		ConnectionState m_connectionState;
		bool m_hasBeenOpened;
		bool m_isDisposed;
		bool m_shouldCloseWhenUnenlisted;
		Dictionary<string, CachedProcedure> m_cachedProcedures;
		MySqlDataReader m_activeReader;
	}
}
