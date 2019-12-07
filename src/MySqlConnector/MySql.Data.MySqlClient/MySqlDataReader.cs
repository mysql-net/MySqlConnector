using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Types;
using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlDataReader : DbDataReader
#if !NET45 && !NET461
		, IDbColumnSchemaGenerator
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
			return m_resultSet!.Read();
		}

		public override Task<bool> ReadAsync(CancellationToken cancellationToken)
		{
			VerifyNotDisposed();
			Command!.CancellableCommand.ResetCommandTimeout();
			return m_resultSet!.ReadAsync(cancellationToken);
		}

		internal Task<bool> ReadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) =>
			m_resultSet!.ReadAsync(ioBehavior, cancellationToken);

		public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
		{
			Command?.CancellableCommand.ResetCommandTimeout();
			return NextResultAsync(Command?.Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, cancellationToken);
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
						await m_resultSet!.ReadEntireAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
						await ScanResultSetAsync(ioBehavior, m_resultSet, cancellationToken).ConfigureAwait(false);
						if (m_hasMoreResults && m_resultSet.ContainsCommandParameters)
							await ReadOutParametersAsync(Command!, m_resultSet, ioBehavior, cancellationToken).ConfigureAwait(false);
						else
							break;
					}

					if (!m_hasMoreResults)
					{
						if (m_commandListPosition.CommandIndex < m_commandListPosition.Commands.Count)
						{
							Command = m_commandListPosition.Commands[m_commandListPosition.CommandIndex];
							using (Command.CancellableCommand.RegisterCancel(cancellationToken))
							{
								var writer = new ByteBufferWriter();
								if (!Command.Connection!.Session.IsCancelingQuery && m_payloadCreator.WriteQueryCommand(ref m_commandListPosition, m_cachedProcedures!, writer))
								{
									using var payload = writer.ToPayloadData();
									await Command.Connection.Session.SendAsync(payload, ioBehavior, cancellationToken).ConfigureAwait(false);
									await m_resultSet.ReadResultSetHeaderAsync(ioBehavior).ConfigureAwait(false);
									ActivateResultSet();
									m_hasMoreResults = true;
								}
							}
						}
					}
					else
					{
						ActivateResultSet();
					}
				}
				while (m_hasMoreResults && (Command!.CommandBehavior & (CommandBehavior.SingleResult | CommandBehavior.SingleRow)) != 0);

				if (!m_hasMoreResults)
					m_resultSet.Reset();
#if !NETSTANDARD1_3
				m_schemaTable = null;
#endif
				return m_hasMoreResults;
			}
			catch (MySqlException)
			{
				m_resultSet!.Reset();
				m_hasMoreResults = false;
#if !NETSTANDARD1_3
				m_schemaTable = null;
#endif
				throw;
			}
		}

		private void ActivateResultSet()
		{
			if (m_resultSet!.ReadResultSetHeaderException is object)
			{
				var mySqlException = m_resultSet.ReadResultSetHeaderException as MySqlException;

				// for any exception not created from an ErrorPayload, mark the session as failed (because we can't guarantee that all data
				// has been read from the connection and that the socket is still usable)
				if (mySqlException?.SqlState is null)
					Command!.Connection!.SetSessionFailed(m_resultSet.ReadResultSetHeaderException);

				throw mySqlException is object ?
					new MySqlException(mySqlException.Number, mySqlException.SqlState, mySqlException.Message, mySqlException) :
					new MySqlException("Failed to read the result set.", m_resultSet.ReadResultSetHeaderException);
			}

			Command!.SetLastInsertedId(m_resultSet.LastInsertId);
			m_recordsAffected = m_recordsAffected is null ? m_resultSet.RecordsAffected : m_recordsAffected.Value + (m_resultSet.RecordsAffected ?? 0);
			m_hasWarnings = m_resultSet.WarningCount != 0;
		}

		private ValueTask<int> ScanResultSetAsync(IOBehavior ioBehavior, ResultSet resultSet, CancellationToken cancellationToken)
		{
			if (!m_hasMoreResults)
				return default;

			if (resultSet.BufferState == ResultSetState.NoMoreData || resultSet.BufferState == ResultSetState.None)
			{
				m_hasMoreResults = false;
				return default;
			}

			if (resultSet.BufferState != ResultSetState.HasMoreData)
				throw new InvalidOperationException("Invalid state: {0}".FormatInvariant(resultSet.BufferState));

			return new ValueTask<int>(ScanResultSetAsyncAwaited(ioBehavior, resultSet, cancellationToken));
		}

		private async Task<int> ScanResultSetAsyncAwaited(IOBehavior ioBehavior, ResultSet resultSet, CancellationToken cancellationToken)
		{
			using (Command!.CancellableCommand.RegisterCancel(cancellationToken))
			{
				try
				{
					await resultSet.ReadResultSetHeaderAsync(ioBehavior).ConfigureAwait(false);
					m_hasMoreResults = resultSet.BufferState != ResultSetState.NoMoreData;
					return 0;
				}
				catch (MySqlException ex) when (ex.Number == (int) MySqlErrorCode.QueryInterrupted)
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
				if (m_resultSet is null)
					throw new InvalidOperationException("There is no current result set.");
				return m_resultSet.ContainsCommandParameters ? 0 : m_resultSet.FieldCount;
			}
		}

		public override object this[int ordinal] => GetResultSet().GetCurrentRow()[ordinal];

		public override object this[string name] => GetResultSet().GetCurrentRow()[name];

		public override bool HasRows
		{
			get
			{
				VerifyNotDisposed();
				if (m_resultSet is null)
					throw new InvalidOperationException("There is no current result set.");
				return !m_resultSet.ContainsCommandParameters && m_resultSet.HasRows;
			}
		}

		public override bool IsClosed => Command is null;
		public override int RecordsAffected => m_recordsAffected.GetValueOrDefault(-1);

		public override int GetOrdinal(string name) => GetResultSet().GetOrdinal(name);

		public override bool GetBoolean(int ordinal) => GetResultSet().GetCurrentRow().GetBoolean(ordinal);
		public bool GetBoolean(string name) => GetBoolean(GetOrdinal(name));

		public override byte GetByte(int ordinal) => GetResultSet().GetCurrentRow().GetByte(ordinal);
		public byte GetByte(string name) => GetByte(GetOrdinal(name));

		public sbyte GetSByte(int ordinal) => GetResultSet().GetCurrentRow().GetSByte(ordinal);
		public sbyte GetSByte(string name) => GetSByte(GetOrdinal(name));

		public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
			=> GetResultSet().GetCurrentRow().GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

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
		public override Type GetFieldType(int ordinal) => GetResultSet().GetFieldType(ordinal);

		public override object GetValue(int ordinal) => GetResultSet().GetCurrentRow().GetValue(ordinal);

		public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

		public override int Depth => GetResultSet().Depth;

		protected override DbDataReader? GetDbDataReader(int ordinal) => throw new NotSupportedException();

		public override DateTime GetDateTime(int ordinal) => GetResultSet().GetCurrentRow().GetDateTime(ordinal);
		public DateTime GetDateTime(string name) => GetDateTime(GetOrdinal(name));

		public DateTimeOffset GetDateTimeOffset(int ordinal) => GetResultSet().GetCurrentRow().GetDateTimeOffset(ordinal);
		public DateTimeOffset GetDateTimeOffset(string name) => GetDateTimeOffset(GetOrdinal(name));

		public MySqlDateTime GetMySqlDateTime(int ordinal) => GetResultSet().GetCurrentRow().GetMySqlDateTime(ordinal);
		public MySqlDateTime GetMySqlDateTime(string name) => GetMySqlDateTime(GetOrdinal(name));

		public MySqlGeometry GetMySqlGeometry(int ordinal) => GetResultSet().GetCurrentRow().GetMySqlGeometry(ordinal);
		public MySqlGeometry GetMySqlGeometry(string name) => GetMySqlGeometry(GetOrdinal(name));

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

#if !NETSTANDARD1_3
		public override DataTable GetSchemaTable() => m_schemaTable ??= BuildSchemaTable();

		public override void Close() => DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
#endif

		public ReadOnlyCollection<DbColumn> GetColumnSchema()
		{
			var columnDefinitions = m_resultSet?.ColumnDefinitions;
			var hasNoSchema = columnDefinitions is null || m_resultSet!.ContainsCommandParameters;
			return hasNoSchema ? new List<DbColumn>().AsReadOnly() :
				columnDefinitions!
					.Select((c, n) => (DbColumn) new MySqlDbColumn(n, c, Connection!.AllowZeroDateTime, GetResultSet().ColumnTypes![n]))
					.ToList().AsReadOnly();
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

			return base.GetFieldValue<T>(ordinal);
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
					DisposeAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

#if !NETSTANDARD2_1 && !NETCOREAPP3_0
		public Task DisposeAsync() => DisposeAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, CancellationToken.None);
#else
		public override ValueTask DisposeAsync() => DisposeAsync(Connection?.AsyncIOBehavior ?? IOBehavior.Asynchronous, CancellationToken.None);
#endif

		internal IMySqlCommand? Command { get; private set; }
		internal MySqlConnection? Connection => Command?.Connection;
		internal ServerSession? Session => Command?.Connection!.Session;

		internal static async Task<MySqlDataReader> CreateAsync(CommandListPosition commandListPosition, ICommandPayloadCreator payloadCreator, IDictionary<string, CachedProcedure?>? cachedProcedures, IMySqlCommand command, CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			var dataReader = new MySqlDataReader(commandListPosition, payloadCreator, cachedProcedures, command, behavior);
			command.Connection!.SetActiveReader(dataReader);

			try
			{
				await dataReader.m_resultSet!.ReadResultSetHeaderAsync(ioBehavior).ConfigureAwait(false);
				dataReader.ActivateResultSet();
				dataReader.m_hasMoreResults = true;

				if (dataReader.m_resultSet.ContainsCommandParameters)
					await ReadOutParametersAsync(dataReader.Command!, dataReader.m_resultSet, ioBehavior, cancellationToken).ConfigureAwait(false);

				// if the command list has multiple commands, keep reading until a result set is found
				while (dataReader.m_resultSet.State == ResultSetState.NoMoreData && commandListPosition.CommandIndex < commandListPosition.Commands.Count)
				{
					await dataReader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				}
			}
			catch (Exception)
			{
				dataReader.Dispose();
				throw;
			}

			return dataReader;
		}

#if !NETSTANDARD1_3
		internal DataTable BuildSchemaTable()
		{
			var schemaTable = new DataTable("SchemaTable") { Locale = CultureInfo.InvariantCulture };

			var colDefinitions = m_resultSet?.ColumnDefinitions;
			if (colDefinitions is null || m_resultSet!.ContainsCommandParameters)
				return schemaTable;
			schemaTable.MinimumCapacity = colDefinitions.Length;

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
#endif

		private MySqlDataReader(CommandListPosition commandListPosition, ICommandPayloadCreator payloadCreator, IDictionary<string, CachedProcedure?>? cachedProcedures, IMySqlCommand command, CommandBehavior behavior)
		{
			m_commandListPosition = commandListPosition;
			m_payloadCreator = payloadCreator;
			m_cachedProcedures = cachedProcedures;
			Command = command;
			m_behavior = behavior;
			m_resultSet = new ResultSet(this);
		}

#if !NETSTANDARD2_1 && !NETCOREAPP3_0
		internal async Task DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#else
		internal async ValueTask DisposeAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
#endif
		{
			if (!m_closed)
			{
				m_closed = true;

				if (m_resultSet is object && Command!.Connection!.State == ConnectionState.Open)
				{
					Command.Connection.Session.SetTimeout(Constants.InfiniteTimeout);
					try
					{
						while (await NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
						{
						}
					}
					catch (MySqlException ex) when (ex.Number == (int) MySqlErrorCode.QueryInterrupted)
					{
						// ignore "Query execution was interrupted" exceptions when closing a data reader
					}
					m_resultSet = null;
				}

				m_hasMoreResults = false;

				var connection = Command!.Connection!;
				connection.FinishQuerying(m_hasWarnings);

				if ((m_behavior & CommandBehavior.CloseConnection) != 0)
				{
					(Command as IDisposable)?.Dispose();
					await connection.CloseAsync(ioBehavior).ConfigureAwait(false);
				}
				Command = null;
			}
		}

		// If ResultSet.ContainsCommandParameters is true, then this method should be called to read the (single)
		// row in that result set, which contains the values of "out" parameters from the previous stored procedure
		// execution. These values will be stored in the parameters of the associated command.
		private static async Task ReadOutParametersAsync(IMySqlCommand command, ResultSet resultSet, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			await resultSet.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

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
					if (dbTypeMapping is object)
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

		private ResultSet GetResultSet()
		{
			VerifyNotDisposed();
			if (m_resultSet is null || m_resultSet.ContainsCommandParameters)
				throw new InvalidOperationException("There is no current result set.");
			return m_resultSet;
		}

		readonly CommandBehavior m_behavior;
		readonly ICommandPayloadCreator m_payloadCreator;
		readonly IDictionary<string, CachedProcedure?>? m_cachedProcedures;
		CommandListPosition m_commandListPosition;
		bool m_closed;
		int? m_recordsAffected;
		bool m_hasWarnings;
		ResultSet? m_resultSet;
		bool m_hasMoreResults;
#if !NETSTANDARD1_3
		DataTable? m_schemaTable;
#endif
	}
}
