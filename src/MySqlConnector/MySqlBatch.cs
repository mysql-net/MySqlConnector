using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector
{
	/// <summary>
	/// <para><see cref="MySqlBatch"/> implements the new
	/// <a href="https://github.com/dotnet/runtime/issues/28633">ADO.NET batching API</a>.
	/// <strong>It is currently experimental</strong> and may change in the future.</para>
	/// <para>When using MariaDB (10.2 or later), the commands will be sent in a single batch, reducing network
	/// round-trip time. With other MySQL Servers, this may be no more efficient than executing the commands
	/// individually.</para>
	/// <para>Example usage:</para>
	/// <code>
	/// using var connection = new MySqlConnection("...connection string...");
	/// await connection.OpenAsync();
	///
	/// using var batch = new MySqlBatch(connection)
	/// {
	/// 	BatchCommands =
	/// 	{
	/// 		new MySqlBatchCommand("INSERT INTO departments(name) VALUES(@name);")
	/// 		{
	/// 			Parameters =
	/// 			{
	/// 				new MySqlParameter("@name", "Sales"),
	/// 			},
	/// 		},
	/// 		new MySqlBatchCommand("SET @dept_id = last_insert_id()"),
	/// 		new MySqlBatchCommand("INSERT INTO employees(name, department_id) VALUES(@name, @dept_id);")
	/// 		{
	/// 			Parameters =
	/// 			{
	/// 				new MySqlParameter("@name", "Jim Halpert"),
	/// 			},
	/// 		},
	/// 	 	new MySqlBatchCommand("INSERT INTO employees(name, department_id) VALUES(@name, @dept_id);")
	/// 		{
	/// 			Parameters =
	/// 			{
	/// 				new MySqlParameter("@name", "Dwight Schrute"),
	/// 			},
	/// 		},
	/// 	},
	///  };
	///  await batch.ExecuteNonQueryAsync();
	/// </code>
	/// </summary>
	/// <remarks>The proposed ADO.NET API that <see cref="MySqlBatch"/> is based on is not finalized. This API is experimental and may change in the future.</remarks>
	public sealed class MySqlBatch : ICancellableCommand, IDisposable
	{
		/// <summary>
		/// Initializes a new <see cref="MySqlBatch"/> object. The <see cref="Connection"/> property must be set before this object can be used.
		/// </summary>
		public MySqlBatch()
			: this(null, null)
		{
		}

		/// <summary>
		/// Initializes a new <see cref="MySqlBatch"/> object, setting the <see cref="Connection"/> and <see cref="Transaction"/> if specified.
		/// </summary>
		/// <param name="connection">(Optional) The <see cref="MySqlConnection"/> to use.</param>
		/// <param name="transaction">(Optional) The <see cref="MySqlTransaction"/> to use.</param>
		public MySqlBatch(MySqlConnection? connection = null, MySqlTransaction? transaction = null)
		{
			Connection = connection;
			Transaction = transaction;
			BatchCommands = new();
			m_commandId = ICancellableCommandExtensions.GetNextId();
		}

		public MySqlConnection? Connection { get; set; }
		public MySqlTransaction? Transaction { get; set; }

		/// <summary>
		/// The collection of commands that will be executed in the batch.
		/// </summary>
		public MySqlBatchCommandCollection BatchCommands { get; }

		/// <summary>
		/// Executes all the commands in the batch, returning a <see cref="MySqlDataReader"/> that can iterate
		/// over the result sets. If multiple resultsets are returned, use <see cref="MySqlDataReader.NextResult"/>
		/// to access them.
		/// </summary>
		public MySqlDataReader ExecuteReader() => ExecuteDbDataReader();

		/// <summary>
		/// Executes all the commands in the batch, returning a <see cref="MySqlDataReader"/> that can iterate
		/// over the result sets. If multiple resultsets are returned, use <see cref="MySqlDataReader.NextResultAsync(CancellationToken)"/>
		/// to access them.
		/// </summary>
		/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
		/// <returns>A <see cref="Task{MySqlDataReader}"/> containing the result of the asynchronous operation.</returns>
		public Task<MySqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default) => ExecuteDbDataReaderAsync(cancellationToken);

		private MySqlDataReader ExecuteDbDataReader()
		{
			((ICancellableCommand) this).ResetCommandTimeout();
			return ExecuteReaderAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		}

		private Task<MySqlDataReader> ExecuteDbDataReaderAsync(CancellationToken cancellationToken)
		{
			((ICancellableCommand) this).ResetCommandTimeout();
			return ExecuteReaderAsync(AsyncIOBehavior, cancellationToken);
		}

		private Task<MySqlDataReader> ExecuteReaderAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (!IsValid(out var exception))
			 	return Utility.TaskFromException<MySqlDataReader>(exception);

			foreach (var batchCommand in BatchCommands)
				batchCommand.Batch = this;

			var payloadCreator = Connection!.Session.SupportsComMulti ? BatchedCommandPayloadCreator.Instance :
				IsPrepared ? SingleCommandPayloadCreator.Instance :
				ConcatenatedCommandPayloadCreator.Instance;
			return CommandExecutor.ExecuteReaderAsync(BatchCommands!, payloadCreator, CommandBehavior.Default, ioBehavior, cancellationToken);
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
				if (exception is not null)
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

		IDisposable? ICancellableCommand.RegisterCancel(CancellationToken token)
		{
			if (!token.CanBeCanceled)
				return null;

			m_cancelAction ??= Cancel;
			return token.Register(m_cancelAction);
		}

		private async Task<int> ExecuteNonQueryAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			((ICancellableCommand) this).ResetCommandTimeout();
			using var reader = await ExecuteReaderAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			do
			{
				while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
				{
				}
			} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
			return reader.RecordsAffected;
		}

		private async Task<object> ExecuteScalarAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			((ICancellableCommand) this).ResetCommandTimeout();
			var hasSetResult = false;
			object? result = null;
			using var reader = await ExecuteReaderAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
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
				exception = new InvalidOperationException("The transaction associated with this batch is not the connection's active transaction; see https://fl.vu/mysql-trans");
			else if (BatchCommands.Count == 0)
				exception = new InvalidOperationException("BatchCommands must contain a command");
			else
				exception = GetExceptionForInvalidCommands();

			return exception is null;
		}

		private bool NeedsPrepare(out Exception? exception)
		{
			exception = null;
			if (m_isDisposed)
				exception = new ObjectDisposedException(GetType().Name);
			else if (Connection is null)
				exception = new InvalidOperationException("Connection property must be non-null.");
			else if (Connection.State != ConnectionState.Open)
				exception = new InvalidOperationException("Connection must be Open; current state is {0}".FormatInvariant(Connection.State));
			else if (BatchCommands.Count == 0)
				exception = new InvalidOperationException("BatchCommands must contain a command");
			else if (Connection?.HasActiveReader ?? false)
				exception = new InvalidOperationException("Cannot call Prepare when there is an open DataReader for this command; it must be closed first.");
			else
				exception = GetExceptionForInvalidCommands();

			return exception is null && !Connection!.IgnorePrepare;
		}

		private Exception? GetExceptionForInvalidCommands()
		{
			foreach (var command in BatchCommands)
			{
				if (command is null)
					return new InvalidOperationException("BatchCommands must not contain null");
				if ((command.CommandBehavior & CommandBehavior.CloseConnection) != 0)
					return new NotSupportedException("CommandBehavior.CloseConnection is not supported by MySqlBatch");
				if (string.IsNullOrWhiteSpace(command.CommandText))
					return new InvalidOperationException("CommandText must be specified on each batch command");
			}
			return null;
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
				if (Connection!.Session.TryGetPreparedStatement(batchCommand.CommandText!) is null)
					await Connection.Session.PrepareAsync(batchCommand, ioBehavior, cancellationToken).ConfigureAwait(false);
			}
		}

		private bool IsPrepared
		{
			get
			{
				foreach (var command in BatchCommands)
				{
					if (Connection!.Session.TryGetPreparedStatement(command!.CommandText!) is null)
						return false;
				}
				return true;
			}
		}

		private IOBehavior AsyncIOBehavior => Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous;

		readonly int m_commandId;
		bool m_isDisposed;
		Action? m_cancelAction;
	}
}
