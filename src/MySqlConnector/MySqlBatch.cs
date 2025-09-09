using System.Diagnostics.CodeAnalysis;
using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector;

/// <summary>
/// <para><see cref="MySqlBatch"/> implements the new
/// <a href="https://github.com/dotnet/runtime/issues/28633">ADO.NET batching API</a>.
/// <strong>It is currently experimental</strong> and may change in the future.</para>
/// <para>When using MariaDB (10.2 or later), the commands will be sent in a single batch, reducing network
/// round-trip time. With other MySQL Servers, this may be no more efficient than executing the commands
/// individually.</para>
/// <para>Example usage:</para>
/// <code>
/// await using var connection = new MySqlConnection("...connection string...");
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
#if NET6_0_OR_GREATER
#pragma warning disable CA1063 // Implement IDisposable Correctly
#endif
public sealed class MySqlBatch :
#if NET6_0_OR_GREATER
	DbBatch,
#endif
	ICancellableCommand, IDisposable
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
		BatchCommands = [];
		m_commandId = ICancellableCommandExtensions.GetNextId();
	}

#if NET6_0_OR_GREATER
	public new MySqlConnection? Connection { get; set; }
	protected override DbConnection? DbConnection { get => Connection; set => Connection = (MySqlConnection?) value; }
	public new MySqlTransaction? Transaction { get; set; }
	protected override DbTransaction? DbTransaction { get => Transaction; set => Transaction = (MySqlTransaction?) value; }
#else
	public MySqlConnection? Connection { get; set; }
	public MySqlTransaction? Transaction { get; set; }
#endif

	/// <summary>
	/// The collection of commands that will be executed in the batch.
	/// </summary>
#if NET6_0_OR_GREATER
	public new MySqlBatchCommandCollection BatchCommands { get; }
	protected override DbBatchCommandCollection DbBatchCommands => BatchCommands;
#else
	public MySqlBatchCommandCollection BatchCommands { get; }
#endif

	/// <summary>
	/// Executes all the commands in the batch, returning a <see cref="MySqlDataReader"/> that can iterate
	/// over the result sets. If multiple resultsets are returned, use <see cref="MySqlDataReader.NextResult"/>
	/// to access them.
	/// </summary>
#if NET6_0_OR_GREATER
	public new MySqlDataReader ExecuteReader(CommandBehavior commandBehavior = CommandBehavior.Default) =>
#else
	public MySqlDataReader ExecuteReader(CommandBehavior commandBehavior = CommandBehavior.Default) =>
#endif
		(MySqlDataReader) ExecuteDbDataReader(commandBehavior);

	/// <summary>
	/// Executes all the commands in the batch, returning a <see cref="MySqlDataReader"/> that can iterate
	/// over the result sets. If multiple resultsets are returned, use <see cref="MySqlDataReader.NextResultAsync(CancellationToken)"/>
	/// to access them.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A <see cref="Task{MySqlDataReader}"/> containing the result of the asynchronous operation.</returns>
#if NET6_0_OR_GREATER
	public new async Task<MySqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default) =>
#else
	public async Task<MySqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default) =>
#endif
		(MySqlDataReader) await ExecuteDbDataReaderAsync(CommandBehavior.Default, cancellationToken).ConfigureAwait(false);

	//// TODO: new ExecuteReaderAsync(CommandBehavior)

#if NET6_0_OR_GREATER
	protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
#else
	[SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Matches .NET 6.0 override")]
	private DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
#endif
	{
		this.ResetCommandTimeout();
#pragma warning disable CA2012 // OK to read .Result because the ValueTask is completed
		return ExecuteReaderAsync(behavior, IOBehavior.Synchronous, CancellationToken.None).Result;
#pragma warning restore CA2012
	}

#if NET6_0_OR_GREATER
	protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
#else
	private async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
#endif
	{
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		return await ExecuteReaderAsync(behavior, AsyncIOBehavior, cancellationToken).ConfigureAwait(false);
	}

	private ValueTask<MySqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (!IsValid(out var exception))
			return ValueTaskExtensions.FromException<MySqlDataReader>(exception);

		CurrentCommandBehavior = behavior;
		foreach (MySqlBatchCommand batchCommand in BatchCommands)
			batchCommand.Batch = this;

		var payloadCreator = IsPrepared ? SingleCommandPayloadCreator.Instance :
			ConcatenatedCommandPayloadCreator.Instance;
		return CommandExecutor.ExecuteReaderAsync(new(BatchCommands!.Commands), payloadCreator, behavior, default, ioBehavior, cancellationToken);
	}

#if NET6_0_OR_GREATER
	public override int ExecuteNonQuery() =>
#else
	public int ExecuteNonQuery() =>
#endif
		ExecuteNonQueryAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

#if NET6_0_OR_GREATER
	public override object? ExecuteScalar() =>
#else
	public object? ExecuteScalar() =>
#endif
		ExecuteScalarAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

#if NET6_0_OR_GREATER
	public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default) =>
#else
	public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default) =>
#endif
		ExecuteNonQueryAsync(AsyncIOBehavior, cancellationToken);

#if NET6_0_OR_GREATER
	public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default) =>
#else
	public Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default) =>
#endif
		ExecuteScalarAsync(AsyncIOBehavior, cancellationToken);

	public
#if NET6_0_OR_GREATER
		override
#endif
		int Timeout
	{
		get;
		set
		{
			field = value;
			((ICancellableCommand) this).EffectiveCommandTimeout = null;
		}
	}

#if NET6_0_OR_GREATER
	public override void Prepare()
#else
	public void Prepare()
#endif
	{
		if (!NeedsPrepare(out var exception))
		{
			if (exception is not null)
				throw exception;
			return;
		}

		DoPrepareAsync(IOBehavior.Synchronous, default).GetAwaiter().GetResult();
	}

#if NET6_0_OR_GREATER
	public override Task PrepareAsync(CancellationToken cancellationToken = default) =>
#else
	public Task PrepareAsync(CancellationToken cancellationToken = default) =>
#endif
		PrepareAsync(AsyncIOBehavior, cancellationToken);

#if NET6_0_OR_GREATER
	public override void Cancel() =>
#else
	public void Cancel() =>
#endif
		Connection?.Cancel(this, m_commandId, true);

#if NET6_0_OR_GREATER
	protected override DbBatchCommand CreateDbBatchCommand() => new MySqlBatchCommand();
#endif

#if NET6_0_OR_GREATER
	public override void Dispose()
#else
	public void Dispose()
#endif
	{
		m_isDisposed = true;
#if NET6_0_OR_GREATER
		base.Dispose();
#endif
	}

	internal CommandBehavior CurrentCommandBehavior { get; set; }

	int ICancellableCommand.CommandId => m_commandId;
	int ICancellableCommand.CommandTimeout => Timeout;
	int? ICancellableCommand.EffectiveCommandTimeout { get; set; }
	int ICancellableCommand.CancelAttemptCount { get; set; }

	CancellationTokenRegistration ICancellableCommand.RegisterCancel(CancellationToken cancellationToken)
	{
		if (!cancellationToken.CanBeCanceled)
			return default;

		m_cancelAction ??= Cancel;
		return cancellationToken.Register(m_cancelAction);
	}

	void ICancellableCommand.SetTimeout(int milliseconds)
	{
		Volatile.Write(ref m_commandTimedOut, false);

		if (m_cancelTimerId != 0)
			TimerQueue.Instance.Remove(m_cancelTimerId);

		if (milliseconds != Constants.InfiniteTimeout)
		{
			m_cancelForCommandTimeoutAction ??= CancelCommandForTimeout;
			m_cancelTimerId = TimerQueue.Instance.Add(milliseconds, m_cancelForCommandTimeoutAction);
		}
	}

	bool ICancellableCommand.IsTimedOut => Volatile.Read(ref m_commandTimedOut);

	private void CancelCommandForTimeout()
	{
		Volatile.Write(ref m_commandTimedOut, true);
		Cancel();
	}

	private async Task<int> ExecuteNonQueryAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		using var reader = await ExecuteReaderAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
		do
		{
			while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
			{
			}
		} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
		return reader.RecordsAffected;
	}

	private async Task<object?> ExecuteScalarAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		var hasSetResult = false;
		object? result = null;
		using var reader = await ExecuteReaderAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
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
		return result;
	}

	private bool IsValid([NotNullWhen(false)] out Exception? exception)
	{
		if (m_isDisposed)
			exception = new ObjectDisposedException(GetType().Name);
		else if (Connection is null)
			exception = new InvalidOperationException("Connection property must be non-null.");
		else if (Connection.State is not ConnectionState.Open and not ConnectionState.Connecting)
			exception = new InvalidOperationException($"Connection must be Open; current state is {Connection.State}");
		else if (!Connection.IgnoreCommandTransaction && Transaction != Connection.CurrentTransaction)
			exception = new InvalidOperationException("The transaction associated with this batch is not the connection's active transaction; see https://mysqlconnector.net/trans");
		else if (BatchCommands.Count == 0)
			exception = new InvalidOperationException("BatchCommands must contain a command");
		else
			exception = GetExceptionForInvalidCommands();

		return exception is null;
	}

	private bool NeedsPrepare(out Exception? exception)
	{
		if (m_isDisposed)
			exception = new ObjectDisposedException(GetType().Name);
		else if (Connection is null)
			exception = new InvalidOperationException("Connection property must be non-null.");
		else if (Connection.State != ConnectionState.Open)
			exception = new InvalidOperationException($"Connection must be Open; current state is {Connection.State}");
		else if (BatchCommands.Count == 0)
			exception = new InvalidOperationException("BatchCommands must contain a command");
		else if (Connection.HasActiveReader)
			exception = new InvalidOperationException("Cannot call Prepare when there is an open DataReader for this command; it must be closed first.");
		else
			exception = GetExceptionForInvalidCommands();

		return exception is null && !Connection!.IgnorePrepare;
	}

	private InvalidOperationException? GetExceptionForInvalidCommands()
	{
		foreach (var command in BatchCommands)
		{
			if (command is null)
				return new InvalidOperationException("BatchCommands must not contain null");
			if (string.IsNullOrWhiteSpace(command.CommandText))
				return new InvalidOperationException("CommandText must be specified on each batch command");
		}
		return null;
	}

	private Task PrepareAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (!NeedsPrepare(out var exception))
			return exception is null ? Task.CompletedTask : Task.FromException(exception);

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

	private readonly int m_commandId;
	private bool m_isDisposed;
	private Action? m_cancelAction;
	private Action? m_cancelForCommandTimeoutAction;
	private uint m_cancelTimerId;
	private bool m_commandTimedOut;
}
