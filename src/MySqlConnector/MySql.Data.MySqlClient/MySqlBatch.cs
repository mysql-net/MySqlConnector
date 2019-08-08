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
	public sealed class MySqlBatch : ICancellableCommand, IDisposable
	{
		public MySqlBatch()
			: this(null, null)
		{
		}

		public MySqlBatch(MySqlConnection connection = null, MySqlTransaction transaction = null)
		{
			Connection = connection;
			Transaction = transaction;
			BatchCommands = new MySqlBatchCommandCollection();
			m_commandId = ICancellableCommandExtensions.GetNextId();
		}

		public MySqlConnection Connection { get; set; }
		public MySqlTransaction Transaction { get; set; }
		public MySqlBatchCommandCollection BatchCommands { get; }

		public DbDataReader ExecuteReader() => ExecuteDbDataReader();
		public Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default) => ExecuteDbDataReaderAsync(cancellationToken);

		private DbDataReader ExecuteDbDataReader()
		{
			((ICancellableCommand) this).ResetCommandTimeout();
			return ExecuteReaderAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		}

		private Task<DbDataReader> ExecuteDbDataReaderAsync(CancellationToken cancellationToken)
		{
			((ICancellableCommand) this).ResetCommandTimeout();
			return ExecuteReaderAsync(AsyncIOBehavior, cancellationToken);
		}

		private Task<DbDataReader> ExecuteReaderAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (!IsValid(out var exception))
			 	return Utility.TaskFromException<DbDataReader>(exception);

			foreach (MySqlBatchCommand batchCommand in BatchCommands)
				batchCommand.Batch = this;

			var payloadCreator = Connection.Session.SupportsComMulti ? BatchedCommandPayloadCreator.Instance :
				IsPrepared ? SingleCommandPayloadCreator.Instance :
				ConcatenatedCommandPayloadCreator.Instance;
			return CommandExecutor.ExecuteReaderAsync(BatchCommands, payloadCreator, CommandBehavior.Default, ioBehavior, cancellationToken);
		}

		public int ExecuteNonQuery() => ExecuteNonQueryAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public object ExecuteScalar() => ExecuteScalarAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default) => ExecuteNonQueryAsync(AsyncIOBehavior, cancellationToken);

		public Task<object> ExecuteScalarAsync(CancellationToken cancellationToken = default) => ExecuteScalarAsync(AsyncIOBehavior, cancellationToken);

		public int Timeout { get; set; }

		public void Prepare()
		{
			if (!NeedsPrepare(out var exception))
			{
				if (exception is object)
					throw exception;
				return;
			}

			DoPrepareAsync(IOBehavior.Synchronous, default).GetAwaiter().GetResult();
		}

		public Task PrepareAsync(CancellationToken cancellationToken = default) => PrepareAsync(AsyncIOBehavior, cancellationToken);

		public void Cancel() => Connection?.Cancel(this);

		public void Dispose()
		{
			m_isDisposed = true;
		}

		int ICancellableCommand.CommandId => m_commandId;
		int ICancellableCommand.CommandTimeout => Timeout;
		int ICancellableCommand.CancelAttemptCount { get; set; }

		IDisposable ICancellableCommand.RegisterCancel(CancellationToken token)
		{
			if (!token.CanBeCanceled)
				return null;

			if (m_cancelAction is null)
				m_cancelAction = Cancel;
			return token.Register(m_cancelAction);
		}

		private async Task<int> ExecuteNonQueryAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			((ICancellableCommand) this).ResetCommandTimeout();
			using (var reader = (MySqlDataReader) await ExecuteReaderAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
			{
				do
				{
					while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
					{
					}
				} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
				return reader.RecordsAffected;
			}
		}

		private async Task<object> ExecuteScalarAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			((ICancellableCommand) this).ResetCommandTimeout();
			var hasSetResult = false;
			object result = null;
			using (var reader = (MySqlDataReader) await ExecuteReaderAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
			{
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
			}
			return result;
		}

		private bool IsValid(out Exception exception)
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
			else if (BatchCommands.Count == 0)
				exception = new InvalidOperationException("BatchCommands must contain a command");

			if (exception is null)
			{
				foreach (var command in BatchCommands)
				{
					if ((command.CommandBehavior & CommandBehavior.CloseConnection) != 0)
					{
						exception = new NotSupportedException("CommandBehavior.CloseConnection is not supported by MySqlBatch");
						break;
					}
				}
			}

			return exception is null;
		}

		private bool NeedsPrepare(out Exception exception)
		{
			exception = null;
			if (Connection is null)
				exception = new InvalidOperationException("Connection property must be non-null.");
			else if (Connection.State != ConnectionState.Open)
				exception = new InvalidOperationException("Connection must be Open; current state is {0}".FormatInvariant(Connection.State));
			else if (BatchCommands.Count == 0)
				exception = new InvalidOperationException("BatchCommands must contain a command");
			else if (Connection?.HasActiveReader ?? false)
				exception = new InvalidOperationException("Cannot call Prepare when there is an open DataReader for this command; it must be closed first.");

			return exception is null && !Connection.IgnorePrepare;
		}

		private Task PrepareAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (!NeedsPrepare(out var exception))
				return exception is null ? Utility.CompletedTask : Utility.TaskFromException(exception);

			return DoPrepareAsync(ioBehavior, cancellationToken);
		}

		private async Task DoPrepareAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			foreach (IMySqlCommand batchCommand in BatchCommands)
			{
				if (batchCommand.CommandType != CommandType.Text)
					throw new NotSupportedException("Only CommandType.Text is currently supported by MySqlBatch.Prepare");
				((MySqlBatchCommand) batchCommand).Batch = this;

				// don't prepare the same SQL twice
				if (Connection.Session.TryGetPreparedStatement(batchCommand.CommandText) is null)
					await Connection.Session.PrepareAsync(batchCommand, ioBehavior, cancellationToken).ConfigureAwait(false);
			}
		}

		private bool IsPrepared
		{
			get
			{
				foreach (var command in BatchCommands)
				{
					if (Connection.Session.TryGetPreparedStatement(command.CommandText) is null)
						return false;
				}
				return true;
			}
		}

		private IOBehavior AsyncIOBehavior => Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous;

		readonly int m_commandId;
		bool m_isDisposed;
		Action m_cancelAction;
	}
}
