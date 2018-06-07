using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

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
			CommandId = Interlocked.Increment(ref s_commandId);
			CommandText = commandText;
			Connection = connection;
			Transaction = transaction;
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

		public new MySqlParameter CreateParameter() => (MySqlParameter) base.CreateParameter();

		public override void Cancel() => Connection.Cancel(this);

		public override int ExecuteNonQuery()
		{
			ResetCommandTimeout();
			return ExecuteNonQueryAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		}

		public override object ExecuteScalar()
		{
			ResetCommandTimeout();
			return ExecuteScalarAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		}

		public new MySqlDataReader ExecuteReader() => (MySqlDataReader) base.ExecuteReader();

		public new MySqlDataReader ExecuteReader(CommandBehavior commandBehavior) => (MySqlDataReader) base.ExecuteReader(commandBehavior);

		public override void Prepare()
		{
			if (Connection == null)
				throw new InvalidOperationException("Connection property must be non-null.");
			if (Connection.State != ConnectionState.Open)
				throw new InvalidOperationException("Connection must be Open; current state is {0}".FormatInvariant(Connection.State));
			if (string.IsNullOrWhiteSpace(CommandText))
				throw new InvalidOperationException("CommandText must be specified");
			if (Connection.IgnorePrepare)
				return;

			// NOTE: Prepared statements in MySQL are not currently supported.
			// 1) Only a subset of statements are actually preparable by the server: http://dev.mysql.com/worklog/task/?id=2871
			// 2) Although CLIENT_MULTI_STATEMENTS is supposed to mean that the Server "Can handle multiple statements per COM_QUERY and COM_STMT_PREPARE" (https://dev.mysql.com/doc/internals/en/capability-flags.html#flag-CLIENT_MULTI_STATEMENTS),
			//    this is not actually true because "Prepared statement handles are defined to work only with strings that contain a single statement" (http://dev.mysql.com/doc/refman/5.7/en/c-api-multiple-queries.html).
			throw new NotSupportedException("Prepared commands are not supported.");
		}

		public override string CommandText
		{
			get => m_commandText;
			set
			{
				if (m_connection?.HasActiveReader ?? false)
					throw new InvalidOperationException("Cannot set MySqlCommand.CommandText when there is an open DataReader for this command; it must be closed first.");
				m_commandText = value;
			}
		}

		public new MySqlTransaction Transaction { get; set; }

		public new MySqlConnection Connection
		{
			get => m_connection;
			set
			{
				if (m_connection?.HasActiveReader ?? false)
					throw new InvalidOperationException("Cannot set MySqlCommand.Connection when there is an open DataReader for this command; it must be closed first.");
				m_connection = value;
			}
		}

		public override int CommandTimeout
		{
			get => Math.Min(m_commandTimeout ?? Connection?.DefaultCommandTimeout ?? 0, int.MaxValue / 1000);
			set => m_commandTimeout = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "CommandTimeout must be greater than or equal to zero.");
		}

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

		public override UpdateRowSource UpdatedRowSource { get; set; }

		public long LastInsertedId { get; internal set; }

		protected override DbConnection DbConnection
		{
			get => Connection;
			set => Connection = (MySqlConnection) value;
		}

		protected override DbParameterCollection DbParameterCollection => m_parameterCollection;

		protected override DbTransaction DbTransaction
		{
			get => Transaction;
			set => Transaction = (MySqlTransaction) value;
		}

		protected override DbParameter CreateDbParameter()
		{
			VerifyNotDisposed();
			return new MySqlParameter();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			ResetCommandTimeout();
			return ExecuteReaderAsync(behavior, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		}

		public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
			ExecuteNonQueryAsync(AsyncIOBehavior, cancellationToken);

		internal Task<int> ExecuteNonQueryAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) =>
			!IsValid(out var exception) ? Utility.TaskFromException<int>(exception) :
				m_commandExecutor.ExecuteNonQueryAsync(CommandText, m_parameterCollection, ioBehavior, cancellationToken);

		public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) =>
			ExecuteScalarAsync(AsyncIOBehavior, cancellationToken);

		internal Task<object> ExecuteScalarAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) =>
			!IsValid(out var exception) ? Utility.TaskFromException<object>(exception) :
				m_commandExecutor.ExecuteScalarAsync(CommandText, m_parameterCollection, ioBehavior, cancellationToken);

		protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
		{
			ResetCommandTimeout();
			return ExecuteReaderAsync(behavior, AsyncIOBehavior, cancellationToken);
		}

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

		internal int CommandId { get; }

		internal int CancelAttemptCount { get; set; }

		/// <summary>
		/// Causes the effective command timeout to be reset back to the value specified by <see cref="CommandTimeout"/>.
		/// </summary>
		/// <remarks>As per the <a href="https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlcommand.commandtimeout.aspx">MSDN documentation</a>,
		/// "This property is the cumulative time-out (for all network packets that are read during the invocation of a method) for all network reads during command
		/// execution or processing of the results. A time-out can still occur after the first row is returned, and does not include user processing time, only network
		/// read time. For example, with a 30 second time out, if Read requires two network packets, then it has 30 seconds to read both network packets. If you call
		/// Read again, it will have another 30 seconds to read any data that it requires."
		/// The <see cref="ResetCommandTimeout"/> method is called by public ADO.NET API methods to reset the effective time remaining at the beginning of a new
		/// method call.</remarks>
		internal void ResetCommandTimeout()
		{
			var commandTimeout = CommandTimeout;
			Connection?.Session?.SetTimeout(commandTimeout == 0 ? Constants.InfiniteTimeout : commandTimeout * 1000);
		}

		private IOBehavior AsyncIOBehavior => Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous;

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
			else if (Connection == null)
				exception = new InvalidOperationException("Connection property must be non-null.");
			else if (Connection.State != ConnectionState.Open && Connection.State != ConnectionState.Connecting)
				exception = new InvalidOperationException("Connection must be Open; current state is {0}".FormatInvariant(Connection.State));
			else if (!Connection.IgnoreCommandTransaction && Transaction != Connection.CurrentTransaction)
				exception = new InvalidOperationException("The transaction associated with this command is not the connection's active transaction; see https://github.com/mysql-net/MySqlConnector/issues/474");
			else if (string.IsNullOrWhiteSpace(CommandText))
				exception = new InvalidOperationException("CommandText must be specified");
			return exception == null;
		}

		internal void ReaderClosed() => (m_commandExecutor as StoredProcedureCommandExecutor)?.SetParams();

		static int s_commandId = 1;

		MySqlConnection m_connection;
		string m_commandText;
		MySqlParameterCollection m_parameterCollection;
		int? m_commandTimeout;
		CommandType m_commandType;
		ICommandExecutor m_commandExecutor;
		Action m_cancelAction;
	}
}
