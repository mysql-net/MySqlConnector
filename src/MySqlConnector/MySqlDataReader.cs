using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using MySqlConnector.Core;
using MySqlConnector.Logging;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector;

#pragma warning disable CA1010 // Generic interface should also be implemented

#if NET462
public sealed class MySqlDataReader : DbDataReader
#else
public sealed class MySqlDataReader : DbDataReader, IDbColumnSchemaGenerator
#endif
{
	public override bool NextResult()
	{
		Command?.CancellableCommand.ResetCommandTimeout();
		return NextResultAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
	}

	public override bool Read()
	{
		VerifyNotDisposed();
		Command!.CancellableCommand.ResetCommandTimeout();
		return m_resultSet.Read();
	}

	public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
	{
		VerifyNotDisposed();
		Command!.CancellableCommand.ResetCommandTimeout();
		using var registration = Command.CancellableCommand.RegisterCancel(cancellationToken);
		return await m_resultSet.ReadAsync(cancellationToken).ConfigureAwait(false);
	}

	internal Task<bool> ReadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) =>
		m_resultSet.ReadAsync(ioBehavior, cancellationToken);

	public override async Task<bool> NextResultAsync(CancellationToken cancellationToken)
	{
		VerifyNotDisposed();
		Command!.CancellableCommand.ResetCommandTimeout();
		using var registration = Command.CancellableCommand.RegisterCancel(cancellationToken);
		return await NextResultAsync(Command?.Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken).ConfigureAwait(false);
	}

	internal async Task<bool> NextResultAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		VerifyNotDisposed();
		try
		{
			do
			{
				while (true)
				{
					await m_resultSet.ReadEntireAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
					await ScanResultSetAsync(ioBehavior, m_resultSet, cancellationToken).ConfigureAwait(false);
					if (m_hasMoreResults && m_resultSet.ContainsCommandParameters)
						await ReadOutParametersAsync(Command!, m_resultSet, ioBehavior, cancellationToken).ConfigureAwait(false);
					else
						break;
				}

				if (!m_hasMoreResults)
				{
					if (m_commandListPosition.CommandIndex < m_commandListPosition.CommandCount)
					{
						Command = m_commandListPosition.CommandAt(m_commandListPosition.CommandIndex);
						using (Command.CancellableCommand.RegisterCancel(cancellationToken))
						{
							await m_payloadCreator!.SendCommandPrologueAsync(Command.Connection!, m_commandListPosition, ioBehavior, cancellationToken).ConfigureAwait(false);

							var writer = new ByteBufferWriter();
							if (!Command.Connection!.Session.IsCancelingQuery && m_payloadCreator.WriteQueryCommand(ref m_commandListPosition, m_cachedProcedures!, writer, false))
							{
								using var payload = writer.ToPayloadData();
								await Command.Connection.Session.SendAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
								await m_resultSet.ReadResultSetHeaderAsync(ioBehavior).ConfigureAwait(false);
								ActivateResultSet(cancellationToken);
								m_hasMoreResults = true;
							}
						}
					}
				}
				else
				{
					ActivateResultSet(cancellationToken);
				}
			}
			while (m_hasMoreResults && (Command!.CommandBehavior & (CommandBehavior.SingleResult | CommandBehavior.SingleRow)) != 0);

			if (!m_hasMoreResults)
				m_resultSet.Reset();
			m_schemaTable = null;
			return m_hasMoreResults;
		}
		catch (MySqlException)
		{
			m_resultSet.Reset();
			m_hasMoreResults = false;
			m_schemaTable = null;
			throw;
		}
	}

	private void ActivateResultSet(CancellationToken cancellationToken)
	{
		if (m_resultSet.ReadResultSetHeaderException is not null)
		{
			var mySqlException = m_resultSet.ReadResultSetHeaderException.SourceException as MySqlException;

			// for any exception not created from an ErrorPayload, mark the session as failed (because we can't guarantee that all data
			// has been read from the connection and that the socket is still usable)
			if (mySqlException?.SqlState is null)
				Command!.Connection!.SetSessionFailed(m_resultSet.ReadResultSetHeaderException.SourceException);

			if (mySqlException?.ErrorCode == MySqlErrorCode.QueryInterrupted && cancellationToken.IsCancellationRequested)
				throw new OperationCanceledException(mySqlException.Message, mySqlException, cancellationToken);

			if (mySqlException?.ErrorCode == MySqlErrorCode.QueryInterrupted && Command!.CancellableCommand.IsTimedOut)
				throw MySqlException.CreateForTimeout(mySqlException);

			if (mySqlException is not null)
			{
				ServerSession.ThrowIfStatementContainsDelimiter(mySqlException, Command!);

				m_resultSet.ReadResultSetHeaderException.Throw();
			}

			throw new MySqlException("Failed to read the result set.", m_resultSet.ReadResultSetHeaderException.SourceException);
		}

		m_hasWarnings = m_resultSet.WarningCount != 0;
	}

	private async ValueTask ScanResultSetAsync(IOBehavior ioBehavior, ResultSet resultSet, CancellationToken cancellationToken)
	{
		if (!m_hasMoreResults)
			return;

		if (resultSet.BufferState is ResultSetState.NoMoreData or ResultSetState.None)
		{
			m_hasMoreResults = false;
			return;
		}

		if (resultSet.BufferState != ResultSetState.HasMoreData)
			throw new InvalidOperationException($"Invalid state: {resultSet.BufferState}");

		using (Command!.CancellableCommand.RegisterCancel(cancellationToken))
		{
			try
			{
				await resultSet.ReadResultSetHeaderAsync(ioBehavior).ConfigureAwait(false);
				m_hasMoreResults = resultSet.BufferState != ResultSetState.NoMoreData;
			}
			catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.QueryInterrupted)
			{
				m_hasMoreResults = false;
				cancellationToken.ThrowIfCancellationRequested();
				throw;
			}
		}
	}

	public override string GetName(int ordinal) => GetResultSet().GetName(ordinal);

	public override int GetValues(object[] values) => GetResultSet().GetCurrentRow().GetValues(values);

	public override bool IsDBNull(int ordinal) => GetResultSet().GetCurrentRow().IsDBNull(ordinal);

	public override int FieldCount
	{
		get
		{
			VerifyNotDisposed();
			return m_resultSet is null ? throw new InvalidOperationException("There is no current result set.") :
				m_resultSet.ContainsCommandParameters ? 0 :
				m_resultSet.FieldCount;
		}
	}

	public override object this[int ordinal] => GetResultSet().GetCurrentRow()[ordinal];

	public override object this[string name] => GetResultSet().GetCurrentRow()[name];

	public override bool HasRows
	{
		get
		{
			VerifyNotDisposed();
			return m_resultSet is null ? throw new InvalidOperationException("There is no current result set.") :
				!m_resultSet.ContainsCommandParameters && m_resultSet.HasRows;
		}
	}

	public override bool IsClosed => Command is null;

	/// <summary>
	/// Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.
	/// </summary>
	/// <remarks>For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command.
	/// For stored procedures, the return value is the number of rows affected by the last statement in the stored procedure,
	/// or zero if the last statement is a SELECT. For all other types of statements, the return value is -1.</remarks>
	public override int RecordsAffected => RealRecordsAffected is ulong recordsAffected ? checked((int) recordsAffected) : -1;

	public override int GetOrdinal(string name) => GetResultSet().GetOrdinal(name);

	public override bool GetBoolean(int ordinal) => GetResultSet().GetCurrentRow().GetBoolean(ordinal);
	public bool GetBoolean(string name) => GetBoolean(GetOrdinal(name));

	public override byte GetByte(int ordinal) => GetResultSet().GetCurrentRow().GetByte(ordinal);
	public byte GetByte(string name) => GetByte(GetOrdinal(name));

	public sbyte GetSByte(int ordinal) => GetResultSet().GetCurrentRow().GetSByte(ordinal);
	public sbyte GetSByte(string name) => GetSByte(GetOrdinal(name));

	public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
		=> GetResultSet().GetCurrentRow().GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

	public long GetBytes(string name, long dataOffset, byte[]? buffer, int bufferOffset, int length)
		=> GetResultSet().GetCurrentRow().GetBytes(GetOrdinal(name), dataOffset, buffer, bufferOffset, length);

	public override char GetChar(int ordinal) => GetResultSet().GetCurrentRow().GetChar(ordinal);
	public char GetChar(string name) => GetChar(GetOrdinal(name));

	public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
		=> GetResultSet().GetCurrentRow().GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

	public override Guid GetGuid(int ordinal) => GetResultSet().GetCurrentRow().GetGuid(ordinal);
	public Guid GetGuid(string name) => GetGuid(GetOrdinal(name));

	public override short GetInt16(int ordinal) => GetResultSet().GetCurrentRow().GetInt16(ordinal);
	public short GetInt16(string name) => GetInt16(GetOrdinal(name));

	public override int GetInt32(int ordinal) => GetResultSet().GetCurrentRow().GetInt32(ordinal);
	public int GetInt32(string name) => GetInt32(GetOrdinal(name));

	public override long GetInt64(int ordinal) => GetResultSet().GetCurrentRow().GetInt64(ordinal);
	public long GetInt64(string name) => GetInt64(GetOrdinal(name));

	public override string GetDataTypeName(int ordinal) => GetResultSet().GetDataTypeName(ordinal);

	public Type GetFieldType(string name) => GetFieldType(GetOrdinal(name));

#if NET6_0_OR_GREATER
	[UnconditionalSuppressMessage("ILLink", "IL2093", Justification = "This method is provided to implement the DbDataReader API. We do not want to retain all public methods of Types just used as sentinel values for field mapping.")]
#endif
	public override Type GetFieldType(int ordinal) => GetResultSet().GetFieldType(ordinal);

	public override object GetValue(int ordinal) => GetResultSet().GetCurrentRow().GetValue(ordinal);

	public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

	public override int Depth => GetResultSet().Depth;

	protected override DbDataReader GetDbDataReader(int ordinal) => throw new NotSupportedException();

#if NET6_0_OR_GREATER
	public DateOnly GetDateOnly(int ordinal) => DateOnly.FromDateTime(GetDateTime(ordinal));
	public DateOnly GetDateOnly(string name) => GetDateOnly(GetOrdinal(name));
#endif

	public override DateTime GetDateTime(int ordinal) => GetResultSet().GetCurrentRow().GetDateTime(ordinal);
	public DateTime GetDateTime(string name) => GetDateTime(GetOrdinal(name));

	public DateTimeOffset GetDateTimeOffset(int ordinal) => GetResultSet().GetCurrentRow().GetDateTimeOffset(ordinal);
	public DateTimeOffset GetDateTimeOffset(string name) => GetDateTimeOffset(GetOrdinal(name));

	public MySqlDateTime GetMySqlDateTime(int ordinal) => GetResultSet().GetCurrentRow().GetMySqlDateTime(ordinal);
	public MySqlDateTime GetMySqlDateTime(string name) => GetMySqlDateTime(GetOrdinal(name));

	public MySqlGeometry GetMySqlGeometry(int ordinal) => GetResultSet().GetCurrentRow().GetMySqlGeometry(ordinal);
	public MySqlGeometry GetMySqlGeometry(string name) => GetMySqlGeometry(GetOrdinal(name));

	public MySqlDecimal GetMySqlDecimal(int ordinal) => GetResultSet().GetCurrentRow().GetMySqlDecimal(ordinal);
	public MySqlDecimal GetMySqlDecimal(string name) => GetMySqlDecimal(GetOrdinal(name));

#if NET6_0_OR_GREATER
	public TimeOnly GetTimeOnly(int ordinal) => TimeOnly.FromTimeSpan(GetTimeSpan(ordinal));
	public TimeOnly GetTimeOnly(string name) => GetTimeOnly(GetOrdinal(name));
#endif

	public TimeSpan GetTimeSpan(int ordinal) => (TimeSpan) GetValue(ordinal);
	public TimeSpan GetTimeSpan(string name) => GetTimeSpan(GetOrdinal(name));

	public override Stream GetStream(int ordinal) => GetResultSet().GetCurrentRow().GetStream(ordinal);
	public Stream GetStream(string name) => GetStream(GetOrdinal(name));

	public override TextReader GetTextReader(int ordinal) => new StringReader(GetString(ordinal));
	public TextReader GetTextReader(string name) => new StringReader(GetString(name));

	public override string GetString(int ordinal) => GetResultSet().GetCurrentRow().GetString(ordinal);
	public string GetString(string name) => GetString(GetOrdinal(name));

	public override decimal GetDecimal(int ordinal) => GetResultSet().GetCurrentRow().GetDecimal(ordinal);
	public decimal GetDecimal(string name) => GetDecimal(GetOrdinal(name));

	public override double GetDouble(int ordinal) => GetResultSet().GetCurrentRow().GetDouble(ordinal);
	public double GetDouble(string name) => GetDouble(GetOrdinal(name));

	public override float GetFloat(int ordinal) => GetResultSet().GetCurrentRow().GetFloat(ordinal);
	public float GetFloat(string name) => GetFloat(GetOrdinal(name));

	public ushort GetUInt16(int ordinal) => GetResultSet().GetCurrentRow().GetUInt16(ordinal);
	public ushort GetUInt16(string name) => GetUInt16(GetOrdinal(name));

	public uint GetUInt32(int ordinal) => GetResultSet().GetCurrentRow().GetUInt32(ordinal);
	public uint GetUInt32(string name) => GetUInt32(GetOrdinal(name));

	public ulong GetUInt64(int ordinal) => GetResultSet().GetCurrentRow().GetUInt64(ordinal);
	public ulong GetUInt64(string name) => GetUInt64(GetOrdinal(name));

	public override int VisibleFieldCount => FieldCount;

	/// <summary>
	/// Returns a <see cref="DataTable"/> that contains metadata about the columns in the result set.
	/// </summary>
	/// <returns>A <see cref="DataTable"/> containing metadata about the columns in the result set.</returns>
	public override DataTable? GetSchemaTable() => m_schemaTable ??= BuildSchemaTable();

	/// <summary>
	/// Returns a <see cref="DataTable"/> that contains metadata about the columns in the result set.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A <see cref="DataTable"/> containing metadata about the columns in the result set.</returns>
	/// <remarks>This method runs synchronously; prefer to call <see cref="GetSchemaTable"/> to avoid the overhead of allocating an unnecessary <c>Task</c>.</remarks>
#if NET5_0_OR_GREATER
	public override Task<DataTable?> GetSchemaTableAsync(CancellationToken cancellationToken = default)
#else
	public Task<DataTable?> GetSchemaTableAsync(CancellationToken cancellationToken = default)
#endif
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(GetSchemaTable());
	}

#pragma warning disable CA2012 // Safe because method completes synchronously
	public override void Close() => DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012

	/// <summary>
	/// Returns metadata about the columns in the result set.
	/// </summary>
	/// <returns>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{DbColumn}"/> containing metadata about the result set.</returns>
	public ReadOnlyCollection<DbColumn> GetColumnSchema()
	{
		var hasNoSchema = !m_resultSet.HasResultSet || m_resultSet.ContainsCommandParameters;
		if (hasNoSchema)
			return new ReadOnlyCollection<DbColumn>([]);

		var columnDefinitions = m_resultSet.ColumnDefinitions;
		var resultSet = GetResultSet();
		var schema = new List<DbColumn>(columnDefinitions.Length);
		for (var n = 0; n < columnDefinitions.Length; n++)
			schema.Add(new MySqlDbColumn(n, columnDefinitions[n], Connection!.AllowZeroDateTime, resultSet.GetColumnType(n)));
		return schema.AsReadOnly();
	}

	/// <summary>
	/// Returns metadata about the columns in the result set.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A <see cref="Task"/> containing <see cref="System.Collections.ObjectModel.ReadOnlyCollection{DbColumn}"/> containing metadata about the result set.</returns>
	/// <remarks>This method runs synchronously; prefer to call <see cref="GetColumnSchema"/> to avoid the overhead of allocating an unnecessary <c>Task</c>.</remarks>
#if NET5_0_OR_GREATER
	public override Task<ReadOnlyCollection<DbColumn>> GetColumnSchemaAsync(CancellationToken cancellationToken = default)
#else
	public Task<ReadOnlyCollection<DbColumn>> GetColumnSchemaAsync(CancellationToken cancellationToken = default)
#endif
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(GetColumnSchema());
	}

	public override T GetFieldValue<T>(int ordinal)
	{
		if (typeof(T) == typeof(bool))
			return (T) (object) GetBoolean(ordinal);
		if (typeof(T) == typeof(byte))
			return (T) (object) GetByte(ordinal);
		if (typeof(T) == typeof(sbyte))
			return (T) (object) GetSByte(ordinal);
		if (typeof(T) == typeof(short))
			return (T) (object) GetInt16(ordinal);
		if (typeof(T) == typeof(ushort))
			return (T) (object) GetUInt16(ordinal);
		if (typeof(T) == typeof(int))
			return (T) (object) GetInt32(ordinal);
		if (typeof(T) == typeof(uint))
			return (T) (object) GetUInt32(ordinal);
		if (typeof(T) == typeof(long))
			return (T) (object) GetInt64(ordinal);
		if (typeof(T) == typeof(ulong))
			return (T) (object) GetUInt64(ordinal);
		if (typeof(T) == typeof(char))
			return (T) (object) GetChar(ordinal);
		if (typeof(T) == typeof(decimal))
			return (T) (object) GetDecimal(ordinal);
		if (typeof(T) == typeof(double))
			return (T) (object) GetDouble(ordinal);
		if (typeof(T) == typeof(float))
			return (T) (object) GetFloat(ordinal);
		if (typeof(T) == typeof(string))
			return (T) (object) GetString(ordinal);
		if (typeof(T) == typeof(DateTime))
			return (T) (object) GetDateTime(ordinal);
		if (typeof(T) == typeof(DateTimeOffset))
			return (T) (object) GetDateTimeOffset(ordinal);
		if (typeof(T) == typeof(Guid))
			return (T) (object) GetGuid(ordinal);
		if (typeof(T) == typeof(MySqlGeometry))
			return (T) (object) GetMySqlGeometry(ordinal);
		if (typeof(T) == typeof(Stream))
			return (T) (object) GetStream(ordinal);
		if (typeof(T) == typeof(TextReader) || typeof(T) == typeof(StringReader))
			return (T) (object) GetTextReader(ordinal);
		if (typeof(T) == typeof(TimeSpan))
			return (T) (object) GetTimeSpan(ordinal);
		if (typeof(T) == typeof(MySqlDecimal))
			return (T) (object) GetMySqlDecimal(ordinal);
#if NET6_0_OR_GREATER
		if (typeof(T) == typeof(DateOnly))
			return (T) (object) GetDateOnly(ordinal);
		if (typeof(T) == typeof(TimeOnly))
			return (T) (object) GetTimeOnly(ordinal);
#endif

		return base.GetFieldValue<T>(ordinal);
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
#pragma warning disable CA2012 // Safe because method completes synchronously
			if (disposing)
				DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#pragma warning restore CA2012
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	public override ValueTask DisposeAsync() => DisposeAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, CancellationToken.None);
#else
	public Task DisposeAsync() => DisposeAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, CancellationToken.None);
#endif

	internal Activity? Activity { get; private set; }
	internal IMySqlCommand? Command { get; private set; }
	internal MySqlConnection? Connection => Command?.Connection;
	internal ulong? RealRecordsAffected { get; set; }
	internal ServerSession? Session => Command?.Connection!.Session;

	internal async Task InitAsync(CommandListPosition commandListPosition, ICommandPayloadCreator payloadCreator, IDictionary<string, CachedProcedure?>? cachedProcedures, IMySqlCommand command, CommandBehavior behavior, Activity? activity, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		// reset fields from last use of this MySqlDataReader
		if (m_hasMoreResults)
			throw new InvalidOperationException("Expected m_hasMoreResults to be false");
		if (m_resultSet.BufferState != ResultSetState.None || m_resultSet.State != ResultSetState.None)
			throw new InvalidOperationException("Expected BufferState and State to be ResultSetState.None.");
		m_closed = false;
		m_hasWarnings = false;
		RealRecordsAffected = null;

		// initialize for new command
		m_commandListPosition = commandListPosition;
		m_payloadCreator = payloadCreator;
		m_cachedProcedures = cachedProcedures;
		Command = command;
		m_behavior = behavior;
		Activity = activity;

		command.Connection!.SetActiveReader(this);

		try
		{
			await m_resultSet.ReadResultSetHeaderAsync(ioBehavior).ConfigureAwait(false);
			ActivateResultSet(cancellationToken);
			m_hasMoreResults = true;

			if (m_resultSet.ContainsCommandParameters)
				await ReadOutParametersAsync(command, m_resultSet, ioBehavior, cancellationToken).ConfigureAwait(false);

			// if the command list has multiple commands, keep reading until a result set is found
			while (m_resultSet.State == ResultSetState.NoMoreData && commandListPosition.CommandIndex < commandListPosition.CommandCount)
			{
				_ = await NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			if (activity is { IsAllDataRequested: true })
			{
				activity.SetException(ex);
				activity.Stop();
			}
			Dispose();
			throw;
		}
	}

#if NET6_0_OR_GREATER
	[UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "typeof(Type).TypeInitializer is not used.")]
#endif
	internal DataTable? BuildSchemaTable()
	{
		if (!m_resultSet.HasResultSet || m_resultSet.ContainsCommandParameters)
			return null;

		var schemaTable = new DataTable("SchemaTable")
		{
			Locale = CultureInfo.InvariantCulture,
			MinimumCapacity = m_resultSet.ColumnDefinitions.Length,
		};

		var columnName = new DataColumn(SchemaTableColumn.ColumnName, typeof(string));
		var ordinal = new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof(int));
		var size = new DataColumn(SchemaTableColumn.ColumnSize, typeof(int));
		var precision = new DataColumn(SchemaTableColumn.NumericPrecision, typeof(int));
		var scale = new DataColumn(SchemaTableColumn.NumericScale, typeof(int));
		var dataType = new DataColumn(SchemaTableColumn.DataType, typeof(System.Type));
		var providerType = new DataColumn(SchemaTableColumn.ProviderType, typeof(int));
		var isLong = new DataColumn(SchemaTableColumn.IsLong, typeof(bool));
		var allowDBNull = new DataColumn(SchemaTableColumn.AllowDBNull, typeof(bool));
		var isReadOnly = new DataColumn(SchemaTableOptionalColumn.IsReadOnly, typeof(bool));
		var isRowVersion = new DataColumn(SchemaTableOptionalColumn.IsRowVersion, typeof(bool));
		var isUnique = new DataColumn(SchemaTableColumn.IsUnique, typeof(bool));
		var isKey = new DataColumn(SchemaTableColumn.IsKey, typeof(bool));
		var isAutoIncrement = new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool));
		var isHidden = new DataColumn(SchemaTableOptionalColumn.IsHidden, typeof(bool));
		var baseCatalogName = new DataColumn(SchemaTableOptionalColumn.BaseCatalogName, typeof(string));
		var baseSchemaName = new DataColumn(SchemaTableColumn.BaseSchemaName, typeof(string));
		var baseTableName = new DataColumn(SchemaTableColumn.BaseTableName, typeof(string));
		var baseColumnName = new DataColumn(SchemaTableColumn.BaseColumnName, typeof(string));
		var isAliased = new DataColumn(SchemaTableColumn.IsAliased, typeof(bool));
		var isExpression = new DataColumn(SchemaTableColumn.IsExpression, typeof(bool));
		var isIdentity = new DataColumn("IsIdentity", typeof(bool));
		ordinal.DefaultValue = 0;
		precision.DefaultValue = 0;
		scale.DefaultValue = 0;
		isLong.DefaultValue = false;

		// must maintain order for backward compatibility
		var columns = schemaTable.Columns;
		columns.Add(columnName);
		columns.Add(ordinal);
		columns.Add(size);
		columns.Add(precision);
		columns.Add(scale);
		columns.Add(isUnique);
		columns.Add(isKey);
		columns.Add(baseCatalogName);
		columns.Add(baseColumnName);
		columns.Add(baseSchemaName);
		columns.Add(baseTableName);
		columns.Add(dataType);
		columns.Add(allowDBNull);
		columns.Add(providerType);
		columns.Add(isAliased);
		columns.Add(isExpression);
		columns.Add(isIdentity);
		columns.Add(isAutoIncrement);
		columns.Add(isRowVersion);
		columns.Add(isHidden);
		columns.Add(isLong);
		columns.Add(isReadOnly);

		foreach (MySqlDbColumn column in GetColumnSchema())
		{
			var schemaRow = schemaTable.NewRow();
			schemaRow[columnName] = column.ColumnName;
			schemaRow[ordinal] = column.ColumnOrdinal;
			schemaRow[dataType] = column.DataType;
			schemaRow[size] = column.ColumnSize;
			schemaRow[providerType] = column.ProviderType;
			schemaRow[isLong] = column.IsLong;
			schemaRow[isUnique] = false;
			schemaRow[isKey] = column.IsKey;
			schemaRow[allowDBNull] = column.AllowDBNull;
			schemaRow[scale] = column.NumericScale;
			schemaRow[precision] = column.NumericPrecision.GetValueOrDefault();

			schemaRow[baseCatalogName] = column.BaseCatalogName;
			schemaRow[baseColumnName] = column.BaseColumnName;
			schemaRow[baseSchemaName] = column.BaseSchemaName;
			schemaRow[baseTableName] = column.BaseTableName;
			schemaRow[isAutoIncrement] = column.IsAutoIncrement;
			schemaRow[isRowVersion] = false;
			schemaRow[isReadOnly] = column.IsReadOnly;

			schemaTable.Rows.Add(schemaRow);
			schemaRow.AcceptChanges();
		}

		return schemaTable;
	}

	internal MySqlDataReader()
	{
		m_resultSet = new(this);
	}

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
	internal async ValueTask DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#else
	internal async Task DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#endif
	{
		if (!m_closed)
		{
			m_closed = true;

			if (m_resultSet is not null && Command!.Connection!.State == ConnectionState.Open)
			{
				Command.Connection.Session.SetTimeout(Constants.InfiniteTimeout);
				try
				{
					while (await NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
					{
					}
				}
				catch (MySqlException ex)
				{
					// ignore "Query execution was interrupted" exceptions when closing a data reader; log other exceptions
					if (ex.ErrorCode != MySqlErrorCode.QueryInterrupted)
						Log.IgnoringExceptionInDisposeAsync(Command.Logger, ex, Command.Connection.Session.Id, ex.Message, Command.CommandText!);
				}
			}

			m_hasMoreResults = false;

			var connection = Command!.Connection!;
			Command.CancellableCommand.SetTimeout(Constants.InfiniteTimeout);
			connection.FinishQuerying(m_hasWarnings);

			Activity?.Stop();
			Activity = null;

			if ((m_behavior & CommandBehavior.CloseConnection) != 0)
				await connection.CloseAsync(ioBehavior).ConfigureAwait(false);

			// clear fields (so that MySqlConnection can be GCed if the user doesn't hold a reference to it)
			Command = null;
			m_commandListPosition = default;
			m_payloadCreator = null;
			m_cachedProcedures = null;
		}
	}

	// If ResultSet.ContainsCommandParameters is true, then this method should be called to read the (single)
	// row in that result set, which contains the values of "out" parameters from the previous stored procedure
	// execution. These values will be stored in the parameters of the associated command.
	private static async Task ReadOutParametersAsync(IMySqlCommand command, ResultSet resultSet, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		_ = await resultSet.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

		var row = resultSet.GetCurrentRow();
		if (row.GetString(0) != SingleCommandPayloadCreator.OutParameterSentinelColumnName)
			throw new InvalidOperationException("Expected out parameter values.");

		for (var i = 0; i < command.OutParameters!.Count; i++)
		{
			var param = command.OutParameters[i];
			var columnIndex = i + 1;
			if (param.HasSetDbType && !row.IsDBNull(columnIndex))
			{
				var dbTypeMapping = TypeMapper.Instance.GetDbTypeMapping(param.DbType);
				if (dbTypeMapping is not null && param.DbType is not DbType.Object)
				{
					param.Value = dbTypeMapping.DoConversion(row.GetValue(columnIndex));
					continue;
				}
			}
			param.Value = row.GetValue(columnIndex);
		}

		if (await resultSet.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
			throw new InvalidOperationException("Expected only one row.");
	}

	private void VerifyNotDisposed()
	{
		if (Command is null)
			throw new InvalidOperationException("Can't call this method when MySqlDataReader is closed.");
	}

	internal PreparedStatement? LastUsedPreparedStatement => m_commandListPosition.LastUsedPreparedStatement;

	private ResultSet GetResultSet()
	{
		VerifyNotDisposed();
		return m_resultSet is null || m_resultSet.ContainsCommandParameters ?
			throw new InvalidOperationException("There is no current result set.") :
			m_resultSet;
	}

	private readonly ResultSet m_resultSet;
	private CommandBehavior m_behavior;
	private ICommandPayloadCreator? m_payloadCreator;
	private IDictionary<string, CachedProcedure?>? m_cachedProcedures;
	private CommandListPosition m_commandListPosition;
	private bool m_closed;
	private bool m_hasWarnings;
	private bool m_hasMoreResults;
	private DataTable? m_schemaTable;
}
