using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient.CommandExecutors;
using MySql.Data.Protocol.Serialization;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlCommand : DbCommand
	{
		public MySqlCommand()
			: this(null, null, null)
		{
		}

		public MySqlCommand(string commandText)
			: this(commandText, null, null)
		{
		}

		public MySqlCommand(MySqlConnection connection, MySqlTransaction transaction)
			: this(null, connection, transaction)
		{
		}

		public MySqlCommand(string commandText, MySqlConnection connection)
			: this(commandText, connection, null)
		{
		}

		public MySqlCommand(string commandText, MySqlConnection connection, MySqlTransaction transaction)
		{
			CommandText = commandText;
			DbConnection = connection;
			DbTransaction = transaction;
			m_parameterCollection = new MySqlParameterCollection();
			CommandType = CommandType.Text;
		}

		public new MySqlParameterCollection Parameters
		{
			get
			{
				VerifyNotDisposed();
				return m_parameterCollection;
			}
		}

		public override void Cancel() => Connection.Cancel(this);

		public override int ExecuteNonQuery() =>
			ExecuteNonQueryAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public override object ExecuteScalar() =>
			ExecuteScalarAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public override void Prepare()
		{
			// NOTE: Prepared statements in MySQL are not currently supported.
			// 1) Only a subset of statements are actually preparable by the server: http://dev.mysql.com/worklog/task/?id=2871
			// 2) Although CLIENT_MULTI_STATEMENTS is supposed to mean that the Server "Can handle multiple statements per COM_QUERY and COM_STMT_PREPARE" (https://dev.mysql.com/doc/internals/en/capability-flags.html#flag-CLIENT_MULTI_STATEMENTS),
			//    this is not actually true because "Prepared statement handles are defined to work only with strings that contain a single statement" (http://dev.mysql.com/doc/refman/5.7/en/c-api-multiple-queries.html).
		}

		public override string CommandText { get; set; }
		public override int CommandTimeout { get; set; }

		public override CommandType CommandType
		{
			get
			{
				return m_commandType;
			}
			set
			{
				if (value != CommandType.Text && value != CommandType.StoredProcedure)
					throw new ArgumentException("CommandType must be Text or StoredProcedure.", nameof(value));
				if (value == m_commandType)
					return;

				m_commandType = value;
				if (value == CommandType.Text)
					m_commandExecutor = new TextCommandExecutor(this);
				else if (value == CommandType.StoredProcedure)
					m_commandExecutor = new StoredProcedureCommandExecutor(this);
			}
		}

		public override bool DesignTimeVisible { get; set; }

		public override UpdateRowSource UpdatedRowSource
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		public long LastInsertedId { get; internal set; }

		protected override DbConnection DbConnection { get; set; }
		protected override DbParameterCollection DbParameterCollection => m_parameterCollection;
		protected override DbTransaction DbTransaction { get; set; }

		protected override DbParameter CreateDbParameter()
		{
			VerifyNotDisposed();
			return new MySqlParameter();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
			ExecuteReaderAsync(behavior, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
			ExecuteNonQueryAsync(Connection.AsyncIOBehavior, cancellationToken);

		internal Task<int> ExecuteNonQueryAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) =>
			!IsValid(out var exception) ? Utility.TaskFromException<int>(exception) :
				m_commandExecutor.ExecuteNonQueryAsync(CommandText, m_parameterCollection, ioBehavior, cancellationToken);

		public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) =>
			ExecuteScalarAsync(Connection.AsyncIOBehavior, cancellationToken);

		internal Task<object> ExecuteScalarAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) =>
			!IsValid(out var exception) ? Utility.TaskFromException<object>(exception) :
				m_commandExecutor.ExecuteScalarAsync(CommandText, m_parameterCollection, ioBehavior, cancellationToken);

		protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) =>
			ExecuteReaderAsync(behavior, Connection.AsyncIOBehavior, cancellationToken);

		internal Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken) =>
			!IsValid(out var exception) ? Utility.TaskFromException<DbDataReader>(exception) :
				m_commandExecutor.ExecuteReaderAsync(CommandText, m_parameterCollection, behavior, ioBehavior, cancellationToken);

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					m_parameterCollection = null;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		internal new MySqlConnection Connection => (MySqlConnection) DbConnection;

		/// <summary>
		/// Registers <see cref="Cancel"/> as a callback with <paramref name="token"/> if cancellation is supported.
		/// </summary>
		/// <param name="token">The <see cref="CancellationToken"/>.</param>
		/// <returns>An object that must be disposed to revoke the cancellation registration.</returns>
		/// <remarks>This method is more efficient than calling <code>token.Register(Command.Cancel)</code> because it avoids
		/// unnecessary allocations.</remarks>
		internal IDisposable RegisterCancel(CancellationToken token)
		{
			if (!token.CanBeCanceled)
				return null;

			if (m_cancelAction == null)
				m_cancelAction = Cancel;
			return token.Register(m_cancelAction);
		}

		private void VerifyNotDisposed()
		{
			if (m_parameterCollection == null)
				throw new ObjectDisposedException(GetType().Name);
		}

		private bool IsValid(out Exception exception)
		{
			exception = null;
			if (m_parameterCollection == null)
				exception = new ObjectDisposedException(GetType().Name);
			else if (DbConnection == null)
				exception = new InvalidOperationException("Connection property must be non-null.");
			else if (DbConnection.State != ConnectionState.Open && DbConnection.State != ConnectionState.Connecting)
				exception = new InvalidOperationException("Connection must be Open; current state is {0}".FormatInvariant(DbConnection.State));
			else if (DbTransaction != Connection.CurrentTransaction)
				exception = new InvalidOperationException("The transaction associated with this command is not the connection's active transaction.");
			else if (string.IsNullOrWhiteSpace(CommandText))
				exception = new InvalidOperationException("CommandText must be specified");
			return exception == null;
		}

		internal void ReaderClosed() => (m_commandExecutor as StoredProcedureCommandExecutor)?.SetParams();

		MySqlParameterCollection m_parameterCollection;
		CommandType m_commandType;
		ICommandExecutor m_commandExecutor;
		Action m_cancelAction;
	}
}
