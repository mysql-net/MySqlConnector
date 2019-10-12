using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlCommand : DbCommand, IMySqlCommand, ICancellableCommand
#if !NETSTANDARD1_3
		, ICloneable
#endif
	{
		public MySqlCommand()
			: this(null, null, null)
		{
		}

		public MySqlCommand(string? commandText)
			: this(commandText, null, null)
		{
		}

		public MySqlCommand(MySqlConnection? connection, MySqlTransaction? transaction)
			: this(null, connection, transaction)
		{
		}

		public MySqlCommand(string? commandText, MySqlConnection? connection)
			: this(commandText, connection, null)
		{
		}

		public MySqlCommand(string? commandText, MySqlConnection? connection, MySqlTransaction? transaction)
		{
			GC.SuppressFinalize(this);
			m_commandId = ICancellableCommandExtensions.GetNextId();
			CommandText = commandText;
			Connection = connection;
			Transaction = transaction;
			CommandType = CommandType.Text;
		}

		private MySqlCommand(MySqlCommand other)
			: this(other.CommandText, other.Connection, other.Transaction)
		{
			m_commandTimeout = other.m_commandTimeout;
			m_commandType = other.m_commandType;
			DesignTimeVisible = other.DesignTimeVisible;
			UpdatedRowSource = other.UpdatedRowSource;
			m_parameterCollection = other.CloneRawParameters();
		}

		public new MySqlParameterCollection Parameters
		{
			get
			{
				VerifyNotDisposed();
				return m_parameterCollection ??= new MySqlParameterCollection();
			}
		}

		MySqlParameterCollection? IMySqlCommand.RawParameters => m_parameterCollection;

		public new MySqlParameter CreateParameter() => (MySqlParameter) base.CreateParameter();

		public override void Cancel() => Connection?.Cancel(this);

		public override int ExecuteNonQuery() => ExecuteNonQueryAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public override object ExecuteScalar() => ExecuteScalarAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public new MySqlDataReader ExecuteReader() => (MySqlDataReader) base.ExecuteReader();

		public new MySqlDataReader ExecuteReader(CommandBehavior commandBehavior) => (MySqlDataReader) base.ExecuteReader(commandBehavior);

		public override void Prepare()
		{
			if (!NeedsPrepare(out var exception))
			{
				if (exception is object)
					throw exception;
				return;
			}

			Connection!.Session.PrepareAsync(this, IOBehavior.Synchronous, default).GetAwaiter().GetResult();
		}

#if !NETSTANDARD2_1 && !NETCOREAPP3_0
		public Task PrepareAsync(CancellationToken cancellationToken = default) => PrepareAsync(AsyncIOBehavior, cancellationToken);
#else
		public override Task PrepareAsync(CancellationToken cancellationToken = default) => PrepareAsync(AsyncIOBehavior, cancellationToken);
#endif

		internal MySqlParameterCollection? CloneRawParameters()
		{
			if (m_parameterCollection is null)
				return null;
			var parameters = new MySqlParameterCollection();
			foreach (var parameter in (IEnumerable<MySqlParameter>) m_parameterCollection)
				parameters.Add(parameter.Clone());
			return parameters;
		}

		private Task PrepareAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (!NeedsPrepare(out var exception))
				return exception is null ? Utility.CompletedTask : Utility.TaskFromException(exception);

			return Connection!.Session.PrepareAsync(this, ioBehavior, cancellationToken);
		}

		private bool NeedsPrepare(out Exception? exception)
		{
			exception = null;
			if (Connection is null)
				exception = new InvalidOperationException("Connection property must be non-null.");
			else if (Connection.State != ConnectionState.Open)
				exception = new InvalidOperationException("Connection must be Open; current state is {0}".FormatInvariant(Connection.State));
			else if (string.IsNullOrWhiteSpace(CommandText))
				exception = new InvalidOperationException("CommandText must be specified");
			else if (Connection?.HasActiveReader ?? false)
				exception = new InvalidOperationException("Cannot call Prepare when there is an open DataReader for this command; it must be closed first.");

			if (exception is object || Connection!.IgnorePrepare)
				return false;

			if (CommandType != CommandType.Text)
			{
				exception = new NotSupportedException("Only CommandType.Text is currently supported by MySqlCommand.Prepare");
				return false;
			}

			// don't prepare the same SQL twice
			return Connection.Session.TryGetPreparedStatement(CommandText!) is null;
		}

		public override string? CommandText
		{
			get => m_commandText;
			set
			{
				if (m_connection?.HasActiveReader ?? false)
					throw new InvalidOperationException("Cannot set MySqlCommand.CommandText when there is an open DataReader for this command; it must be closed first.");
				m_commandText = value;
			}
		}

		public bool IsPrepared => ((IMySqlCommand) this).TryGetPreparedStatements() is object;

		public new MySqlTransaction? Transaction { get; set; }

		public new MySqlConnection? Connection
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
			get => m_commandType;
			set
			{
				if (value != CommandType.Text && value != CommandType.StoredProcedure)
					throw new ArgumentException("CommandType must be Text or StoredProcedure.", nameof(value));
				m_commandType = value;
			}
		}

		public override bool DesignTimeVisible { get; set; }

		public override UpdateRowSource UpdatedRowSource { get; set; }

		public long LastInsertedId { get; private set; }

		void IMySqlCommand.SetLastInsertedId(long value) => LastInsertedId = value;

		protected override DbConnection? DbConnection
		{
			get => Connection;
			set => Connection = (MySqlConnection?) value;
		}

		protected override DbParameterCollection DbParameterCollection => Parameters;

		protected override DbTransaction? DbTransaction
		{
			get => Transaction;
			set => Transaction = (MySqlTransaction?) value;
		}

		protected override DbParameter CreateDbParameter()
		{
			VerifyNotDisposed();
			return new MySqlParameter();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			this.ResetCommandTimeout();
			return ExecuteReaderAsync(behavior, IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		}

		public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
			ExecuteNonQueryAsync(AsyncIOBehavior, cancellationToken);

		internal async Task<int> ExecuteNonQueryAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			this.ResetCommandTimeout();
			using var reader = (MySqlDataReader) await ExecuteReaderAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
			do
			{
				while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
				{
				}
			} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
			return reader.RecordsAffected;
		}

		public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) =>
			ExecuteScalarAsync(AsyncIOBehavior, cancellationToken);

		internal async Task<object> ExecuteScalarAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			this.ResetCommandTimeout();
			var hasSetResult = false;
			object? result = null;
			using var reader = (MySqlDataReader) await ExecuteReaderAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
			do
			{
				var hasResult = await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				if (!hasSetResult)
				{
					if (hasResult)
						result = reader.GetValue(0);
					hasSetResult = true;
				}
			} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
			return result!;
		}

		protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
		{
			this.ResetCommandTimeout();
			return ExecuteReaderAsync(behavior, AsyncIOBehavior, cancellationToken);
		}

		internal Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (!IsValid(out var exception))
				return Utility.TaskFromException<DbDataReader>(exception);

			m_commandBehavior = behavior;
			return CommandExecutor.ExecuteReaderAsync(new IMySqlCommand[] { this }, SingleCommandPayloadCreator.Instance, behavior, ioBehavior, cancellationToken);
		}

		public MySqlCommand Clone() => new MySqlCommand(this);

#if !NETSTANDARD1_3
		object ICloneable.Clone() => Clone();
#endif

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
					m_parameterCollection = null;
			}
			finally
			{
				base.Dispose(disposing);
			}
			m_isDisposed = true;
		}

#if !NETSTANDARD2_1 && !NETCOREAPP3_0
		public Task DisposeAsync()
#else
		public override ValueTask DisposeAsync()
#endif
		{
			Dispose();
			return Utility.CompletedValueTask;
		}

		/// <summary>
		/// Registers <see cref="Cancel"/> as a callback with <paramref name="token"/> if cancellation is supported.
		/// </summary>
		/// <param name="token">The <see cref="CancellationToken"/>.</param>
		/// <returns>An object that must be disposed to revoke the cancellation registration.</returns>
		/// <remarks>This method is more efficient than calling <code>token.Register(Command.Cancel)</code> because it avoids
		/// unnecessary allocations.</remarks>
		IDisposable? ICancellableCommand.RegisterCancel(CancellationToken token)
		{
			if (!token.CanBeCanceled)
				return null;

			m_cancelAction ??= Cancel;
			return token.Register(m_cancelAction);
		}

		int ICancellableCommand.CommandId => m_commandId;

		int ICancellableCommand.CancelAttemptCount { get; set; }

		ICancellableCommand IMySqlCommand.CancellableCommand => this;

		private IOBehavior AsyncIOBehavior => Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous;

		private void VerifyNotDisposed()
		{
			if (m_isDisposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		private bool IsValid([NotNullWhen(false)] out Exception? exception)
		{
			exception = null;
			if (m_isDisposed)
				exception = new ObjectDisposedException(GetType().Name);
			else if (Connection is null)
				exception = new InvalidOperationException("Connection property must be non-null.");
			else if (Connection.State != ConnectionState.Open && Connection.State != ConnectionState.Connecting)
				exception = new InvalidOperationException("Connection must be Open; current state is {0}".FormatInvariant(Connection.State));
			else if (!Connection.IgnoreCommandTransaction && Transaction != Connection.CurrentTransaction)
				exception = new InvalidOperationException("The transaction associated with this command is not the connection's active transaction; see https://fl.vu/mysql-trans");
			else if (string.IsNullOrWhiteSpace(CommandText))
				exception = new InvalidOperationException("CommandText must be specified");
			return exception is null;
		}

		PreparedStatements? IMySqlCommand.TryGetPreparedStatements() => CommandType == CommandType.Text && !string.IsNullOrWhiteSpace(CommandText) && m_connection is object &&
			m_connection.State == ConnectionState.Open ? m_connection.Session.TryGetPreparedStatement(CommandText!) : null;

		CommandBehavior IMySqlCommand.CommandBehavior => m_commandBehavior;
		MySqlParameterCollection? IMySqlCommand.OutParameters { get; set; }
		MySqlParameter? IMySqlCommand.ReturnParameter { get; set; }

		readonly int m_commandId;
		bool m_isDisposed;
		MySqlConnection? m_connection;
		string? m_commandText;
		MySqlParameterCollection? m_parameterCollection;
		int? m_commandTimeout;
		CommandType m_commandType;
		CommandBehavior m_commandBehavior;
		Action? m_cancelAction;
	}
}
