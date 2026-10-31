using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using MySqlConnector.ColumnReaders;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
using MySqlConnector.Utilities;
#endif

namespace MySqlConnector.Core;

internal sealed class Row
{
	public Row(bool isBinary, ResultSet resultSet)
	{
		m_isBinary = isBinary;
		ResultSet = resultSet;

		var columnDefinitions = ResultSet.ColumnDefinitions!;
		m_dataOffsetLengths = new OffsetLength[columnDefinitions.Length];
		m_columnReaders = new ColumnReader[columnDefinitions.Length];
		for (var column = 0; column < m_columnReaders.Length; column++)
			m_columnReaders[column] = ColumnReader.Create(isBinary, columnDefinitions[column], resultSet.Connection);
	}

	public void SetData(ReadOnlyMemory<byte> data)
	{
		m_data = data;
		if (m_isBinary)
		{
			Array.Clear(m_dataOffsetLengths, 0, m_dataOffsetLengths.Length);
			for (var column = 0; column < m_dataOffsetLengths.Length; column++)
			{
				if ((data.Span[((column + 2) / 8) + 1] & (1 << ((column + 2) % 8))) != 0)
				{
					// column is NULL
					m_dataOffsetLengths[column] = (-1, 0);
				}
			}

			var reader = new ByteArrayReader(data.Span);

			// skip packet header (1 byte) and NULL bitmap (formula for length at https://dev.mysql.com/doc/internals/en/null-bitmap.html)
			reader.Offset += 1 + ((m_dataOffsetLengths.Length + 7 + 2) / 8);
			for (var column = 0; column < m_dataOffsetLengths.Length; column++)
			{
				if (m_dataOffsetLengths[column].Offset != -1)
				{
					var columnDefinition = ResultSet.ColumnDefinitions![column];
					var length = columnDefinition.ColumnType switch
					{
						ColumnType.Longlong or ColumnType.Double => 8,
						ColumnType.Long or ColumnType.Int24 or ColumnType.Float => 4,
						ColumnType.Short or ColumnType.Year => 2,
						ColumnType.Tiny => 1,
						ColumnType.Date or ColumnType.DateTime or ColumnType.NewDate or ColumnType.Timestamp or ColumnType.Time => reader.ReadByte(),
						ColumnType.DateTime2 or ColumnType.Timestamp2 => throw new NotSupportedException($"ColumnType {columnDefinition.ColumnType} is not supported"),
						_ => checked((int) reader.ReadLengthEncodedInteger()),
					};

					m_dataOffsetLengths[column] = (reader.Offset, length);
					reader.Offset += length;
				}
			}
		}
		else
		{
			var reader = new ByteArrayReader(data.Span);
			for (var column = 0; column < m_dataOffsetLengths.Length; column++)
			{
				var length = reader.ReadLengthEncodedIntegerOrNull();
				m_dataOffsetLengths[column] = length == -1 ? (-1, 0) : (reader.Offset, length);
				reader.Offset += m_dataOffsetLengths[column].Length;
			}
		}
	}

	public object GetValue(int ordinal)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(ordinal, ResultSet.ColumnDefinitions!.Length);

		if (m_dataOffsetLengths[ordinal].Offset == -1)
			return DBNull.Value;

		var (offset, length) = m_dataOffsetLengths[ordinal];
		var data = m_data.Slice(offset, length).Span;
		var columnDefinition = ResultSet.ColumnDefinitions[ordinal];
		return m_columnReaders[ordinal].ReadValue(data, columnDefinition);
	}

	public bool GetBoolean(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "Boolean", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.Boolean => columnReader.ReadBoolean(data, columnDefinition),
			ColumnReaderKind.SignedInteger => columnReader.ReadInt64(data, columnDefinition) != 0,
			ColumnReaderKind.UnsignedInteger => columnReader.ReadUInt64(data, columnDefinition) != 0,
			ColumnReaderKind.Decimal => columnReader.ReadDecimal(data, columnDefinition) != 0,
			_ => throw CreateInvalidCastException(ordinal, "Boolean"),
		};
	}

	public sbyte GetSByte(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "SByte", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.SignedInteger => checked((sbyte) columnReader.ReadInt64(data, columnDefinition)),
			ColumnReaderKind.UnsignedInteger => checked((sbyte) columnReader.ReadUInt64(data, columnDefinition)),
			ColumnReaderKind.Decimal => (sbyte) columnReader.ReadDecimal(data, columnDefinition),
			ColumnReaderKind.Boolean => columnReader.ReadBoolean(data, columnDefinition) ? (sbyte) 1 : (sbyte) 0,
			_ => throw CreateInvalidCastException(ordinal, "SByte"),
		};
	}

	public byte GetByte(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "Byte", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.SignedInteger => checked((byte) columnReader.ReadInt64(data, columnDefinition)),
			ColumnReaderKind.UnsignedInteger => checked((byte) columnReader.ReadUInt64(data, columnDefinition)),
			ColumnReaderKind.Decimal => (byte) columnReader.ReadDecimal(data, columnDefinition),
			ColumnReaderKind.Boolean => columnReader.ReadBoolean(data, columnDefinition) ? (byte) 1 : (byte) 0,
			_ => throw CreateInvalidCastException(ordinal, "Byte"),
		};
	}

	public long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
	{
		CheckBinaryColumn(ordinal);

		if (buffer is null)
		{
			// this isn't required by the DbDataReader.GetBytes API documentation, but is what mysql-connector-net does
			// (as does SqlDataReader: http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqldatareader.getbytes.aspx)
			return m_dataOffsetLengths[ordinal].Length;
		}

		CheckBufferArguments(dataOffset, buffer, bufferOffset, length);

		var offset = (int) dataOffset;
		var lengthToCopy = Math.Max(0, Math.Min(m_dataOffsetLengths[ordinal].Length - offset, length));
		if (lengthToCopy > 0)
			m_data.Slice(m_dataOffsetLengths[ordinal].Offset + offset, lengthToCopy).Span.CopyTo(buffer.AsSpan(bufferOffset));
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
		var columnReader = GetColumnReader(ordinal, "Guid", out var data, out var columnDefinition);
		switch (columnReader.Kind)
		{
		case ColumnReaderKind.Guid:
			return columnReader.ReadGuid(data, columnDefinition);

		case ColumnReaderKind.String:
			// try the common 'D' format first, then fall back to all the formats that Guid.TryParse accepts
			if ((Utf8Parser.TryParse(data, out Guid guid, out var bytesConsumed, 'D') && bytesConsumed == data.Length) || Guid.TryParse(Encoding.UTF8.GetString(data), out guid))
				return guid;
			break;

		case ColumnReaderKind.Bytes when data.Length == 16:
			return Connection.GuidFormat switch
			{
				MySqlGuidFormat.Binary16 => GuidBinary16ColumnReader.ReadGuid(data),
				MySqlGuidFormat.TimeSwapBinary16 => GuidTimeSwapBinary16ColumnReader.ReadGuid(data),
				_ => GuidLittleEndianBinary16ColumnReader.ReadGuid(data),
			};
		}

		throw CreateInvalidCastException(ordinal, "Guid");
	}

	public short GetInt16(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "Int16", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.SignedInteger => checked((short) columnReader.ReadInt64(data, columnDefinition)),
			ColumnReaderKind.UnsignedInteger => checked((short) columnReader.ReadUInt64(data, columnDefinition)),
			ColumnReaderKind.Decimal => (short) columnReader.ReadDecimal(data, columnDefinition),
			ColumnReaderKind.Boolean => columnReader.ReadBoolean(data, columnDefinition) ? (short) 1 : (short) 0,
			_ => throw CreateInvalidCastException(ordinal, "Int16"),
		};
	}

	public int GetInt32(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "Int32", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.SignedInteger => checked((int) columnReader.ReadInt64(data, columnDefinition)),
			ColumnReaderKind.UnsignedInteger => checked((int) columnReader.ReadUInt64(data, columnDefinition)),
			ColumnReaderKind.Decimal => (int) columnReader.ReadDecimal(data, columnDefinition),
			ColumnReaderKind.Boolean => columnReader.ReadBoolean(data, columnDefinition) ? 1 : 0,
			_ => throw CreateInvalidCastException(ordinal, "Int32"),
		};
	}

	public long GetInt64(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "Int64", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.SignedInteger => columnReader.ReadInt64(data, columnDefinition),
			ColumnReaderKind.UnsignedInteger => checked((long) columnReader.ReadUInt64(data, columnDefinition)),
			ColumnReaderKind.Decimal => (long) columnReader.ReadDecimal(data, columnDefinition),
			ColumnReaderKind.Boolean => columnReader.ReadBoolean(data, columnDefinition) ? 1 : 0,
			_ => throw CreateInvalidCastException(ordinal, "Int64"),
		};
	}

	public ushort GetUInt16(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "UInt16", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.SignedInteger => checked((ushort) columnReader.ReadInt64(data, columnDefinition)),
			ColumnReaderKind.UnsignedInteger => checked((ushort) columnReader.ReadUInt64(data, columnDefinition)),
			ColumnReaderKind.Decimal => (ushort) columnReader.ReadDecimal(data, columnDefinition),
			ColumnReaderKind.Boolean => columnReader.ReadBoolean(data, columnDefinition) ? (ushort) 1 : (ushort) 0,
			_ => throw CreateInvalidCastException(ordinal, "UInt16"),
		};
	}

	public uint GetUInt32(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "UInt32", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.SignedInteger => checked((uint) columnReader.ReadInt64(data, columnDefinition)),
			ColumnReaderKind.UnsignedInteger => checked((uint) columnReader.ReadUInt64(data, columnDefinition)),
			ColumnReaderKind.Decimal => (uint) columnReader.ReadDecimal(data, columnDefinition),
			ColumnReaderKind.Boolean => columnReader.ReadBoolean(data, columnDefinition) ? 1u : 0,
			_ => throw CreateInvalidCastException(ordinal, "UInt32"),
		};
	}

	public ulong GetUInt64(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "UInt64", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.SignedInteger => checked((ulong) columnReader.ReadInt64(data, columnDefinition)),
			ColumnReaderKind.UnsignedInteger => columnReader.ReadUInt64(data, columnDefinition),
			ColumnReaderKind.Decimal => (ulong) columnReader.ReadDecimal(data, columnDefinition),
			ColumnReaderKind.Boolean => columnReader.ReadBoolean(data, columnDefinition) ? 1ul : 0,
			_ => throw CreateInvalidCastException(ordinal, "UInt64"),
		};
	}

	public DateTime GetDateTime(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "DateTime", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.DateTime => columnReader.ReadDateTime(data, columnDefinition),
			ColumnReaderKind.String => ParseDateTime(data),
			_ => throw CreateInvalidCastException(ordinal, "DateTime"),
		};
	}

	private DateTime ParseDateTime(ReadOnlySpan<byte> data) =>
		data.Length is >= 10 and <= 26 ?
			TextDateTimeColumnReader.ParseDateTime(data, Connection.ConvertZeroDateTime, Connection.AllowZeroDateTime, Connection.DateTimeKind, out var mySqlDateTime) ?? mySqlDateTime.GetDateTime() :
			throw new FormatException($"Couldn't interpret value as a valid DateTime: {Encoding.UTF8.GetString(data)}");

	public DateTimeOffset GetDateTimeOffset(int ordinal) => new(DateTime.SpecifyKind(GetDateTime(ordinal), DateTimeKind.Utc));

	public Stream GetStream(int ordinal)
	{
		CheckBinaryColumn(ordinal);
		var (offset, length) = m_dataOffsetLengths[ordinal];
#if NET11_0_OR_GREATER
		return new ReadOnlyMemoryStream(m_data.Slice(offset, length));
#else
		return MemoryMarshal.TryGetArray(m_data, out var arraySegment) ?
			new MemoryStream(arraySegment.Array!, arraySegment.Offset + offset, length, writable: false) :
			throw new InvalidOperationException("Can't get underlying array.");
#endif
	}

	public string GetString(int ordinal) => (string) GetValue(ordinal);

	public decimal GetDecimal(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "Decimal", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.Decimal => columnReader.ReadDecimal(data, columnDefinition),
			ColumnReaderKind.Double => (decimal) columnReader.ReadDouble(data, columnDefinition),
			ColumnReaderKind.Single => (decimal) columnReader.ReadSingle(data, columnDefinition),
			_ => throw CreateInvalidCastException(ordinal, "Decimal"),
		};
	}

	public double GetDouble(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "Double", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.Double => columnReader.ReadDouble(data, columnDefinition),
			ColumnReaderKind.Single => columnReader.ReadSingle(data, columnDefinition),
			ColumnReaderKind.Decimal => (double) columnReader.ReadDecimal(data, columnDefinition),
			_ => throw CreateInvalidCastException(ordinal, "Double"),
		};
	}

	public float GetFloat(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "Single", out var data, out var columnDefinition);
		return columnReader.Kind switch
		{
			ColumnReaderKind.Single => columnReader.ReadSingle(data, columnDefinition),
			ColumnReaderKind.Double => ToSingle(columnReader.ReadDouble(data, columnDefinition)),
			ColumnReaderKind.Decimal => (float) columnReader.ReadDecimal(data, columnDefinition),
			_ => throw CreateInvalidCastException(ordinal, "Single"),
		};

		// Loss of precision is expected, significant loss of information is not.
		// Use explicit range checks to guard against that.
		static float ToSingle(double value) =>
			value is >= float.MinValue and <= float.MaxValue ? (float) value : throw new InvalidCastException("The value cannot be safely cast to Single.");
	}

	public MySqlDateTime GetMySqlDateTime(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "MySqlDateTime", out var data, out var columnDefinition);
		return columnReader.Kind == ColumnReaderKind.DateTime ? columnReader.ReadMySqlDateTime(data, columnDefinition) :
			throw CreateInvalidCastException(ordinal, "MySqlDateTime");
	}

	public TimeSpan GetTimeSpan(int ordinal)
	{
		var columnReader = GetColumnReader(ordinal, "TimeSpan", out var data, out var columnDefinition);
		return columnReader.Kind == ColumnReaderKind.TimeSpan ? columnReader.ReadTimeSpan(data, columnDefinition) :
			throw CreateInvalidCastException(ordinal, "TimeSpan");
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
		var (offset, length) = m_dataOffsetLengths[ordinal];
		var data = m_data.Slice(offset, length).Span;
		var columnType = ResultSet.ColumnDefinitions![ordinal].ColumnType;
		if (columnType is ColumnType.NewDecimal or ColumnType.Decimal)
			return new MySqlDecimal(Encoding.UTF8.GetString(data));
		throw new InvalidCastException($"Can't convert {ResultSet.ColumnDefinitions![ordinal].ColumnType} to MySqlDecimal.");
	}

	public int GetValues(object[] values)
	{
		ArgumentNullException.ThrowIfNull(values);
		int count = Math.Min(values.Length, ResultSet.ColumnDefinitions!.Length);
		for (int i = 0; i < count; i++)
			values[i] = GetValue(i);
		return count;
	}

	public bool IsDBNull(int ordinal) => m_dataOffsetLengths[ordinal].Offset == -1;

	public object this[int ordinal] => GetValue(ordinal);

	public object this[string name] => GetValue(ResultSet.GetOrdinal(name));

	private ResultSet ResultSet { get; }
	private MySqlConnection Connection => ResultSet.Connection;

	// Returns the ColumnReader for the specified column, and the column's data; throws if the column is NULL.
	private ColumnReader GetColumnReader(int ordinal, string typeName, out ReadOnlySpan<byte> data, out ColumnDefinitionPayload columnDefinition)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(ordinal, ResultSet.ColumnDefinitions!.Length);

		var (offset, length) = m_dataOffsetLengths[ordinal];
		if (offset == -1)
			throw new InvalidCastException($"Can't convert NULL to {typeName}");

		data = m_data.Span.Slice(offset, length);
		columnDefinition = ResultSet.ColumnDefinitions[ordinal];
		return m_columnReaders[ordinal];
	}

	private InvalidCastException CreateInvalidCastException(int ordinal, string typeName) =>
		new($"Can't convert {ResultSet.GetColumnType(ordinal)} to {typeName}");

	private void CheckBinaryColumn(int ordinal)
	{
		if (m_dataOffsetLengths[ordinal].Offset == -1)
			throw new InvalidCastException("Column is NULL.");

		var column = ResultSet.ColumnDefinitions![ordinal];
		var columnType = column.ColumnType;
		if ((column.ColumnFlags & ColumnFlags.Binary) == 0 ||
			(columnType != ColumnType.String && columnType != ColumnType.VarString && columnType != ColumnType.TinyBlob &&
			columnType != ColumnType.Blob && columnType != ColumnType.MediumBlob && columnType != ColumnType.LongBlob &&
			columnType != ColumnType.Geometry && columnType != ColumnType.Vector))
		{
			throw new InvalidCastException($"Can't convert {columnType} to bytes.");
		}
	}

	private static void CheckBufferArguments<T>(long dataOffset, T[] buffer, int bufferOffset, int length)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(dataOffset);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(dataOffset, int.MaxValue);
		ArgumentOutOfRangeException.ThrowIfNegative(length);
		ArgumentOutOfRangeException.ThrowIfNegative(bufferOffset);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(bufferOffset, buffer.Length);
		if (checked(bufferOffset + length) > buffer.Length)
			throw new ArgumentException(nameof(bufferOffset) + " + " + nameof(length) + " cannot exceed " + nameof(buffer) + "." + nameof(buffer.Length), nameof(length));
	}

	private readonly struct OffsetLength(int offset, int length)
	{
		public static implicit operator OffsetLength((int Offset, int Length) x) => new(x.Offset, x.Length);

		public void Deconstruct(out int offset, out int length)
		{
			offset = Offset;
			length = Length;
		}

		public int Offset { get; } = offset;
		public int Length { get; } = length;
	}

	private readonly bool m_isBinary;
	private readonly OffsetLength[] m_dataOffsetLengths;
	private readonly ColumnReader[] m_columnReaders;
	private ReadOnlyMemory<byte> m_data;
}
