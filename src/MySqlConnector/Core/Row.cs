using System;
using System.Buffers.Text;
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
			return value switch
			{
				bool b => b,
				sbyte by => (@by != 0),
				byte b => (b != 0),
				short s => (s != 0),
				ushort us => (us != 0),
				int i => (i != 0),
				uint u => (u != 0),
				long l => (l != 0),
				ulong ul => (ul != 0),
				decimal d => (d != 0),
				_ => (bool) value
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
				_ => (sbyte) value
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
				_ => (byte) value
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

			return value switch
			{
				string stringValue when Guid.TryParse(stringValue, out guid) => guid,
				byte[] bytes when bytes.Length == 16 => CreateGuidFromBytes(Connection.GuidFormat, bytes),
				_ => throw new InvalidCastException(
					"The value could not be converted to a GUID: {0}".FormatInvariant(value))
			};
		}

		public short GetInt16(int ordinal)
		{
			var value = GetValue(ordinal);
			return value switch
			{
				short s => s,
				sbyte s => s,
				byte b => b,
				ushort u => checked((short) u),
				int i => checked((short) i),
				uint u => checked((short) u),
				long l => checked((short) l),
				ulong u => checked((short) u),
				decimal d => (short) d,
				_ => (short) value
			};
		}

		public int GetInt32(int ordinal)
		{
			var value = GetValue(ordinal);
			return value switch
			{
				int i => i,
				sbyte s => s,
				byte b => b,
				short s => s,
				ushort u => u,
				uint u => checked((int) u),
				long l => checked((int) l),
				ulong u => checked((int) u),
				decimal d => (int) d,
				_ => (int) value
			};
		}

		public long GetInt64(int ordinal)
		{
			var value = GetValue(ordinal);
			return value switch
			{
				long l => l,
				sbyte s => s,
				byte b => b,
				short s => s,
				ushort u => u,
				int i => i,
				uint u => u,
				ulong u => checked((long) u),
				decimal d => (long) d,
				_ => (long) value
			};
		}

		public ushort GetUInt16(int ordinal)
		{
			var value = GetValue(ordinal);
			return value switch
			{
				ushort u => u,
				sbyte s => checked((ushort) s),
				byte b => b,
				short s => checked((ushort) s),
				int i => checked((ushort) i),
				uint u => checked((ushort) u),
				long l => checked((ushort) l),
				ulong u => checked((ushort) u),
				decimal d => (ushort) d,
				_ => (ushort) value
			};
		}

		public uint GetUInt32(int ordinal)
		{
			var value = GetValue(ordinal);
			return value switch
			{
				uint u => u,
				sbyte s => checked((uint) s),
				byte b => b,
				short s => checked((uint) s),
				ushort u => u,
				int i => checked((uint) i),
				long l => checked((uint) l),
				ulong u => checked((uint) u),
				decimal d => (uint) d,
				_ => (uint) value
			};
		}

		public ulong GetUInt64(int ordinal)
		{
			var value = GetValue(ordinal);
			return value switch
			{
				ulong u => u,
				sbyte s => checked((ulong) s),
				byte b => b,
				short s => checked((ulong) s),
				ushort u => u,
				int i => checked((ulong) i),
				uint u => u,
				long l => checked((ulong) l),
				decimal d => (ulong) d,
				_ => (ulong) value
			};
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
				new MemoryStream(arraySegment.Array! ?? throw new InvalidOperationException(), arraySegment.Offset + m_dataOffsets[ordinal], m_dataLengths[ordinal], writable: false) :
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

			// Loss of precision is expected, significant loss of information is not.
			// Use explicit range checks to guard against that.
			return value switch
			{
				double doubleValue => (doubleValue >= float.MinValue && doubleValue <= float.MaxValue ? (float) doubleValue : throw new InvalidCastException("The value cannot be safely cast to Single.")),
				decimal decimalValue => (float) decimalValue,
				_ => (float) value
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
			throw new InvalidCastException("Can't convert {0} to MySqlGeometry.".FormatInvariant(ResultSet.ColumnDefinitions![ordinal].ColumnType));
		}

		public int GetValues(object[] values)
		{
			var count = Math.Min((values ?? throw new ArgumentNullException(nameof(values))).Length, ResultSet.ColumnDefinitions!.Length);
			for (var i = 0; i < count; i++)
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
			switch (guidFormat)
			{
			case MySqlGuidFormat.Binary16:
				return new Guid(new[] { bytes[3], bytes[2], bytes[1], bytes[0], bytes[5], bytes[4], bytes[7], bytes[6], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] });
			case MySqlGuidFormat.TimeSwapBinary16:
				return new Guid(new[] { bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2], bytes[1], bytes[0], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] });
			default:
				return new Guid(bytes.ToArray());
#else
			unsafe
			{
				if (guidFormat == MySqlGuidFormat.Binary16)
					return new Guid(stackalloc byte[16] { bytes[3], bytes[2], bytes[1], bytes[0], bytes[5], bytes[4], bytes[7], bytes[6], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] });
				if (guidFormat == MySqlGuidFormat.TimeSwapBinary16)
					return new Guid(stackalloc byte[16] { bytes[7], bytes[6], bytes[5], bytes[4], bytes[3], bytes[2], bytes[1], bytes[0], bytes[8], bytes[9], bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15] });
				return new Guid(bytes);
			}
#endif
			}
		}

		protected static object ReadBit(ReadOnlySpan<byte> data, ColumnFlags columnFlags)
		{
			if ((columnFlags & ColumnFlags.Binary) == 0)
			{
				// when the Binary flag IS NOT set, the BIT column is transmitted as MSB byte array
				ulong bitValue = 0;
				foreach (var d in data)
					bitValue = bitValue * 256 + d;

				return bitValue;
			}
			else
			{
				// when the Binary flag IS set, the BIT column is transmitted as text
				return ParseUInt64(data);
			}
		}

		protected static ulong ParseUInt64(ReadOnlySpan<byte> data) =>
			!Utf8Parser.TryParse(data, out ulong value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

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
		private readonly int[] m_dataOffsets;
		private readonly int[] m_dataLengths;
	}
}
