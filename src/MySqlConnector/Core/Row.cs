using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using MySqlConnector.ColumnReaders;
using MySqlConnector.Protocol;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

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
		if (ordinal < 0 || ordinal >= ResultSet.ColumnDefinitions!.Length)
			throw new ArgumentOutOfRangeException(nameof(ordinal), $"value must be between 0 and {ResultSet.ColumnDefinitions!.Length - 1}");

		if (m_dataOffsets[ordinal] == -1)
			return DBNull.Value;

		var data = m_data.Slice(m_dataOffsets[ordinal], m_dataLengths[ordinal]).Span;
		var columnDefinition = ResultSet.ColumnDefinitions[ordinal];
		return m_columnReaders[ordinal].ReadValue(data, columnDefinition);
	}

	public bool GetBoolean(int ordinal)
	{
		var value = GetValue(ordinal);
		return value switch
		{
			bool boolValue => boolValue,
			sbyte sbyteValue => sbyteValue != 0,
			byte byteValue => byteValue != 0,
			short shortValue => shortValue != 0,
			ushort ushortValue => ushortValue != 0,
			int intValue => intValue != 0,
			uint uintValue => uintValue != 0,
			long longValue => longValue != 0,
			ulong ulongValue => ulongValue != 0,
			decimal decimalValue => decimalValue != 0,
			_ => (bool) value,
		};
	}

	public sbyte GetSByte(int ordinal)
	{
		var value = GetValue(ordinal);
		return value switch
		{
			sbyte sbyteValue => sbyteValue,
			byte byteValue => checked((sbyte) byteValue),
			short shortValue => checked((sbyte) shortValue),
			ushort ushortValue => checked((sbyte) ushortValue),
			int intValue => checked((sbyte) intValue),
			uint uintValue => checked((sbyte) uintValue),
			long longValue => checked((sbyte) longValue),
			ulong ulongValue => checked((sbyte) ulongValue),
			decimal decimalValue => (sbyte) decimalValue,
			bool boolValue => boolValue ? (sbyte) 1 : (sbyte) 0,
			_ => (sbyte) value,
		};
	}

	public byte GetByte(int ordinal)
	{
		var value = GetValue(ordinal);
		return value switch
		{
			byte byteValue => byteValue,
			sbyte sbyteValue => checked((byte) sbyteValue),
			short shortValue => checked((byte) shortValue),
			ushort ushortValue => checked((byte) ushortValue),
			int intValue => checked((byte) intValue),
			uint uintValue => checked((byte) uintValue),
			long longValue => checked((byte) longValue),
			ulong ulongValue => checked((byte) ulongValue),
			decimal decimalValue => (byte) decimalValue,
			bool boolValue => boolValue ? (byte) 1 : (byte) 0,
			_ => (byte) value,
		};
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
			m_data.Slice(m_dataOffsets[ordinal] + offset, lengthToCopy).Span.CopyTo(buffer.AsSpan(bufferOffset));
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

		if (value is byte[] { Length: 16 } bytes)
			return CreateGuidFromBytes(Connection.GuidFormat, bytes);

		return (Guid) value;
	}

	public short GetInt16(int ordinal)
	{
		var value = GetValue(ordinal);
		return value switch
		{
			short shortValue => shortValue,
			sbyte sbyteValue => sbyteValue,
			byte byteValue => byteValue,
			ushort ushortValue => checked((short) ushortValue),
			int intValue => checked((short) intValue),
			uint uintValue => checked((short) uintValue),
			long longValue => checked((short) longValue),
			ulong ulongValue => checked((short) ulongValue),
			decimal decimalValue => (short) decimalValue,
			bool boolValue => boolValue ? (short) 1 : (short) 0,
			_ => (short) value,
		};
	}

	public int GetInt32(int ordinal)
	{
		if (ordinal < 0 || ordinal >= ResultSet.ColumnDefinitions!.Length)
			throw new ArgumentOutOfRangeException(nameof(ordinal), $"value must be between 0 and {ResultSet.ColumnDefinitions!.Length - 1}");
		if (m_dataOffsets[ordinal] == -1)
			throw new InvalidCastException("Can't convert NULL to Int32");

		var data = m_data.Slice(m_dataOffsets[ordinal], m_dataLengths[ordinal]).Span;
		var columnDefinition = ResultSet.ColumnDefinitions[ordinal];
		if (m_columnReaders[ordinal].TryReadInt32(data, columnDefinition) is { } value)
			return value;
		throw new InvalidCastException($"Can't convert {ResultSet.GetColumnType(ordinal)} to Int32");
	}

	public long GetInt64(int ordinal)
	{
		var value = GetValue(ordinal);
		return value switch
		{
			long longValue => longValue,
			sbyte sbyteValue => sbyteValue,
			byte byteValue => byteValue,
			short shortValue => shortValue,
			ushort ushortValue => ushortValue,
			int intValue => intValue,
			uint uintValue => uintValue,
			ulong ulongValue => checked((long) ulongValue),
			decimal decimalValue => (long) decimalValue,
			bool boolValue => boolValue ? 1 : 0,
			_ => (long) value,
		};
	}

	public ushort GetUInt16(int ordinal)
	{
		var value = GetValue(ordinal);
		return value switch
		{
			ushort ushortValue => ushortValue,
			sbyte sbyteValue => checked((ushort) sbyteValue),
			byte byteValue => byteValue,
			short shortValue => checked((ushort) shortValue),
			int intValue => checked((ushort) intValue),
			uint uintValue => checked((ushort) uintValue),
			long longValue => checked((ushort) longValue),
			ulong ulongValue => checked((ushort) ulongValue),
			decimal decimalValue => (ushort) decimalValue,
			bool boolValue => boolValue ? (ushort) 1 : (ushort) 0,
			_ => (ushort) value,
		};
	}

	public uint GetUInt32(int ordinal)
	{
		var value = GetValue(ordinal);
		return value switch
		{
			uint uintValue => uintValue,
			sbyte sbyteValue => checked((uint) sbyteValue),
			byte byteValue => byteValue,
			short shortValue => checked((uint) shortValue),
			ushort ushortValue => ushortValue,
			int intValue => checked((uint) intValue),
			long longValue => checked((uint) longValue),
			ulong ulongValue => checked((uint) ulongValue),
			decimal decimalValue => (uint) decimalValue,
			bool boolValue => boolValue ? 1u : 0,
			_ => (uint) value,
		};
	}

	public ulong GetUInt64(int ordinal)
	{
		var value = GetValue(ordinal);
		return value switch
		{
			ulong ulongValue => ulongValue,
			sbyte sbyteValue => checked((ulong) sbyteValue),
			byte byteValue => byteValue,
			short shortValue => checked((ulong) shortValue),
			ushort ushortValue => ushortValue,
			int intValue => checked((ulong) intValue),
			uint uintValue => uintValue,
			long longValue => checked((ulong) longValue),
			decimal decimalValue => (ulong) decimalValue,
			bool boolValue => boolValue ? 1ul : 0,
			_ => (ulong) value,
		};
	}

	public DateTime GetDateTime(int ordinal)
	{
		var value = GetValue(ordinal);

		if (value is string dateString)
		{
			// slightly inefficient to roundtrip the bytes through a string, but this is assumed to be an infrequent code path; this could be optimised to reprocess the original bytes
			if (dateString.Length is >= 10 and <= 26)
				value = TextDateTimeColumnReader.ParseDateTime(Encoding.UTF8.GetBytes(dateString), Connection.ConvertZeroDateTime, Connection.AllowZeroDateTime, Connection.DateTimeKind);
			else
				throw new FormatException($"Couldn't interpret value as a valid DateTime: {value}");
		}

		if (value is MySqlDateTime mySqlDateTime)
			return mySqlDateTime.GetDateTime();
		return (DateTime) value;
	}

	public DateTimeOffset GetDateTimeOffset(int ordinal) => new DateTimeOffset(DateTime.SpecifyKind(GetDateTime(ordinal), DateTimeKind.Utc));

	public Stream GetStream(int ordinal)
	{
		CheckBinaryColumn(ordinal);
		return MemoryMarshal.TryGetArray(m_data, out var arraySegment) ?
			new MemoryStream(arraySegment.Array!, arraySegment.Offset + m_dataOffsets[ordinal], m_dataLengths[ordinal], writable: false) :
			throw new InvalidOperationException("Can't get underlying array.");
	}

	public string GetString(int ordinal) => (string) GetValue(ordinal);

	public decimal GetDecimal(int ordinal)
	{
		var value = GetValue(ordinal);
		return value switch
		{
			decimal decimalValue => decimalValue,
			double doubleValue => (decimal) doubleValue,
			float floatValue => (decimal) floatValue,
			_ => (decimal) value,
		};
	}

	public double GetDouble(int ordinal)
	{
		var value = GetValue(ordinal);
		return value switch
		{
			double doubleValue => doubleValue,
			float floatValue => floatValue,
			decimal decimalValue => (double) decimalValue,
			_ => (double) value,
		};
	}

	public float GetFloat(int ordinal)
	{
		// Loss of precision is expected, significant loss of information is not.
		// Use explicit range checks to guard against that.
		var value = GetValue(ordinal);
		return value switch
		{
			float floatValue => floatValue,
			double doubleValue when doubleValue is >= float.MinValue and <= float.MaxValue => (float) doubleValue,
			double _ => throw new InvalidCastException("The value cannot be safely cast to Single."),
			decimal decimalValue => (float) decimalValue,
			_ => (float) value,
		};
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
		throw new InvalidCastException($"Can't convert {ResultSet.ColumnDefinitions![ordinal].ColumnType} to MySqlGeometry.");
	}

	public MySqlDecimal GetMySqlDecimal(int ordinal)
	{
		if (IsDBNull(ordinal))
			return (MySqlDecimal) GetValue(ordinal);
		var data = m_data.Slice(m_dataOffsets[ordinal], m_dataLengths[ordinal]).Span;
		var columnType = ResultSet.ColumnDefinitions![ordinal].ColumnType;
		if (columnType is ColumnType.NewDecimal or ColumnType.Decimal)
			return new MySqlDecimal(Encoding.UTF8.GetString(data));
		throw new InvalidCastException($"Can't convert {ResultSet.ColumnDefinitions![ordinal].ColumnType} to MySqlDecimal.");
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

	protected Row(bool binary, ResultSet resultSet)
	{
		ResultSet = resultSet;
		m_dataOffsets = new int[ResultSet.ColumnDefinitions!.Length];
		m_dataLengths = new int[m_dataOffsets.Length];
		m_columnReaders = new ColumnReader[m_dataOffsets.Length];
		for (var i = 0; i < m_columnReaders.Length; i++)
			m_columnReaders[i] = ColumnReader.Create(binary, ResultSet.ColumnDefinitions[i], resultSet.Connection);
	}

	protected abstract Row CloneCore();

	protected abstract void GetDataOffsets(ReadOnlySpan<byte> data, int[] dataOffsets, int[] dataLengths);

	protected ResultSet ResultSet { get; }
	protected MySqlConnection Connection => ResultSet.Connection;

#if NET5_0_OR_GREATER
	[SkipLocalsInit]
#endif
	protected static unsafe Guid CreateGuidFromBytes(MySqlGuidFormat guidFormat, ReadOnlySpan<byte> bytes) =>
		guidFormat switch
		{
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
			MySqlGuidFormat.Binary16 => new Guid(stackalloc byte[16] { bytes[3], bytes[2], bytes[1], bytes[0], bytes[5], bytes[4], bytes[7], bytes[6], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] }),
			MySqlGuidFormat.TimeSwapBinary16 => new Guid(stackalloc byte[16] { bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2], bytes[1], bytes[0], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] }),
			_ => new Guid(bytes),
#else
			MySqlGuidFormat.Binary16 => new Guid(new[] { bytes[3], bytes[2], bytes[1], bytes[0], bytes[5], bytes[4], bytes[7], bytes[6], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] }),
			MySqlGuidFormat.TimeSwapBinary16 => new Guid(new[] { bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2], bytes[1], bytes[0], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] }),
			_ => new Guid(bytes.ToArray()),
#endif
		};

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
			throw new InvalidCastException($"Can't convert {columnType} to bytes.");
		}
	}

	private static void CheckBufferArguments<T>(long dataOffset, T[] buffer, int bufferOffset, int length)
	{
		if (dataOffset < 0)
			throw new ArgumentOutOfRangeException(nameof(dataOffset), dataOffset, nameof(dataOffset) + " must be non-negative");
		if (dataOffset > int.MaxValue)
			throw new ArgumentOutOfRangeException(nameof(dataOffset), dataOffset, nameof(dataOffset) + " must be a 32-bit integer");
		if (length < 0)
			throw new ArgumentOutOfRangeException(nameof(length), length, nameof(length) + " must be non-negative");
		if (bufferOffset < 0)
			throw new ArgumentOutOfRangeException(nameof(bufferOffset), bufferOffset, nameof(bufferOffset) + " must be non-negative");
		if (bufferOffset > buffer.Length)
			throw new ArgumentOutOfRangeException(nameof(bufferOffset), bufferOffset, nameof(bufferOffset) + " must be within the buffer");
		if (checked(bufferOffset + length) > buffer.Length)
			throw new ArgumentException(nameof(bufferOffset) + " + " + nameof(length) + " cannot exceed " + nameof(buffer) + "." + nameof(buffer.Length), nameof(length));
	}

	private readonly int[] m_dataOffsets;
	private readonly int[] m_dataLengths;
	private readonly ColumnReader[] m_columnReaders;
	private ReadOnlyMemory<byte> m_data;
}
