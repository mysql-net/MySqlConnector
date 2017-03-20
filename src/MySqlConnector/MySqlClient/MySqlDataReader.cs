using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient.Results;
using MySql.Data.Protocol.Serialization;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlDataReader : DbDataReader
	{
		public override bool NextResult() =>
			NextResultAsync(IOBehavior.Synchronous, CancellationToken.None).GetAwaiter().GetResult();

		public override bool Read() => GetResultSet().Read();

		public override Task<bool> ReadAsync(CancellationToken cancellationToken) => GetResultSet().ReadAsync(cancellationToken);

		internal Task<bool> ReadAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) =>
			GetResultSet().ReadAsync(ioBehavior, cancellationToken);

		public override Task<bool> NextResultAsync(CancellationToken cancellationToken) =>
			NextResultAsync(Command.Connection.AsyncIOBehavior, cancellationToken);

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
			m_resultSetBuffered = await resultSet.ReadResultSetHeaderAsync(ioBehavior, cancellationToken);
			return m_resultSetBuffered;
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

		public override int Depth
		{
			get { throw new NotSupportedException(); }
		}

		protected override DbDataReader GetDbDataReader(int ordinal)
		{
			throw new NotSupportedException();
		}

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
			throw new NotSupportedException();
		}

		public override void Close()
		{
			DoClose();
		}
#endif

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

		internal static async Task<MySqlDataReader> CreateAsync(MySqlCommand command, CommandBehavior behavior, IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			var dataReader = new MySqlDataReader(command, behavior);
			await dataReader.ReadFirstResultSetAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			return dataReader;
		}

		internal async Task ReadFirstResultSetAsync(IOBehavior ioBehavior, CancellationToken cancellationToken)
		{
			m_resultSet = await new ResultSet(this).ReadResultSetHeaderAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			ActivateResultSet(m_resultSet);
			m_resultSetBuffered = m_resultSet;
		}

		private MySqlDataReader(MySqlCommand command, CommandBehavior behavior)
		{
			Command = command;
			m_behavior = behavior;
		}

		private void DoClose()
		{
			if (Command != null)
			{
				while (NextResult())
				{
				}
				m_resultSet = null;
				m_resultSetBuffered = null;
				m_nextResultSetBuffer.Clear();

				var connection = Command.Connection;
				connection.ActiveReader = null;
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
			if (m_resultSet == null)
				throw new InvalidOperationException("There is no current result set.");
			return m_resultSet;
		}

		readonly CommandBehavior m_behavior;
		int m_recordsAffected;
		ResultSet m_resultSet;
		ResultSet m_resultSetBuffered;
		readonly Queue<ResultSet> m_nextResultSetBuffer = new Queue<ResultSet>();
	}
}
