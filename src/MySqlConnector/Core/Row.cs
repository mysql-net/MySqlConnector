using System;
using System.IO;
using System.Runtime.InteropServices;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal abstract class Row
	{
		public void SetData(ReadOnlyMemory<byte> data)
		{
			m_data = data;
			GetDataOffsets(m_data.Span, m_dataOffsets, m_dataLengths!);
		}

		public Row Clone()
		{
			var clonedRow = CloneCore();
			var clonedData = new byte[m_data.Length];
			m_data.CopyTo(clonedData);
			clonedRow.SetData(clonedData);
			return clonedRow;
		}

		public object GetValue(int ordinal)
		{
			if (ordinal < 0 || ordinal > ResultSet.ColumnDefinitions!.Length)
				throw new ArgumentOutOfRangeException(nameof(ordinal), "value must be between 0 and {0}.".FormatInvariant(ResultSet.ColumnDefinitions!.Length));

			if (m_dataOffsets[ordinal] == -1)
				return DBNull.Value;

			var data = m_data.Slice(m_dataOffsets[ordinal], m_dataLengths[ordinal]).Span;
			var columnDefinition = ResultSet.ColumnDefinitions[ordinal];
			return GetValueCore(data, columnDefinition);
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

		public sbyte GetSByte(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is sbyte sbyteValue)
				return sbyteValue;

			if (value is byte byteValue)
				return checked((sbyte) byteValue);
			if (value is short shortValue)
				return checked((sbyte) shortValue);
			if (value is ushort ushortValue)
				return checked((sbyte) ushortValue);
			if (value is int intValue)
				return checked((sbyte) intValue);
			if (value is uint uintValue)
				return checked((sbyte) uintValue);
			if (value is long longValue)
				return checked((sbyte) longValue);
			if (value is ulong ulongValue)
				return checked((sbyte) ulongValue);
			if (value is decimal decimalValue)
				return (sbyte) decimalValue;
			return (sbyte) value;
		}

		public byte GetByte(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is byte byteValue)
				return byteValue;

			if (value is sbyte sbyteValue)
				return checked((byte) sbyteValue);
			if (value is short shortValue)
				return checked((byte) shortValue);
			if (value is ushort ushortValue)
				return checked((byte) ushortValue);
			if (value is int intValue)
				return checked((byte) intValue);
			if (value is uint uintValue)
				return checked((byte) uintValue);
			if (value is long longValue)
				return checked((byte) longValue);
			if (value is ulong ulongValue)
				return checked((byte) ulongValue);
			if (value is decimal decimalValue)
				return (byte) decimalValue;
			return (byte) value;
		}

		public long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
		{
			CheckBinaryColumn(ordinal);

			if (buffer is null)
			{
				// this isn't required by the DbDataReader.GetBytes API documentation, but is what mysql-connector-net does
				// (as does SqlDataReader: http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqldatareader.getbytes.aspx)
				return m_dataLengths[ordinal];
			}

			CheckBufferArguments(dataOffset, buffer, bufferOffset, length);

			var offset = (int) dataOffset;
			var lengthToCopy = Math.Max(0, Math.Min(m_dataLengths[ordinal] - offset, length));
			if (lengthToCopy > 0)
				m_data.Slice(m_dataOffsets[ordinal] + offset, lengthToCopy).Span.CopyTo(buffer.AsSpan().Slice(bufferOffset));
			return lengthToCopy;
		}

		public char GetChar(int ordinal)
		{
			var stringValue = (string) GetValue(ordinal);
			return stringValue.Length > 0 ? stringValue[0] : throw new InvalidCastException();
		}

		public long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
		{
			var value = GetString(ordinal);
			if (buffer is null)
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

		public DateTime GetDateTime(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is MySqlDateTime mySqlDateTime)
				return mySqlDateTime.GetDateTime();
			return (DateTime) value;
		}

		public DateTimeOffset GetDateTimeOffset(int ordinal) => new DateTimeOffset(DateTime.SpecifyKind(GetDateTime(ordinal), DateTimeKind.Utc));

		public Stream GetStream(int ordinal)
		{
			CheckBinaryColumn(ordinal);
			return (MemoryMarshal.TryGetArray(m_data, out var arraySegment)) ?
				new MemoryStream(arraySegment.Array!, arraySegment.Offset + m_dataOffsets[ordinal], m_dataLengths[ordinal], writable: false) :
				throw new InvalidOperationException("Can't get underlying array.");
		}

		public string GetString(int ordinal) => (string) GetValue(ordinal);

		public decimal GetDecimal(int ordinal) => (decimal) GetValue(ordinal);

		public double GetDouble(int ordinal)
		{
			var value = GetValue(ordinal);
			return value is float floatValue ? floatValue :
				value is decimal decimalValue ? (double) decimalValue :
				(double) value;
		}

		public float GetFloat(int ordinal)
		{
			var value = GetValue(ordinal);
			return value is decimal decimalValue ? (float) decimalValue :
				(float) value;
		}

		public MySqlDateTime GetMySqlDateTime(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is DateTime dateTime)
				return new MySqlDateTime(dateTime);
			return (MySqlDateTime) value;
		}

		public MySqlGeometry GetMySqlGeometry(int ordinal)
		{
			var value = GetValue(ordinal);
			if (value is byte[] bytes && ResultSet.ColumnDefinitions![ordinal].ColumnType == ColumnType.Geometry)
				return new MySqlGeometry(bytes);
			throw new InvalidCastException("Can't convert {0} to MySqlGeometry.".FormatInvariant(ResultSet.ColumnDefinitions![ordinal].ColumnType));
		}

		public int GetValues(object[] values)
		{
			int count = Math.Min((values ?? throw new ArgumentNullException(nameof(values))).Length, ResultSet.ColumnDefinitions!.Length);
			for (int i = 0; i < count; i++)
				values[i] = GetValue(i);
			return count;
		}

		public bool IsDBNull(int ordinal) => m_dataOffsets[ordinal] == -1;

		public object this[int ordinal] => GetValue(ordinal);

		public object this[string name] => GetValue(ResultSet.GetOrdinal(name));

		protected Row(ResultSet resultSet)
		{
			ResultSet = resultSet;
			m_dataOffsets = new int[ResultSet.ColumnDefinitions!.Length];
			m_dataLengths = new int[ResultSet.ColumnDefinitions.Length];
		}

		protected abstract Row CloneCore();

		protected abstract object GetValueCore(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition);

		protected abstract void GetDataOffsets(ReadOnlySpan<byte> data, int[] dataOffsets, int[] dataLengths);

		protected ResultSet ResultSet { get; }
		protected MySqlConnection Connection => ResultSet.Connection;

		protected static Guid CreateGuidFromBytes(MySqlGuidFormat guidFormat, ReadOnlySpan<byte> bytes)
		{
#if NET45 || NET461 || NET471 || NETSTANDARD1_3 || NETSTANDARD2_0
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

		private void CheckBinaryColumn(int ordinal)
		{
			if (m_dataOffsets[ordinal] == -1)
				throw new InvalidCastException("Column is NULL.");

			var column = ResultSet.ColumnDefinitions![ordinal];
			var columnType = column.ColumnType;
			if ((column.ColumnFlags & ColumnFlags.Binary) == 0 ||
				(columnType != ColumnType.String && columnType != ColumnType.VarString && columnType != ColumnType.TinyBlob &&
				columnType != ColumnType.Blob && columnType != ColumnType.MediumBlob && columnType != ColumnType.LongBlob &&
				columnType != ColumnType.Geometry))
			{
				throw new InvalidCastException("Can't convert {0} to bytes.".FormatInvariant(columnType));
			}
		}

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

		ReadOnlyMemory<byte> m_data;
		int[] m_dataOffsets;
		int[] m_dataLengths;
	}
}
