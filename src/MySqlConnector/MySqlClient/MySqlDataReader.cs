using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient.Results;
using MySql.Data.Protocol.Serialization;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlDataReader : DbDataReader
#if NETSTANDARD1_3 || NETSTANDARD2_0
		, IDbColumnSchemaGenerator
#endif
	{
		public override bool NextResult()
		{
			Command.ResetCommandTimeout();
			return NextResultAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();
		}

		public override bool Read()
		{
			Command.ResetCommandTimeout();
			return GetResultSet().Read();
		}

		public override Task<bool> ReadAsync(CancellationToken cancellationToken)
		{
			Command.ResetCommandTimeout();
			return GetResultSet().ReadAsync(cancellationToken);
		}

		internal Task<bool> ReadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) =>
			GetResultSet().ReadAsync(ioBehavior, cancellationToken);

		public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
		{
			Command.ResetCommandTimeout();
			return NextResultAsync(Command.Connection.AsyncIOBehavior, cancellationToken);
		}

		internal async Task<bool> NextResultAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			VerifyNotDisposed();
			try
			{
				await m_resultSet.ReadEntireAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				var nextResult = m_nextResultSetBuffer.Count > 0
					? m_nextResultSetBuffer.Dequeue()
					: await ScanResultSetAsync(ioBehavior, m_resultSet, cancellationToken).ConfigureAwait(false);

				if (nextResult != null)
				{
					ActivateResultSet(nextResult);

					// https://github.com/mysql-net/MySqlConnector/issues/135
					if (Command.CommandType == CommandType.StoredProcedure)
					{
						var nextRead = await nextResult.BufferReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
						if (nextRead == null)
						{
							var nextResultSet = m_nextResultSetBuffer.Count > 0
								? m_nextResultSetBuffer.Peek()
								: await BufferNextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
							if (nextResultSet == null)
								nextResult = null;
						}
					}
				}

				m_resultSet = nextResult ?? new ResultSet(this);
				return nextResult != null;
			}
			catch (MySqlException)
			{
				m_resultSet = new ResultSet(this);
				m_resultSetBuffered = null;
				m_nextResultSetBuffer.Clear();
				throw;
			}
		}

		private void ActivateResultSet(ResultSet resultSet)
		{
			if (resultSet.ReadResultSetHeaderException != null)
			{
				var mySqlException = resultSet.ReadResultSetHeaderException as MySqlException;

				// for any exception not created from an ErrorPayload, mark the session as failed (because we can't guarantee that all data
				// has been read from the connection and that the socket is still usable)
				if (mySqlException?.SqlState == null)
					Command.Connection.Session.SetFailed();

				throw mySqlException != null ?
					new MySqlException(mySqlException.Number, mySqlException.SqlState, mySqlException.Message, mySqlException) :
					resultSet.ReadResultSetHeaderException;
			}

			Command.LastInsertedId = resultSet.LastInsertId;
			m_recordsAffected += resultSet.RecordsAffected;
		}

		private async Task<ResultSet> BufferNextResultAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			if (m_resultSetBuffered != null)
				await m_resultSetBuffered.BufferEntireAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			// ScanResultSetAsync sets m_resultSetBuffered to the next result set if there is one
			if (await ScanResultSetAsync(ioBehavior, null, cancellationToken).ConfigureAwait(false) == null)
				return null;
			m_nextResultSetBuffer.Enqueue(m_resultSetBuffered);
			return m_resultSetBuffered;
		}

		private ValueTask<ResultSet> ScanResultSetAsync(IOBehavior ioBehavior, ResultSet resultSet, CancellationToken cancellationToken)
		{
			if (m_resultSetBuffered == null)
				return new ValueTask<ResultSet>((ResultSet)null);

			if (m_resultSetBuffered.BufferState == ResultSetState.NoMoreData || m_resultSetBuffered.BufferState == ResultSetState.None)
			{
				m_resultSetBuffered = null;
				return new ValueTask<ResultSet>((ResultSet)null);
			}

			if (m_resultSetBuffered.BufferState != ResultSetState.HasMoreData)
				throw new InvalidOperationException("Invalid state: {0}".FormatInvariant(m_resultSetBuffered.State));

			if (resultSet == null)
				resultSet = new ResultSet(this);
			return new ValueTask<ResultSet>(ScanResultSetAsyncAwaited(ioBehavior, resultSet, cancellationToken));
		}

		private async Task<ResultSet> ScanResultSetAsyncAwaited(IOBehavior ioBehavior, ResultSet resultSet, CancellationToken cancellationToken)
		{
			using (Command.RegisterCancel(cancellationToken))
			{
				try
				{
					m_resultSetBuffered = await resultSet.ReadResultSetHeaderAsync(ioBehavior).ConfigureAwait(false);
					return m_resultSetBuffered;
				}
				catch (MySqlException ex) when (ex.Number == (int) MySqlErrorCode.QueryInterrupted)
				{
					m_resultSetBuffered = null;
					cancellationToken.ThrowIfCancellationRequested();
					throw;
				}
			}
		}

		public override string GetName(int ordinal) => GetResultSet().GetName(ordinal);

		public override int GetValues(object[] values) => GetResultSet().GetCurrentRow().GetValues(values);

		public override bool IsDBNull(int ordinal) => GetResultSet().GetCurrentRow().IsDBNull(ordinal);

		public override int FieldCount => GetResultSet().FieldCount;

		public override object this[int ordinal] => GetResultSet().GetCurrentRow()[ordinal];

		public override object this[string name] => GetResultSet().GetCurrentRow()[name];

		public override bool HasRows => GetResultSet().HasRows;
		public override bool IsClosed => Command == null;
		public override int RecordsAffected => m_recordsAffected;

		public override int GetOrdinal(string name) => GetResultSet().GetCurrentRow().GetOrdinal(name);

		public override bool GetBoolean(int ordinal) => GetResultSet().GetCurrentRow().GetBoolean(ordinal);

		public override byte GetByte(int ordinal) => GetResultSet().GetCurrentRow().GetByte(ordinal);

		public sbyte GetSByte(int ordinal) => GetResultSet().GetCurrentRow().GetSByte(ordinal);

		public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
			=> GetResultSet().GetCurrentRow().GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

		public override char GetChar(int ordinal) => GetResultSet().GetCurrentRow().GetChar(ordinal);

		public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
			=> GetResultSet().GetCurrentRow().GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

		public override Guid GetGuid(int ordinal) => GetResultSet().GetCurrentRow().GetGuid(ordinal);

		public override short GetInt16(int ordinal) => GetResultSet().GetCurrentRow().GetInt16(ordinal);

		public override int GetInt32(int ordinal) => GetResultSet().GetCurrentRow().GetInt32(ordinal);

		public override long GetInt64(int ordinal) => GetResultSet().GetCurrentRow().GetInt64(ordinal);

		public override string GetDataTypeName(int ordinal) => GetResultSet().GetDataTypeName(ordinal);

		public override Type GetFieldType(int ordinal) => GetResultSet().GetFieldType(ordinal);

		public override object GetValue(int ordinal) => GetResultSet().GetCurrentRow().GetValue(ordinal);

		public override IEnumerator GetEnumerator() => new DbEnumerator(this, closeReader: false);

		public override int Depth => throw new NotSupportedException();

		protected override DbDataReader GetDbDataReader(int ordinal) => throw new NotSupportedException();

		public override DateTime GetDateTime(int ordinal) => GetResultSet().GetCurrentRow().GetDateTime(ordinal);

		public DateTimeOffset GetDateTimeOffset(int ordinal) => GetResultSet().GetCurrentRow().GetDateTimeOffset(ordinal);

		public override string GetString(int ordinal) => GetResultSet().GetCurrentRow().GetString(ordinal);

		public override decimal GetDecimal(int ordinal) => GetResultSet().GetCurrentRow().GetDecimal(ordinal);

		public override double GetDouble(int ordinal) => GetResultSet().GetCurrentRow().GetDouble(ordinal);

		public override float GetFloat(int ordinal) => GetResultSet().GetCurrentRow().GetFloat(ordinal);

		public override int VisibleFieldCount => FieldCount;

#if !NETSTANDARD1_3
		public override DataTable GetSchemaTable()
		{
			if (m_schemaTable == null)
			{
				m_schemaTable = BuildSchemaTable();
			}

			return m_schemaTable;
		}

		public override void Close()
		{
			DoClose();
		}
#endif

		public ReadOnlyCollection<DbColumn> GetColumnSchema()
		{
			return GetResultSet().ColumnDefinitions
				.Select((c, n) => (DbColumn) new MySqlDbColumn(n, c, GetFieldType(n), GetDataTypeName(n)))
				.ToList().AsReadOnly();
		}

		public override T GetFieldValue<T>(int ordinal)
		{
			if (typeof(T) == typeof(DateTimeOffset))
				return (T) Convert.ChangeType(GetDateTimeOffset(ordinal), typeof(T));

			return base.GetFieldValue<T>(ordinal);
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
				base.Dispose(disposing);
			}
		}

		internal MySqlCommand Command { get; private set; }
		internal MySqlConnection Connection => Command?.Connection;
		internal MySqlSession Session => Command?.Connection.Session;

		internal static async Task<MySqlDataReader> CreateAsync(MySqlCommand command, CommandBehavior behavior, IOBehavior ioBehavior)
		{
			var dataReader = new MySqlDataReader(command, behavior);
			command.Connection.SetActiveReader(dataReader);

			try
			{
				await dataReader.ReadFirstResultSetAsync(ioBehavior).ConfigureAwait(false);
				if (command.Connection.BufferResultSets)
				{
					while (await dataReader.BufferNextResultAsync(ioBehavior, CancellationToken.None).ConfigureAwait(false) != null)
					{
					}
				}
				return dataReader;
			}
			catch (Exception)
			{
				dataReader.Dispose();
				throw;
			}
			finally
			{
				if (command.Connection.BufferResultSets)
					command.Connection.FinishQuerying();
			}
		}

		internal async Task ReadFirstResultSetAsync(IOBehavior ioBehavior)
		{
			m_resultSet = await new ResultSet(this).ReadResultSetHeaderAsync(ioBehavior).ConfigureAwait(false);
			ActivateResultSet(m_resultSet);
			m_resultSetBuffered = m_resultSet;
		}

#if !NETSTANDARD1_3
		internal DataTable BuildSchemaTable()
		{
			var colDefinitions = GetResultSet().ColumnDefinitions;
			DataTable schemaTable = new DataTable("SchemaTable");
			schemaTable.Locale = CultureInfo.InvariantCulture;
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

		private MySqlDataReader(MySqlCommand command, CommandBehavior behavior)
		{
			Command = command;
			m_behavior = behavior;
		}

		private void DoClose()
		{
			if (Command != null)
			{
				if (m_resultSet != null)
				{
					Command.Connection.Session.SetTimeout(Constants.InfiniteTimeout);
					try
					{
						while (NextResultAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult())
						{
						}
					}
					catch (MySqlException ex) when (ex.Number == (int) MySqlErrorCode.QueryInterrupted)
					{
						// ignore "Query execution was interrupted" exceptions when closing a data reader
					}
					m_resultSet = null;
				}

				m_resultSetBuffered = null;
				m_nextResultSetBuffer.Clear();

				var connection = Command.Connection;
				if (!connection.BufferResultSets)
					connection.FinishQuerying();

				Command.ReaderClosed();
				if ((m_behavior & CommandBehavior.CloseConnection) != 0)
				{
					Command.Dispose();
					connection.Close();
				}
				Command = null;
			}
		}

		private void VerifyNotDisposed()
		{
			if (Command == null)
				throw new ObjectDisposedException(GetType().Name);
		}

		private ResultSet GetResultSet()
		{
			VerifyNotDisposed();
			return m_resultSet ?? throw new InvalidOperationException("There is no current result set.");
		}

		readonly CommandBehavior m_behavior;
		int m_recordsAffected;
		ResultSet m_resultSet;
		ResultSet m_resultSetBuffered;
#if !NETSTANDARD1_3
		DataTable m_schemaTable;
#endif
		readonly Queue<ResultSet> m_nextResultSetBuffer = new Queue<ResultSet>();
	}
}
