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

		public async Task<MySqlTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return await BeginDbTransactionAsync(IsolationLevel.Unspecified, cancellationToken).ConfigureAwait(false) as MySqlTransaction;
		}

		public async Task<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken))
		{
			return await BeginDbTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false) as MySqlTransaction;
		}

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
		{
			return BeginDbTransactionAsync(isolationLevel).GetAwaiter().GetResult();
		}

		private async Task<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken))
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
				await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

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

		public override void Open() => OpenAsync(CancellationToken.None).GetAwaiter().GetResult();

		public override async Task OpenAsync(CancellationToken cancellationToken)
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
				m_session = await CreateSessionAsync(cancellationToken).ConfigureAwait(false);

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

		public static void ClearPool(MySqlConnection connection) => ClearPoolAsync(connection, CancellationToken.None).GetAwaiter().GetResult();
		public static void ClearAllPools() => ClearAllPoolsAsync(CancellationToken.None).GetAwaiter().GetResult();
		public static Task ClearPoolAsync(MySqlConnection connection) => ClearPoolAsync(connection, CancellationToken.None);
		public static Task ClearAllPoolsAsync() => ClearAllPoolsAsync(CancellationToken.None);
		public static Task ClearAllPoolsAsync(CancellationToken cancellationToken) => ConnectionPool.ClearPoolsAsync(cancellationToken);

		public static async Task ClearPoolAsync(MySqlConnection connection, CancellationToken cancellationToken)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			var pool = ConnectionPool.GetPool(connection.m_connectionStringBuilder);
			if (pool != null)
				await pool.ClearAsync(cancellationToken).ConfigureAwait(false);
		}

		protected override DbCommand CreateDbCommand() => new MySqlCommand(this, CurrentTransaction);

#if !NETSTANDARD1_3
		protected override DbProviderFactory DbProviderFactory => MySqlClientFactory.Instance;
#endif

		public override int ConnectionTimeout
		{
			get { throw new NotImplementedException(); }
		}

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

		private async Task<MySqlSession> CreateSessionAsync(CancellationToken cancellationToken)
		{
			// get existing session from the pool if possible
			if (m_connectionStringBuilder.Pooling)
			{
				var pool = ConnectionPool.GetPool(m_connectionStringBuilder);
				// this returns an open session
				return await pool.GetSessionAsync(cancellationToken).ConfigureAwait(false);
			}
			else
			{
				var session = new MySqlSession(null);
				await session.ConnectAsync(m_connectionStringBuilder.Server.Split(','), (int) m_connectionStringBuilder.Port, m_connectionStringBuilder.UserID,
					m_connectionStringBuilder.Password, m_connectionStringBuilder.Database, (int) m_connectionStringBuilder.ConnectionTimeout, cancellationToken).ConfigureAwait(false);
				return session;
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
						m_session.DisposeAsync(CancellationToken.None).GetAwaiter().GetResult();
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
