using System;
using System.Data;
using System.Data.Common;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlConnection : DbConnection
	{
		public MySqlConnection()
		{
			m_connectionStringBuilder = new MySqlConnectionStringBuilder();
		}

		public MySqlConnection(string connectionString)
			: this()
		{
			ConnectionString = connectionString;
		}

		public new MySqlTransaction BeginTransaction() => (MySqlTransaction) base.BeginTransaction();

		public Task<MySqlTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default(CancellationToken)) =>
			BeginDbTransactionAsync(IsolationLevel.Unspecified, IOBehavior.Asynchronous, cancellationToken);

		public Task<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken)) =>
			BeginDbTransactionAsync(isolationLevel, IOBehavior.Asynchronous, cancellationToken);

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
			BeginDbTransactionAsync(isolationLevel, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		private async Task<MySqlTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (State != ConnectionState.Open)
				throw new InvalidOperationException("Connection is not open.");
			if (CurrentTransaction != null)
				throw new InvalidOperationException("Transactions may not be nested.");

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

			using (var cmd = new MySqlCommand("set session transaction isolation level " + isolationLevelValue + "; start transaction;", this))
				await cmd.ExecuteNonQueryAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			var transaction = new MySqlTransaction(this, isolationLevel);
			CurrentTransaction = transaction;
			return transaction;
		}

#if !NETSTANDARD1_3
		public override void EnlistTransaction(System.Transactions.Transaction transaction)
		{
			throw new NotSupportedException("System.Transactions.Transaction is not supported. Use BeginTransaction instead.");
		}
#endif

		public override void Close() => DoClose();

		public override void ChangeDatabase(string databaseName)
		{
			throw new NotImplementedException();
		}

		public override void Open() => OpenAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public override Task OpenAsync(CancellationToken cancellationToken) =>
			OpenAsync(IOBehavior.Asynchronous, cancellationToken);

		private async Task OpenAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyNotDisposed();
			if (State != ConnectionState.Closed)
				throw new InvalidOperationException("Cannot Open when State is {0}.".FormatInvariant(State));
#if !NETSTANDARD1_3
			if (System.Transactions.Transaction.Current != null)
				throw new NotSupportedException("Ambient transactions are not supported. Use BeginTransaction instead.");
#endif

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
		}

		public override string ConnectionString
		{
			get
			{
				return m_connectionStringBuilder.GetConnectionString(!m_hasBeenOpened || m_connectionStringBuilder.PersistSecurityInfo);
			}
			set
			{
				m_connectionStringBuilder = new MySqlConnectionStringBuilder(value);
				m_database = m_connectionStringBuilder.Database;

				if (m_connectionStringBuilder.UseCompression)
					throw new NotSupportedException("Compression=True is not supported.");
				if (!m_connectionStringBuilder.UseAffectedRows)
					throw new NotSupportedException("UseAffectedRows=False is not supported.");
			}
		}

		public override string Database => m_database;

		public override ConnectionState State => m_connectionState;

		public override string DataSource => m_connectionStringBuilder.Server;

		public override string ServerVersion => m_session.ServerVersion.OriginalString;

		public int ServerThread => m_session.ConnectionId;

		public static void ClearPool(MySqlConnection connection) => ClearPoolAsync(connection, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		public static Task ClearPoolAsync(MySqlConnection connection) => ClearPoolAsync(connection, IOBehavior.Asynchronous, CancellationToken.None);
		public static Task ClearPoolAsync(MySqlConnection connection, CancellationToken cancellationToken) => ClearPoolAsync(connection, IOBehavior.Asynchronous, cancellationToken);
		public static void ClearAllPools() => ConnectionPool.ClearPoolsAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		public static Task ClearAllPoolsAsync() => ConnectionPool.ClearPoolsAsync(IOBehavior.Asynchronous, CancellationToken.None);
		public static Task ClearAllPoolsAsync(CancellationToken cancellationToken) => ConnectionPool.ClearPoolsAsync(IOBehavior.Asynchronous, cancellationToken);

		private static async Task ClearPoolAsync(MySqlConnection connection, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			var pool = ConnectionPool.GetPool(connection.m_connectionStringBuilder);
			if (pool != null)
				await pool.ClearAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
		}

		protected override DbCommand CreateDbCommand() => new MySqlCommand(this, CurrentTransaction);

#if !NETSTANDARD1_3
		protected override DbProviderFactory DbProviderFactory => MySqlClientFactory.Instance;
#endif

		public override int ConnectionTimeout => (int) m_connectionStringBuilder.ConnectionTimeout;

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
					DoClose();
			}
			finally
			{
				m_isDisposed = true;
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

		internal MySqlTransaction CurrentTransaction { get; set; }
		internal bool HasActiveReader { get; set; }
		internal bool AllowUserVariables => m_connectionStringBuilder.AllowUserVariables;
		internal bool ConvertZeroDateTime => m_connectionStringBuilder.ConvertZeroDateTime;
		internal bool OldGuids => m_connectionStringBuilder.OldGuids;

		private async Task<MySqlSession> CreateSessionAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			var connectTimeout = m_connectionStringBuilder.ConnectionTimeout == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(checked((int) m_connectionStringBuilder.ConnectionTimeout));
			using (var timeoutSource = new CancellationTokenSource(connectTimeout))
			using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token))
			{
				try
				{
					// get existing session from the pool if possible
					if (m_connectionStringBuilder.Pooling)
					{
						var pool = ConnectionPool.GetPool(m_connectionStringBuilder);

						// this returns an open session
						return await pool.GetSessionAsync(ioBehavior, linkedSource.Token).ConfigureAwait(false);
					}
					else
					{
						var session = new MySqlSession();
						await session.ConnectAsync(m_connectionStringBuilder.Server.Split(','), (int) m_connectionStringBuilder.Port, m_connectionStringBuilder.UserID,
							m_connectionStringBuilder.Password, m_connectionStringBuilder.Database, ioBehavior, linkedSource.Token).ConfigureAwait(false);
						return session;
					}
				}
				catch (OperationCanceledException ex) when (timeoutSource.IsCancellationRequested)
				{
					throw new MySqlException("Connect Timeout expired.", ex);
				}
			}
		}

		private void SetState(ConnectionState newState)
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
			if (m_connectionState != ConnectionState.Closed)
			{
				if (CurrentTransaction != null)
				{
					CurrentTransaction.Dispose();
					CurrentTransaction = null;
				}
				if (m_session != null)
				{
					if (m_connectionStringBuilder.Pooling)
						m_session.ReturnToPool();
					else
						m_session.DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
					m_session = null;
				}
				SetState(ConnectionState.Closed);
			}
		}

		MySqlConnectionStringBuilder m_connectionStringBuilder;
		MySqlSession m_session;
		ConnectionState m_connectionState;
		bool m_hasBeenOpened;
		bool m_isDisposed;
		string m_database;
	}
}
