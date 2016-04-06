using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Serialization;
using static System.FormattableString;

namespace MySql.Data.MySqlClient
{
	public sealed class MySqlDataReader : DbDataReader
	{
		public override bool NextResult()
		{
			return NextResultAsync(CancellationToken.None).GetAwaiter().GetResult();
		}

		public override async Task<bool> NextResultAsync(CancellationToken cancellationToken)
		{
			VerifyNotDisposed();

			while (m_state == State.ReadingRows)
				await ReadAsync(cancellationToken).ConfigureAwait(false);

			if (m_state == State.NoMoreData)
				return false;
			if (m_state != State.HasMoreData)
				throw new InvalidOperationException(Invariant($"Invalid state: {m_state}"));

			Reset();
			await ReadResultSetHeader(cancellationToken).ConfigureAwait(false);
			return true;
		}

		public override bool Read()
		{
			VerifyNotDisposed();
			return ReadAsync(CancellationToken.None).GetAwaiter().GetResult();
		}

		public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
		{
			VerifyNotDisposed();

			// if we've already read past the end of this resultset, Read returns false
			if (m_state == State.HasMoreData || m_state == State.NoMoreData)
				return false;

			if (m_state != State.AlreadyReadFirstRow)
			{
				var payload = await m_session.ReceiveReplyAsync(cancellationToken).ConfigureAwait(false);

				var reader = new ByteArrayReader(payload.ArraySegment);
				var headerByte = reader.ReadByte();

				if (headerByte == 0xFE)
				{
					int warningCount = reader.ReadUInt16();
					var flags = (ServerStatus) reader.ReadUInt16();

					m_state = flags.HasFlag(ServerStatus.MoreResultsExist) ? State.HasMoreData : State.NoMoreData;
					return false;
				}

				reader.Offset--;
				for (var column = 0; column < m_dataOffsets.Length; column++)
				{
					var length = (int) reader.ReadLengthEncodedInteger();
					m_dataLengths[column] = length;
					m_dataOffsets[column] = length == 0xFB ? -1 : reader.Offset;
					reader.Offset += length == 0xFB ? 0 : length;
				}

				m_currentRow = payload.ArraySegment.Array;
			}

			m_state = State.ReadingRows;
			return true;
		}

		public override bool IsClosed => m_command == null;

		public override int RecordsAffected => m_recordsAffected;

		public override bool GetBoolean(int ordinal)
		{
			return (bool) GetValue(ordinal);
		}

		public sbyte GetSByte(int ordinal)
		{
			return (sbyte) GetValue(ordinal);
		}

		public override byte GetByte(int ordinal)
		{
			return (byte) GetValue(ordinal);
		}

		public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
		{
			VerifyRead();

			if (m_dataOffsets[ordinal] == -1)
				throw new InvalidCastException("Column is NULL.");

			var column = m_columnDefinitions[ordinal];
			var columnType = column.ColumnType;
			if (!column.ColumnFlags.HasFlag(ColumnFlags.Binary) ||
				(columnType != ColumnType.String && columnType != ColumnType.VarString && columnType != ColumnType.TinyBlob &&
				columnType != ColumnType.Blob && columnType != ColumnType.MediumBlob && columnType != ColumnType.LongBlob))
			{
				throw new InvalidCastException(Invariant($"Can't convert {columnType} to bytes."));
			}

			if (buffer == null)
			{
				// this isn't required by the DbDataReader.GetBytes API documentation, but is what mysql-connector-net does
				// (as does SqlDataReader: http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqldatareader.getbytes.aspx)
				return m_dataLengths[ordinal];
			}

			if (bufferOffset + length > buffer.Length)
				throw new ArgumentException("bufferOffset + length cannot exceed buffer.Length", nameof(length));

			int lengthToCopy = Math.Min(m_dataLengths[ordinal] - (int) dataOffset, length);
			Array.Copy(m_currentRow, checked((int) (m_dataOffsets[ordinal] + dataOffset)), buffer, bufferOffset, lengthToCopy);
			return lengthToCopy;
		}

		public override char GetChar(int ordinal)
		{
			return (char) GetValue(ordinal);
		}

		public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException();
		}

		public override Guid GetGuid(int ordinal)
		{
			return (Guid) GetValue(ordinal);
		}

		public override short GetInt16(int ordinal)
		{
			return (short) GetValue(ordinal);
		}

		public override int GetInt32(int ordinal)
		{
			object value = GetValue(ordinal);
			if (value is short)
				return (short) value;
			else if (value is long)
				return checked((int) (long) value);
			return (int) value;
		}

		public override long GetInt64(int ordinal)
		{
			object value = GetValue(ordinal);
			if (value is short)
				return (short) value;
			if (value is int)
				return (int) value;
			return (long) value;
		}

		public override DateTime GetDateTime(int ordinal)
		{
			return (DateTime) GetValue(ordinal);
		}

		public override string GetString(int ordinal)
		{
			return (string) GetValue(ordinal);
		}

		public override decimal GetDecimal(int ordinal)
		{
			return (decimal) GetValue(ordinal);
		}

		public override double GetDouble(int ordinal)
		{
			object value = GetValue(ordinal);
			if (value is float)
				return (float) value;
			return (double) value;
		}

		public override float GetFloat(int ordinal)
		{
			return (float) GetValue(ordinal);
		}

		public override string GetName(int ordinal)
		{
			VerifyHasResult();
			if (ordinal < 0 || ordinal > m_columnDefinitions.Length)
				throw new ArgumentOutOfRangeException(nameof(ordinal), Invariant($"value must be between 0 and {m_columnDefinitions.Length - 1}"));
			return m_columnDefinitions[ordinal].Name;
		}

		public override int GetValues(object[] values)
		{
			VerifyRead();
			int count = Math.Min(values.Length, m_columnDefinitions.Length);
			for (int i = 0; i < count; i++)
				values[i] = GetValue(i);
			return count;
		}

		public override bool IsDBNull(int ordinal)
		{
			VerifyRead();
			// TODO: Correct exception for invalid ordinal?
			return m_dataOffsets[ordinal] == -1;
		}

		public override int FieldCount
		{
			get
			{
				VerifyNotDisposed();
				return m_columnDefinitions.Length;
			}
		}

		public override object this[int ordinal] => GetValue(ordinal);

		public override object this[string name] => GetValue(GetOrdinal(name));

		public override bool HasRows
		{
			get
			{
				VerifyNotDisposed();
				if (m_state == State.ReadResultSetHeader)
				{
					if (Read())
					{
						m_state = State.AlreadyReadFirstRow;
						return true;
					}
					return false;
				}
				return m_state == State.ReadingRows;
			}
		}

		public override int GetOrdinal(string name)
		{
			VerifyHasResult();

			for (int column = 0; column < m_columnDefinitions.Length; column++)
			{
				if (m_columnDefinitions[column].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
					return column;
			}

			// TODO: Correct exception
			throw new IndexOutOfRangeException(Invariant($"The column name '{name}' does not exist in the result set."));
		}

		public override string GetDataTypeName(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override Type GetFieldType(int ordinal)
		{
			VerifyHasResult();
			if (ordinal < 0 || ordinal > m_columnDefinitions.Length)
				throw new ArgumentOutOfRangeException(nameof(ordinal), Invariant($"value must be between 0 and {m_columnDefinitions.Length}."));

			var columnDefinition = m_columnDefinitions[ordinal];
			var isUnsigned = columnDefinition.ColumnFlags.HasFlag(ColumnFlags.Unsigned);
			switch (columnDefinition.ColumnType)
			{
			case ColumnType.Tiny:
				return columnDefinition.ColumnLength == 1 ? typeof(bool) :
					isUnsigned ? typeof(byte) : typeof(sbyte);

			case ColumnType.Int24:
			case ColumnType.Long:
				return isUnsigned ? typeof(uint) : typeof(int);

			case ColumnType.Longlong:
				return isUnsigned ? typeof(ulong) : typeof(long);

			case ColumnType.String:
				if (!Connection.OldGuids && columnDefinition.ColumnLength / SerializationUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
					return typeof(Guid);
				goto case ColumnType.VarString;

			case ColumnType.VarString:
			case ColumnType.TinyBlob:
			case ColumnType.Blob:
			case ColumnType.MediumBlob:
			case ColumnType.LongBlob:
				return columnDefinition.ColumnFlags.HasFlag(ColumnFlags.Binary) ?
					(Connection.OldGuids && columnDefinition.ColumnLength == 16 ? typeof(Guid) : typeof(byte[])) :
					typeof(string);

			case ColumnType.Short:
				return isUnsigned ? typeof(ushort) : typeof(short);

			case ColumnType.Date:
			case ColumnType.DateTime:
			case ColumnType.Timestamp:
				return typeof(DateTime);

			case ColumnType.Time:
				return typeof(TimeSpan);

			case ColumnType.Year:
				return typeof(int);

			case ColumnType.Float:
				return typeof(float);

			case ColumnType.Double:
				return typeof(double);

			case ColumnType.Decimal:
			case ColumnType.NewDecimal:
				return typeof(decimal);

			default:
				throw new NotImplementedException(Invariant($"GetFieldType for {columnDefinition.ColumnType} is not implemented"));
			}
		}

		public override object GetValue(int ordinal)
		{
			VerifyRead();
			if (ordinal < 0 || ordinal > m_columnDefinitions.Length)
				throw new ArgumentOutOfRangeException(nameof(ordinal), Invariant($"value must be between 0 and {m_columnDefinitions.Length}."));

			if (m_dataOffsets[ordinal] == -1)
				return DBNull.Value;

			var data = new ArraySegment<byte>(m_currentRow, m_dataOffsets[ordinal], m_dataLengths[ordinal]);
			var columnDefinition = m_columnDefinitions[ordinal];
			var isUnsigned = columnDefinition.ColumnFlags.HasFlag(ColumnFlags.Unsigned);
			switch (columnDefinition.ColumnType)
			{
			case ColumnType.Tiny:
				var value = int.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture);
				if (columnDefinition.ColumnLength == 1)
					return value != 0;
				return isUnsigned ? (object) (byte) value : (sbyte) value;

			case ColumnType.Int24:
			case ColumnType.Long:
				return isUnsigned ? (object) uint.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture) :
					int.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture);

			case ColumnType.Longlong:
				return isUnsigned ? (object) ulong.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture) :
					long.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture);

			case ColumnType.String:
				if (!Connection.OldGuids && columnDefinition.ColumnLength / SerializationUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
					return Guid.Parse(Encoding.UTF8.GetString(data));
				goto case ColumnType.VarString;

			case ColumnType.VarString:
			case ColumnType.VarChar:
			case ColumnType.TinyBlob:
			case ColumnType.Blob:
			case ColumnType.MediumBlob:
			case ColumnType.LongBlob:
				if (columnDefinition.CharacterSet == CharacterSet.Binary)
				{
					var result = new byte[m_dataLengths[ordinal]];
					Array.Copy(m_currentRow, m_dataOffsets[ordinal], result, 0, result.Length);
					return Connection.OldGuids && columnDefinition.ColumnLength == 16 ? (object) new Guid(result) : result;
				}
				return Encoding.UTF8.GetString(data);

			case ColumnType.Short:
				return isUnsigned ? (object) ushort.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture) :
					short.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture);

			case ColumnType.Date:
			case ColumnType.DateTime:
			case ColumnType.Timestamp:
				return ParseDateTime(data);

			case ColumnType.Time:
				return ParseTimeSpan(data);

			case ColumnType.Year:
				return int.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture);

			case ColumnType.Float:
				return float.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture);

			case ColumnType.Double:
				return double.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture);

			case ColumnType.Decimal:
			case ColumnType.NewDecimal:
				return decimal.Parse(Encoding.UTF8.GetString(data), CultureInfo.InvariantCulture);

			default:
				throw new NotImplementedException(Invariant($"Reading {columnDefinition.ColumnType} not implemented"));
			}
		}

		public override IEnumerator GetEnumerator()
		{
			throw new NotSupportedException();
		}

		public override int Depth
		{
			get { throw new NotSupportedException(); }
		}

		protected override DbDataReader GetDbDataReader(int ordinal)
		{
			throw new NotSupportedException();
		}

		public override int VisibleFieldCount => FieldCount;

		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					Reset();
					m_session = null;

					if (m_behavior.HasFlag(CommandBehavior.CloseConnection))
					{
						var dbConnection = m_command.Connection;
						m_command.Dispose();
						dbConnection.Close();
					}

					m_command = null;
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}
		
		internal static async Task<DbDataReader> CreateAsync(MySqlCommand command, CommandBehavior behavior, CancellationToken cancellationToken)
		{
			var dataReader = new MySqlDataReader(command, behavior);
			await dataReader.ReadResultSetHeader(cancellationToken).ConfigureAwait(false);
			return dataReader;
		}

		internal DateTime ParseDateTime(ArraySegment<byte> value)
		{
			var parts = Encoding.UTF8.GetString(value).Split('-', ' ', ':', '.');

			var year = int.Parse(parts[0], CultureInfo.InvariantCulture);
			var month = int.Parse(parts[1], CultureInfo.InvariantCulture);
			var day = int.Parse(parts[2], CultureInfo.InvariantCulture);

			if (year == 0 && month == 0 && day == 0)
			{
				if (Connection.ConvertZeroDateTime)
					return DateTime.MinValue;
				throw new InvalidCastException("Unable to convert MySQL date/time to System.DateTime.");
			}

			if (parts.Length == 3)
				return new DateTime(year, month, day);

			var hour = int.Parse(parts[3], CultureInfo.InvariantCulture);
			var minute = int.Parse(parts[4], CultureInfo.InvariantCulture);
			var second = int.Parse(parts[5], CultureInfo.InvariantCulture);
			if (parts.Length == 6)
				return new DateTime(year, month, day, hour, minute, second);

			var microseconds = int.Parse(parts[6] + new string('0', 6 - parts[6].Length), CultureInfo.InvariantCulture);
			return new DateTime(year, month, day, hour, minute, second, microseconds / 1000).AddTicks(microseconds % 1000 * 10);
		}

		internal static TimeSpan ParseTimeSpan(ArraySegment<byte> value)
		{
			var parts = Encoding.UTF8.GetString(value).Split(':', '.');

			var hours = int.Parse(parts[0], CultureInfo.InvariantCulture);
			var minutes = int.Parse(parts[1], CultureInfo.InvariantCulture);
			if (hours < 0)
				minutes = -minutes;
			var seconds = int.Parse(parts[2], CultureInfo.InvariantCulture);
			if (hours < 0)
				seconds = -seconds;
			if (parts.Length == 3)
				return new TimeSpan(hours, minutes, seconds);

			var microseconds = int.Parse(parts[3] + new string('0', 6 - parts[3].Length), CultureInfo.InvariantCulture);
			if (hours < 0)
				microseconds = -microseconds;
			return new TimeSpan(0, hours, minutes, seconds, microseconds / 1000) + TimeSpan.FromTicks(microseconds % 1000 * 10);
		}

		private MySqlDataReader(MySqlCommand command, CommandBehavior behavior)
		{
			m_command = command;
			m_session = Connection.Session;
			m_behavior = behavior;
		}

		private MySqlConnection Connection => (MySqlConnection) m_command.Connection;

		private async Task ReadResultSetHeader(CancellationToken cancellationToken)
		{
			var payload = await m_session.ReceiveReplyAsync(cancellationToken).ConfigureAwait(false);

			var firstByte = payload.ArraySegment.Array[payload.ArraySegment.Offset];
			if (firstByte == 0)
			{
				var ok = OkPayload.Create(payload);
				m_recordsAffected += ok.AffectedRowCount;
				m_command.LastInsertedId = ok.LastInsertId;
				m_state = ok.ServerStatus.HasFlag(ServerStatus.MoreResultsExist) ? State.HasMoreData : State.NoMoreData;
			}
			else if (firstByte == 0xFB)
			{
				throw new NotSupportedException("Don't support LOCAL_INFILE_Request");
			}
			else
			{
				var reader = new ByteArrayReader(payload.ArraySegment);
				var columnCount = (int) reader.ReadLengthEncodedInteger();
				m_columnDefinitions = new ColumnDefinitionPayload[columnCount];
				m_dataOffsets = new int[columnCount];
				m_dataLengths = new int[columnCount];

				for (var column = 0; column < m_columnDefinitions.Length; column++)
				{
					payload = await m_session.ReceiveReplyAsync(cancellationToken).ConfigureAwait(false);
					m_columnDefinitions[column] = ColumnDefinitionPayload.Create(payload);
				}

				payload = await m_session.ReceiveReplyAsync(cancellationToken).ConfigureAwait(false);
				EofPayload.Create(payload);

				m_command.LastInsertedId = -1;
				m_state = State.ReadResultSetHeader;
			}
		}

		private void Reset()
		{
			m_columnDefinitions = null;
			m_dataOffsets = null;
			m_dataLengths = null;
			m_state = State.None;
		}

		private void VerifyHasResult()
		{
			VerifyNotDisposed();
			if (m_state != State.ReadResultSetHeader && m_state != State.ReadingRows)
				throw new InvalidOperationException("There is no current result set.");
		}

		private void VerifyRead()
		{
			VerifyHasResult();
			if (m_state != State.ReadingRows)
				throw new InvalidOperationException("Read must be called first.");
		}

		private void VerifyNotDisposed()
		{
			if (m_command == null)
				throw new ObjectDisposedException(GetType().Name);
		}

		private enum State
		{
			None,
			ReadResultSetHeader,
			AlreadyReadFirstRow,
			ReadingRows,
			HasMoreData,
			NoMoreData,
		}

		MySqlCommand m_command;
		MySqlSession m_session;
		State m_state;
		readonly CommandBehavior m_behavior;
		int m_recordsAffected;
		ColumnDefinitionPayload[] m_columnDefinitions;
		int[] m_dataOffsets;
		int[] m_dataLengths;
		byte[] m_currentRow;
	}
}
