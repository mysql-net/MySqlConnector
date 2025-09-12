using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector;

/// <summary>
/// <see cref="MySqlCommand"/> represents a SQL statement or stored procedure name
/// to execute against a MySQL database.
/// </summary>
public sealed class MySqlCommand : DbCommand, IMySqlCommand, ICancellableCommand, ICloneable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MySqlCommand"/> class.
	/// </summary>
	public MySqlCommand()
		: this(null, null, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MySqlCommand"/> class, setting <see cref="CommandText"/> to <paramref name="commandText"/>.
	/// </summary>
	/// <param name="commandText">The text to assign to <see cref="CommandText"/>.</param>
	public MySqlCommand(string? commandText)
		: this(commandText, null, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MySqlCommand"/> class with the specified <see cref="MySqlConnection"/> and <see cref="MySqlTransaction"/>.
	/// </summary>
	/// <param name="connection">The <see cref="MySqlConnection"/> to use.</param>
	/// <param name="transaction">The active <see cref="MySqlTransaction"/>, if any.</param>
	public MySqlCommand(MySqlConnection? connection, MySqlTransaction? transaction)
		: this(null, connection, transaction)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MySqlCommand"/> class with the specified command text and <see cref="MySqlConnection"/>.
	/// </summary>
	/// <param name="commandText">The text to assign to <see cref="CommandText"/>.</param>
	/// <param name="connection">The <see cref="MySqlConnection"/> to use.</param>
	public MySqlCommand(string? commandText, MySqlConnection? connection)
		: this(commandText, connection, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MySqlCommand"/> class with the specified command text,<see cref="MySqlConnection"/>, and <see cref="MySqlTransaction"/>.
	/// </summary>
	/// <param name="commandText">The text to assign to <see cref="CommandText"/>.</param>
	/// <param name="connection">The <see cref="MySqlConnection"/> to use.</param>
	/// <param name="transaction">The active <see cref="MySqlTransaction"/>, if any.</param>
	public MySqlCommand(string? commandText, MySqlConnection? connection, MySqlTransaction? transaction)
	{
		GC.SuppressFinalize(this);
		m_commandId = ICancellableCommandExtensions.GetNextId();
		CommandText = commandText ?? "";
		Connection = connection;
		Transaction = transaction;
		CommandType = CommandType.Text;
	}

	private MySqlCommand(MySqlCommand other)
		: this(other.CommandText, other.Connection, other.Transaction)
	{
		GC.SuppressFinalize(this);
		m_commandTimeout = other.m_commandTimeout;
		((ICancellableCommand) this).EffectiveCommandTimeout = null;
		CommandType = other.CommandType;
		DesignTimeVisible = other.DesignTimeVisible;
		UpdatedRowSource = other.UpdatedRowSource;
		m_parameterCollection = other.CloneRawParameters();
		m_attributeCollection = other.CloneRawAttributes();
	}

	/// <summary>
	/// The collection of <see cref="MySqlParameter"/> objects for this command.
	/// </summary>
	public new MySqlParameterCollection Parameters => m_parameterCollection ??= [];

	MySqlParameterCollection? IMySqlCommand.RawParameters => m_parameterCollection;

	/// <summary>
	/// The collection of <see cref="MySqlAttribute"/> objects for this command.
	/// </summary>
	public MySqlAttributeCollection Attributes => m_attributeCollection ??= [];

	MySqlAttributeCollection? IMySqlCommand.RawAttributes => m_attributeCollection;

	public new MySqlParameter CreateParameter() => (MySqlParameter) base.CreateParameter();

	/// <inheritdoc/>
	public override void Cancel() => Connection?.Cancel(this, m_commandId, true);

#pragma warning disable CA2012 // OK to read .Result because the ValueTask is completed
	/// <summary>
	/// Executes this command on the associated <see cref="MySqlConnection"/>.
	/// </summary>
	/// <returns>The number of rows affected.</returns>
	/// <remarks>For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command.
	/// For stored procedures, the return value is the number of rows affected by the last statement in the stored procedure,
	/// or zero if the last statement is a SELECT. For all other types of statements, the return value is -1.</remarks>
	public override int ExecuteNonQuery() => ExecuteNonQueryAsync(IOBehavior.Synchronous, CancellationToken.None).Result;

	public override object? ExecuteScalar() => ExecuteScalarAsync(IOBehavior.Synchronous, CancellationToken.None).Result;

	public new MySqlDataReader ExecuteReader() => ExecuteReaderAsync(default, IOBehavior.Synchronous, default).Result;

	public new MySqlDataReader ExecuteReader(CommandBehavior commandBehavior) => ExecuteReaderAsync(commandBehavior, IOBehavior.Synchronous, default).GetAwaiter().GetResult();
#pragma warning restore CA2012 // OK to read .Result because the ValueTask is completed

	/// <inheritdoc/>
	public override void Prepare()
	{
		if (!NeedsPrepare(out var exception))
		{
			if (exception is not null)
				throw exception;
			return;
		}

		Connection!.Session.PrepareAsync(this, IOBehavior.Synchronous, default).GetAwaiter().GetResult();
	}

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public override Task PrepareAsync(CancellationToken cancellationToken = default) => PrepareAsync(AsyncIOBehavior, cancellationToken);
#else
	public Task PrepareAsync(CancellationToken cancellationToken = default) => PrepareAsync(AsyncIOBehavior, cancellationToken);
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

	private MySqlAttributeCollection? CloneRawAttributes()
	{
		if (m_attributeCollection is null)
			return null;
		var attributes = new MySqlAttributeCollection();
		foreach (var attribute in m_attributeCollection)
			attributes.Add(new MySqlAttribute(attribute.AttributeName, attribute.Value));
		return attributes;
	}

	bool IMySqlCommand.AllowUserVariables => AllowUserVariables;

	internal bool AllowUserVariables { get; set; }
	internal bool NoActivity { get; set; }

	private Task PrepareAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (!NeedsPrepare(out var exception))
			return exception is null ? Task.CompletedTask : Task.FromException(exception);

		return Connection!.Session.PrepareAsync(this, ioBehavior, cancellationToken);
	}

	private bool NeedsPrepare(out Exception? exception)
	{
		exception = null;
		if (Connection is null)
			exception = new InvalidOperationException("Connection property must be non-null.");
		else if (Connection.State != ConnectionState.Open)
			exception = new InvalidOperationException($"Connection must be Open; current state is {Connection.State}");
		else if (string.IsNullOrWhiteSpace(CommandText))
			exception = new InvalidOperationException("CommandText must be specified");
		else if (Connection?.HasActiveReader is true)
			exception = new InvalidOperationException("Cannot call Prepare when there is an open DataReader for this command's connection; it must be closed first.");

		if (exception is not null || Connection!.IgnorePrepare)
			return false;

		if (CommandType is not CommandType.StoredProcedure and not CommandType.Text)
		{
			exception = new NotSupportedException("Only CommandType.Text and CommandType.StoredProcedure are currently supported by MySqlCommand.Prepare.");
			return false;
		}

		// don't prepare the same SQL twice
		return Connection.Session.TryGetPreparedStatement(CommandText!) is null;
	}

	/// <summary>
	/// Gets or sets the command text to execute.
	/// </summary>
	/// <remarks>If <see cref="CommandType"/> is <see cref="CommandType.Text"/>, this is one or more SQL statements to execute.
	/// If <see cref="CommandType"/> is <see cref="CommandType.StoredProcedure"/>, this is the name of the stored procedure; any
	/// special characters in the stored procedure name must be quoted or escaped.</remarks>
	[AllowNull]
	public override string CommandText
	{
		get;
		set
		{
			if (Connection?.ActiveCommandId == m_commandId)
				throw new InvalidOperationException("Cannot set MySqlCommand.CommandText when there is an open DataReader for this command; it must be closed first.");
			field = value ?? "";
		}
	}

	public bool IsPrepared => ((IMySqlCommand) this).TryGetPreparedStatements() is not null;

	public new MySqlTransaction? Transaction { get; set; }

	public new MySqlConnection? Connection
	{
		get;
		set
		{
			if (field?.ActiveCommandId == m_commandId)
				throw new InvalidOperationException("Cannot set MySqlCommand.Connection when there is an open DataReader for this command; it must be closed first.");
			field = value;
		}
	}

	/// <inheritdoc/>
	public override int CommandTimeout
	{
		get => Math.Min(m_commandTimeout ?? Connection?.DefaultCommandTimeout ?? 0, int.MaxValue / 1000);
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			m_commandTimeout = value;
			((ICancellableCommand) this).EffectiveCommandTimeout = null;
		}
	}

	/// <inheritdoc/>
	public override CommandType CommandType
	{
		get;
		set
		{
			if (value is not CommandType.Text and not CommandType.StoredProcedure)
				throw new ArgumentException("CommandType must be Text or StoredProcedure.", nameof(value));
			field = value;
		}
	}

	/// <inheritdoc/>
	public override bool DesignTimeVisible { get; set; }

	/// <inheritdoc/>
	public override UpdateRowSource UpdatedRowSource { get; set; }

	/// <summary>
	/// Holds the first automatically-generated ID for a value inserted in an <c>AUTO_INCREMENT</c> column in the last statement.
	/// </summary>
	/// <remarks>
	/// See <a href="https://dev.mysql.com/doc/refman/8.0/en/information-functions.html#function_last-insert-id"><c>LAST_INSERT_ID()</c></a> for more information.
	/// </remarks>
	public long LastInsertedId { get; private set; }

	void IMySqlCommand.SetLastInsertedId(long lastInsertedId) => LastInsertedId = lastInsertedId;

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

	protected override DbParameter CreateDbParameter() => new MySqlParameter();

#pragma warning disable CA2012 // OK to read .Result because the ValueTask is completed
	protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) =>
		ExecuteReaderAsync(behavior, IOBehavior.Synchronous, CancellationToken.None).Result;
#pragma warning restore CA2012

	/// <summary>
	/// Executes this command asynchronously on the associated <see cref="MySqlConnection"/>.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	/// <remarks>For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command.
	/// For stored procedures, the return value is the number of rows affected by the last statement in the stored procedure,
	/// or zero if the last statement is a SELECT. For all other types of statements, the return value is -1.</remarks>
	public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) =>
		ExecuteNonQueryAsync(AsyncIOBehavior, cancellationToken).AsTask();

	internal async ValueTask<int> ExecuteNonQueryAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Volatile.Write(ref m_commandTimedOut, false);
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		using var reader = await ExecuteReaderNoResetTimeoutAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
		do
		{
			while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
			{
			}
		} while (await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false));
		return reader.RecordsAffected;
	}

	public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) =>
		ExecuteScalarAsync(AsyncIOBehavior, cancellationToken).AsTask();

	internal async ValueTask<object?> ExecuteScalarAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Volatile.Write(ref m_commandTimedOut, false);
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		var hasSetResult = false;
		object? result = null;
		using var reader = await ExecuteReaderNoResetTimeoutAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
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

	public new Task<MySqlDataReader> ExecuteReaderAsync(CancellationToken cancellationToken = default) =>
		ExecuteReaderAsync(default, AsyncIOBehavior, cancellationToken).AsTask();

	public new Task<MySqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken = default) =>
		ExecuteReaderAsync(behavior, AsyncIOBehavior, cancellationToken).AsTask();

	protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) =>
		await ExecuteReaderAsync(behavior, AsyncIOBehavior, cancellationToken).ConfigureAwait(false);

	internal async ValueTask<MySqlDataReader> ExecuteReaderAsync(CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		Volatile.Write(ref m_commandTimedOut, false);
		this.ResetCommandTimeout();
		using var registration = ((ICancellableCommand) this).RegisterCancel(cancellationToken);
		return await ExecuteReaderNoResetTimeoutAsync(behavior, ioBehavior, cancellationToken).ConfigureAwait(false);
	}

	internal ValueTask<MySqlDataReader> ExecuteReaderNoResetTimeoutAsync(CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		if (!IsValid(out var exception))
			return ValueTaskExtensions.FromException<MySqlDataReader>(exception);

		var activity = NoActivity ? null : Connection!.Session.StartActivity(ActivitySourceHelper.ExecuteActivityName,
			ActivitySourceHelper.DatabaseStatementTagName, CommandText);
		m_commandBehavior = behavior;
		return CommandExecutor.ExecuteReaderAsync(new(this), SingleCommandPayloadCreator.Instance, behavior, activity, ioBehavior, cancellationToken);
	}

	public MySqlCommand Clone() => new(this);

	object ICloneable.Clone() => Clone();

	protected override void Dispose(bool disposing)
	{
		m_isDisposed = true;
		base.Dispose(disposing);
	}

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public override ValueTask DisposeAsync()
#else
	public Task DisposeAsync()
#endif
	{
		Dispose();
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		return default;
#else
		return Task.CompletedTask;
#endif
	}

	/// <summary>
	/// Registers <see cref="Cancel"/> as a callback with <paramref name="cancellationToken"/> if cancellation is supported.
	/// </summary>
	/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
	/// <returns>An object that must be disposed to revoke the cancellation registration.</returns>
	/// <remarks>This method is more efficient than calling <code>token.Register(Command.Cancel)</code> because it avoids
	/// unnecessary allocations.</remarks>
	CancellationTokenRegistration ICancellableCommand.RegisterCancel(CancellationToken cancellationToken)
	{
		if (!cancellationToken.CanBeCanceled)
			return default;

		m_cancelAction ??= Cancel;
		return cancellationToken.Register(m_cancelAction);
	}

	void ICancellableCommand.SetTimeout(int milliseconds)
	{
		if (m_cancelTimerId != 0)
			TimerQueue.Instance.Remove(m_cancelTimerId);

		if (milliseconds != Constants.InfiniteTimeout)
		{
			m_cancelForCommandTimeoutAction ??= CancelCommandForTimeout;
			m_cancelTimerId = TimerQueue.Instance.Add(milliseconds, m_cancelForCommandTimeoutAction);
		}
	}

	bool ICancellableCommand.IsTimedOut => Volatile.Read(ref m_commandTimedOut);

	int ICancellableCommand.CommandId => m_commandId;

	int? ICancellableCommand.EffectiveCommandTimeout { get; set; }

	int ICancellableCommand.CancelAttemptCount { get; set; }

	ICancellableCommand IMySqlCommand.CancellableCommand => this;

	private IOBehavior AsyncIOBehavior => Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous;

	private void CancelCommandForTimeout()
	{
		Volatile.Write(ref m_commandTimedOut, true);
		Connection?.Cancel(this, m_commandId, false);
	}

	private bool IsValid([NotNullWhen(false)] out Exception? exception)
	{
		exception = null;
		if (m_isDisposed)
			exception = new ObjectDisposedException(GetType().Name);
		else if (Connection is null)
			exception = new InvalidOperationException("Connection property must be non-null.");
		else if (Connection.State is not ConnectionState.Open and not ConnectionState.Connecting)
			exception = new InvalidOperationException($"Connection must be Open; current state is {Connection.State}");
		else if (!Connection.IgnoreCommandTransaction && Transaction != Connection.CurrentTransaction)
			exception = new InvalidOperationException("The transaction associated with this command is not the connection's active transaction; see https://mysqlconnector.net/trans");
		else if (string.IsNullOrWhiteSpace(CommandText))
			exception = new InvalidOperationException("CommandText must be specified");
		return exception is null;
	}

	PreparedStatements? IMySqlCommand.TryGetPreparedStatements() => CommandType == CommandType.Text && !string.IsNullOrWhiteSpace(CommandText) && Connection is not null &&
		Connection.State == ConnectionState.Open ? Connection.Session.TryGetPreparedStatement(CommandText!) : null;

	CommandBehavior IMySqlCommand.CommandBehavior => m_commandBehavior;
	MySqlParameterCollection? IMySqlCommand.OutParameters { get; set; }
	MySqlParameter? IMySqlCommand.ReturnParameter { get; set; }
	ILogger IMySqlCommand.Logger => Connection!.LoggingConfiguration.CommandLogger;

	private readonly int m_commandId;
	private bool m_isDisposed;
	private MySqlParameterCollection? m_parameterCollection;
	private MySqlAttributeCollection? m_attributeCollection;
	private int? m_commandTimeout;
	private CommandBehavior m_commandBehavior;
	private Action? m_cancelAction;
	private Action? m_cancelForCommandTimeoutAction;
	private uint m_cancelTimerId;
	private bool m_commandTimedOut;
}
