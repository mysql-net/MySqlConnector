using System;
using System.Buffers.Text;
using System.Globalization;
using System.Text;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class Row : IDisposable
	{
		public Row(ResultSet resultSet) => ResultSet = resultSet;

		public void SetData(int[] dataLengths, int[] dataOffsets, ArraySegment<byte> payload)
		{
			m_dataLengths = dataLengths;
			m_dataOffsets = dataOffsets;
			m_payload = payload;
		}

		public void BufferData()
		{
			// by default, m_payload, m_dataLengths, and m_dataOffsets are re-used to save allocations
			// if the row is going to be buffered, we need to copy them
			// since m_payload can span multiple rows, offests must recalculated to include only this row

			var bufferDataLengths = new int[m_dataLengths.Length];
			Buffer.BlockCopy(m_dataLengths, 0, bufferDataLengths, 0, m_dataLengths.Length * sizeof(int));
			m_dataLengths = bufferDataLengths;

			var bufferDataOffsets = new int[m_dataOffsets.Length];
			for (var i = 0; i < m_dataOffsets.Length; i++)
			{
				// a -1 offset denotes null, only adjust positive offsets
				if (m_dataOffsets[i] >= 0)
					bufferDataOffsets[i] = m_dataOffsets[i] - m_payload.Offset;
				else
					bufferDataOffsets[i] = m_dataOffsets[i];
			}
			m_dataOffsets = bufferDataOffsets;

			var bufferedPayload = new byte[m_payload.Count];
			Buffer.BlockCopy(m_payload.Array, m_payload.Offset, bufferedPayload, 0, m_payload.Count);
			m_payload = new ArraySegment<byte>(bufferedPayload);
		}

		public void Dispose() => ClearData();

		public void ClearData()
		{
			m_dataLengths = null;
			m_dataOffsets = null;
			m_payload = default(ArraySegment<byte>);
		}

		public bool GetBoolean(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is bool)
				return (bool) value;

			if (value is sbyte)
				return (sbyte) value != 0;
			if (value is byte)
				return (byte) value != 0;
			if (value is short)
				return (short) value != 0;
			if (value is ushort)
				return (ushort) value != 0;
			if (value is int)
				return (int) value != 0;
			if (value is uint)
				return (uint) value != 0;
			if (value is long)
				return (long) value != 0;
			if (value is ulong)
				return (ulong) value != 0;
			return (bool) value;
		}

		public sbyte GetSByte(int ordinal) => (sbyte) GetValue(ordinal);

		public byte GetByte(int ordinal) => (byte) GetValue(ordinal);

		public long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
		{
			if (m_dataOffsets[ordinal] == -1)
				throw new InvalidCastException("Column is NULL.");

			var column = ResultSet.ColumnDefinitions[ordinal];
			var columnType = column.ColumnType;
			if ((column.ColumnFlags & ColumnFlags.Binary) == 0 ||
				(columnType != ColumnType.String && columnType != ColumnType.VarString && columnType != ColumnType.TinyBlob &&
				columnType != ColumnType.Blob && columnType != ColumnType.MediumBlob && columnType != ColumnType.LongBlob))
			{
				throw new InvalidCastException("Can't convert {0} to bytes.".FormatInvariant(columnType));
			}

			if (buffer == null)
			{
				// this isn't required by the DbDataReader.GetBytes API documentation, but is what mysql-connector-net does
				// (as does SqlDataReader: http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqldatareader.getbytes.aspx)
				return m_dataLengths[ordinal];
			}

			CheckBufferArguments(dataOffset, buffer, bufferOffset, length);

			var offset = (int) dataOffset;
			var lengthToCopy = Math.Max(0, Math.Min(m_dataLengths[ordinal] - offset, length));
			Buffer.BlockCopy(m_payload.Array, m_dataOffsets[ordinal] + offset, buffer, bufferOffset, lengthToCopy);
			return lengthToCopy;
		}

		public char GetChar(int ordinal)
		{
			var stringValue = (string) GetValue(ordinal);
			return stringValue.Length > 0 ? stringValue[0] : throw new InvalidCastException();
		}

		public long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
		{
			var value = GetString(ordinal);
			if (buffer == null)
				return value.Length;

			CheckBufferArguments(dataOffset, buffer, bufferOffset, length);

			var offset = (int) dataOffset;
			var lengthToCopy = Math.Max(0, Math.Min(value.Length - offset, length));
			if (lengthToCopy > 0)
				value.CopyTo(offset, buffer, bufferOffset, lengthToCopy);
			return lengthToCopy;
		}

		public Guid GetGuid(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is Guid guid)
				return guid;

			if (value is string stringValue && Guid.TryParse(stringValue, out guid))
				return guid;

			if (value is byte[] bytes && bytes.Length == 16)
				return CreateGuidFromBytes(Connection.GuidFormat, bytes);

			throw new InvalidCastException("The value could not be converted to a GUID: {0}".FormatInvariant(value));
		}

		public short GetInt16(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is short)
				return (short) value;

			if (value is sbyte)
				return (sbyte) value;
			if (value is byte)
				return (byte) value;
			if (value is ushort)
				return checked((short) (ushort) value);
			if (value is int)
				return checked((short) (int) value);
			if (value is uint)
				return checked((short) (uint) value);
			if (value is long)
				return checked((short) (long) value);
			if (value is ulong)
				return checked((short) (ulong) value);
			if (value is decimal)
				return (short) (decimal) value;
			return (short) value;
		}

		public int GetInt32(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is int)
				return (int) value;

			if (value is sbyte)
				return (sbyte) value;
			if (value is byte)
				return (byte) value;
			if (value is short)
				return (short) value;
			if (value is ushort)
				return (ushort) value;
			if (value is uint)
				return checked((int) (uint) value);
			if (value is long)
				return checked((int) (long) value);
			if (value is ulong)
				return checked((int) (ulong) value);
			if (value is decimal)
				return (int) (decimal) value;
			return (int) value;
		}

		public long GetInt64(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is long)
				return (long) value;

			if (value is sbyte)
				return (sbyte) value;
			if (value is byte)
				return (byte) value;
			if (value is short)
				return (short) value;
			if (value is ushort)
				return (ushort) value;
			if (value is int)
				return (int) value;
			if (value is uint)
				return (uint) value;
			if (value is ulong)
				return checked((long) (ulong) value);
			if (value is decimal)
				return (long) (decimal) value;
			return (long) value;
		}

		public ushort GetUInt16(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is ushort)
				return (ushort) value;

			if (value is sbyte)
				return checked((ushort) (sbyte) value);
			if (value is byte)
				return (byte) value;
			if (value is short)
				return checked((ushort) (short) value);
			if (value is int)
				return checked((ushort) (int) value);
			if (value is uint)
				return checked((ushort) (uint) value);
			if (value is long)
				return checked((ushort) (long) value);
			if (value is ulong)
				return checked((ushort) (ulong) value);
			if (value is decimal)
				return (ushort) (decimal) value;
			return (ushort) value;
		}

		public uint GetUInt32(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is uint)
				return (uint) value;

			if (value is sbyte)
				return checked((uint) (sbyte) value);
			if (value is byte)
				return (byte) value;
			if (value is short)
				return checked((uint) (short) value);
			if (value is ushort)
				return (ushort) value;
			if (value is int)
				return checked((uint) (int) value);
			if (value is long)
				return checked((uint) (long) value);
			if (value is ulong)
				return checked((uint) (ulong) value);
			if (value is decimal)
				return (uint) (decimal) value;
			return (uint) value;
		}

		public ulong GetUInt64(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is ulong)
				return (ulong) value;

			if (value is sbyte)
				return checked((ulong) (sbyte) value);
			if (value is byte)
				return (byte) value;
			if (value is short)
				return checked((ulong) (short) value);
			if (value is ushort)
				return (ushort) value;
			if (value is int)
				return checked((ulong) (int) value);
			if (value is uint)
				return (uint) value;
			if (value is long)
				return checked((ulong) (long) value);
			if (value is decimal)
				return (ulong) (decimal) value;
			return (ulong) value;
		}

		public DateTime GetDateTime(int ordinal) => (DateTime) GetValue(ordinal);

		public DateTimeOffset GetDateTimeOffset(int ordinal) => new DateTimeOffset(DateTime.SpecifyKind(GetDateTime(ordinal), DateTimeKind.Utc));

		public string GetString(int ordinal) => (string) GetValue(ordinal);

		public decimal GetDecimal(int ordinal) => (decimal) GetValue(ordinal);

		public double GetDouble(int ordinal)
		{
			var value = GetValue(ordinal);
			return value is float floatValue ? floatValue : (double) value;
		}

		public float GetFloat(int ordinal) => (float) GetValue(ordinal);

		public MySqlDateTime GetMySqlDateTime(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is DateTime dateTime)
				return new MySqlDateTime(dateTime);
			return (MySqlDateTime) value;
		}

		public int GetValues(object[] values)
		{
			int count = Math.Min(values.Length, ResultSet.ColumnDefinitions.Length);
			for (int i = 0; i < count; i++)
				values[i] = GetValue(i);
			return count;
		}

		public bool IsDBNull(int ordinal) => m_dataOffsets[ordinal] == -1;

		public object this[int ordinal] => GetValue(ordinal);

		public object this[string name] => GetValue(ResultSet.GetOrdinal(name));

		public object GetValue(int ordinal)
		{
			if (ordinal < 0 || ordinal > ResultSet.ColumnDefinitions.Length)
				throw new ArgumentOutOfRangeException(nameof(ordinal), "value must be between 0 and {0}.".FormatInvariant(ResultSet.ColumnDefinitions.Length));

			if (m_dataOffsets[ordinal] == -1)
				return DBNull.Value;

			var data = new ReadOnlySpan<byte>(m_payload.Array, m_dataOffsets[ordinal], m_dataLengths[ordinal]);
			var columnDefinition = ResultSet.ColumnDefinitions[ordinal];
			var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
			switch (columnDefinition.ColumnType)
			{
			case ColumnType.Tiny:
				var value = ParseInt32(data);
				if (Connection.TreatTinyAsBoolean && columnDefinition.ColumnLength == 1)
					return value != 0;
				return isUnsigned ? (object) (byte) value : (sbyte) value;

			case ColumnType.Int24:
			case ColumnType.Long:
				return isUnsigned ? (object) ParseUInt32(data) : ParseInt32(data);

			case ColumnType.Longlong:
				return isUnsigned ? (object) ParseUInt64(data) : ParseInt64(data);

			case ColumnType.Bit:
				// BIT column is transmitted as MSB byte array
				ulong bitValue = 0;
				for (int i = 0; i < m_dataLengths[ordinal]; i++)
					bitValue = bitValue * 256 + m_payload.Array[m_dataOffsets[ordinal] + i];
				return bitValue;

			case ColumnType.String:
				if (Connection.GuidFormat == MySqlGuidFormat.Char36 && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
					return Utf8Parser.TryParse(data, out Guid guid, out int guid36BytesConsumed, 'D') && guid36BytesConsumed == 36 ? guid : throw new FormatException();
				if (Connection.GuidFormat == MySqlGuidFormat.Char32 && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 32)
					return Utf8Parser.TryParse(data, out Guid guid, out int guid32BytesConsumed, 'N') && guid32BytesConsumed == 32 ? guid : throw new FormatException();
				goto case ColumnType.VarString;

			case ColumnType.VarString:
			case ColumnType.VarChar:
			case ColumnType.TinyBlob:
			case ColumnType.Blob:
			case ColumnType.MediumBlob:
			case ColumnType.LongBlob:
				if (columnDefinition.CharacterSet == CharacterSet.Binary)
				{
					var guidFormat = Connection.GuidFormat;
					if ((guidFormat == MySqlGuidFormat.Binary16 || guidFormat == MySqlGuidFormat.TimeSwapBinary16 || guidFormat == MySqlGuidFormat.LittleEndianBinary16) && columnDefinition.ColumnLength == 16)
						return CreateGuidFromBytes(guidFormat, data);

					return data.ToArray();
				}
				return Encoding.UTF8.GetString(data);

			case ColumnType.Json:
				return Encoding.UTF8.GetString(data);

			case ColumnType.Short:
				return isUnsigned ? (object) ParseUInt16(data) : ParseInt16(data);

			case ColumnType.Date:
			case ColumnType.DateTime:
			case ColumnType.Timestamp:
				return ParseDateTime(data);

			case ColumnType.Time:
				return Utility.ParseTimeSpan(data);

			case ColumnType.Year:
				return ParseInt32(data);

			case ColumnType.Float:
				return !Utf8Parser.TryParse(data, out float floatValue, out var floatBytesConsumed) || floatBytesConsumed != data.Length ? throw new FormatException() : floatValue;

			case ColumnType.Double:
				return !Utf8Parser.TryParse(data, out double doubleValue, out var doubleBytesConsumed) || doubleBytesConsumed != data.Length ? throw new FormatException() : doubleValue;

			case ColumnType.Decimal:
			case ColumnType.NewDecimal:
				return Utf8Parser.TryParse(data, out decimal decimalValue, out int bytesConsumed) && bytesConsumed == data.Length ? decimalValue : throw new FormatException();

			default:
				throw new NotImplementedException("Reading {0} not implemented".FormatInvariant(columnDefinition.ColumnType));
			}
		}

		private static short ParseInt16(ReadOnlySpan<byte> data) =>
			!Utf8Parser.TryParse(data, out short value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

		private static ushort ParseUInt16(ReadOnlySpan<byte> data) =>
			!Utf8Parser.TryParse(data, out ushort value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

		private static int ParseInt32(ReadOnlySpan<byte> data) =>
			!Utf8Parser.TryParse(data, out int value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

		private static uint ParseUInt32(ReadOnlySpan<byte> data) =>
			!Utf8Parser.TryParse(data, out uint value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

		private static long ParseInt64(ReadOnlySpan<byte> data) =>
			!Utf8Parser.TryParse(data, out long value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

		private static ulong ParseUInt64(ReadOnlySpan<byte> data) =>
			!Utf8Parser.TryParse(data, out ulong value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

		private static void CheckBufferArguments<T>(long dataOffset, T[] buffer, int bufferOffset, int length)
		{
			if (dataOffset < 0)
				throw new ArgumentOutOfRangeException(nameof(dataOffset), dataOffset, "dataOffset must be non-negative");
			if (dataOffset > int.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(dataOffset), dataOffset, "dataOffset must be a 32-bit integer");
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), length, "length must be non-negative");
			if (bufferOffset < 0)
				throw new ArgumentOutOfRangeException(nameof(bufferOffset), bufferOffset, "bufferOffset must be non-negative");
			if (bufferOffset > buffer.Length)
				throw new ArgumentOutOfRangeException(nameof(bufferOffset), bufferOffset, "bufferOffset must be within the buffer");
			if (checked(bufferOffset + length) > buffer.Length)
				throw new ArgumentException("bufferOffset + length cannot exceed buffer.Length", nameof(length));
		}

		private object ParseDateTime(ReadOnlySpan<byte> value)
		{
			if (!Utf8Parser.TryParse(value, out int year, out var bytesConsumed) || bytesConsumed != 4)
				goto InvalidDateTime;
			if (value.Length < 5 || value[4] != 45)
				goto InvalidDateTime;
			if (!Utf8Parser.TryParse(value.Slice(5), out int month, out bytesConsumed) || bytesConsumed != 2)
				goto InvalidDateTime;
			if (value.Length < 8 || value[7] != 45)
				goto InvalidDateTime;
			if (!Utf8Parser.TryParse(value.Slice(8), out int day, out bytesConsumed) || bytesConsumed != 2)
				goto InvalidDateTime;

			if (year == 0 && month == 0 && day == 0)
			{
				if (Connection.ConvertZeroDateTime)
					return DateTime.MinValue;
				if (Connection.AllowZeroDateTime)
					return new MySqlDateTime();
				throw new InvalidCastException("Unable to convert MySQL date/time to System.DateTime.");
			}

			int hour, minute, second, microseconds;
			if (value.Length == 10)
			{
				hour = 0;
				minute = 0;
				second = 0;
				microseconds = 0;
			}
			else
			{
				if (value[10] != 32)
					goto InvalidDateTime;
				if (!Utf8Parser.TryParse(value.Slice(11), out hour, out bytesConsumed) || bytesConsumed != 2)
					goto InvalidDateTime;
				if (value.Length < 14 || value[13] != 58)
					goto InvalidDateTime;
				if (!Utf8Parser.TryParse(value.Slice(14), out minute, out bytesConsumed) || bytesConsumed != 2)
					goto InvalidDateTime;
				if (value.Length < 17 || value[16] != 58)
					goto InvalidDateTime;
				if (!Utf8Parser.TryParse(value.Slice(17), out second, out bytesConsumed) || bytesConsumed != 2)
					goto InvalidDateTime;

				if (value.Length == 19)
				{
					microseconds = 0;
				}
				else
				{
					if (value[19] != 46)
						goto InvalidDateTime;

					if (!Utf8Parser.TryParse(value.Slice(20), out microseconds, out bytesConsumed) || bytesConsumed != value.Length - 20)
						goto InvalidDateTime;
					for (; bytesConsumed < 6; bytesConsumed++)
						microseconds *= 10;
				}
			}

			var dt = new DateTime(year, month, day, hour, minute, second, microseconds / 1000, Connection.DateTimeKind).AddTicks(microseconds % 1000 * 10);
			return Connection.AllowZeroDateTime ? (object) new MySqlDateTime(dt) : dt;

InvalidDateTime:
			throw new FormatException("Couldn't interpret '{0}' as a valid DateTime".FormatInvariant(Encoding.UTF8.GetString(value)));
		}

		private static Guid CreateGuidFromBytes(MySqlGuidFormat guidFormat, ReadOnlySpan<byte> bytes)
		{
#if NET45 || NET461 || NETSTANDARD1_3 || NETSTANDARD2_0
			if (guidFormat == MySqlGuidFormat.Binary16)
				return new Guid(new[] { bytes[3], bytes[2], bytes[1], bytes[0], bytes[5], bytes[4], bytes[7], bytes[6], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] });
			if (guidFormat == MySqlGuidFormat.TimeSwapBinary16)
				return new Guid(new[] { bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2], bytes[1], bytes[0], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] });
			return new Guid(bytes.ToArray());
#else
			unsafe
			{
				if (guidFormat == MySqlGuidFormat.Binary16)
				{
					ReadOnlySpan<byte> guid = stackalloc byte[16] { bytes[3], bytes[2], bytes[1], bytes[0], bytes[5], bytes[4], bytes[7], bytes[6], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] };
					return new Guid(guid);
				}
				if (guidFormat == MySqlGuidFormat.TimeSwapBinary16)
				{
					ReadOnlySpan<byte> guid = stackalloc byte[16] { bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2], bytes[1], bytes[0], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] };
					return new Guid(guid);
				}
				return new Guid(bytes);
			}

#endif
		}

		public readonly ResultSet ResultSet;
		public MySqlConnection Connection => ResultSet.Connection;

		ArraySegment<byte> m_payload;
		int[] m_dataLengths;
		int[] m_dataOffsets;
	}
}
