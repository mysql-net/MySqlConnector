using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;
#if NET8_0_OR_GREATER
using System.Text.Unicode;
#endif
using MySqlConnector.Core;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector;

public sealed class MySqlParameter : DbParameter, IDbDataParameter, ICloneable
{
	public MySqlParameter()
		: this(default(string?), default(object?))
	{
	}

	public MySqlParameter(string? name, object? value)
	{
		ResetDbType();
		m_name = name ?? "";
		NormalizedParameterName = NormalizeParameterName(m_name);
		Value = value;
		m_sourceColumn = "";
		SourceVersion = DataRowVersion.Current;
	}

	public MySqlParameter(string name, MySqlDbType mySqlDbType)
		: this(name, mySqlDbType, 0)
	{
	}

	public MySqlParameter(string name, MySqlDbType mySqlDbType, int size)
		: this(name, mySqlDbType, size, "")
	{
	}

	public MySqlParameter(string name, MySqlDbType mySqlDbType, int size, string sourceColumn)
	{
		m_name = name ?? "";
		NormalizedParameterName = NormalizeParameterName(m_name);
		MySqlDbType = mySqlDbType;
		Size = size;
		m_sourceColumn = sourceColumn ?? "";
		SourceVersion = DataRowVersion.Current;
	}

	public MySqlParameter(string name, MySqlDbType mySqlDbType, int size, ParameterDirection direction, bool isNullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value)
		: this(name, mySqlDbType, size, sourceColumn)
	{
		Direction = direction;
		IsNullable = isNullable;
		Precision = precision;
		Scale = scale;
		SourceVersion = sourceVersion;
		Value = value;
	}

	public override DbType DbType
	{
		get => m_dbType;
		set
		{
			m_dbType = value;
			m_mySqlDbType = TypeMapper.Instance.GetMySqlDbTypeForDbType(value);
			HasSetDbType = true;
		}
	}

	public MySqlDbType MySqlDbType
	{
		get => m_mySqlDbType;
		set
		{
			m_dbType = TypeMapper.Instance.GetDbTypeForMySqlDbType(value);
			m_mySqlDbType = value;
			HasSetDbType = true;
		}
	}

	public override ParameterDirection Direction
	{
		get => m_direction.GetValueOrDefault(ParameterDirection.Input);
		set
		{
			if (value != ParameterDirection.Input && value != ParameterDirection.Output &&
				value != ParameterDirection.InputOutput && value != ParameterDirection.ReturnValue)
			{
				throw new ArgumentOutOfRangeException(nameof(value), $"{value} is not a supported value for ParameterDirection");
			}
			m_direction = value;
		}
	}

	public override bool IsNullable { get; set; }
	public override byte Precision { get; set; }
	public override byte Scale { get; set; }

	[AllowNull]
	public override string ParameterName
	{
		get => m_name;
		set
		{
			m_name = value ?? "";
			var newNormalizedParameterName = value is null ? "" : NormalizeParameterName(m_name);
			ParameterCollection?.ChangeParameterName(this, NormalizedParameterName, newNormalizedParameterName);
			NormalizedParameterName = newNormalizedParameterName;
		}
	}

	public override int Size { get; set; }

	[AllowNull]
	public override string SourceColumn
	{
		get => m_sourceColumn;
		set => m_sourceColumn = value ?? "";
	}

	public override bool SourceColumnNullMapping { get; set; }

	public override DataRowVersion SourceVersion { get; set; }

	public override object? Value
	{
		get => m_value;
		set
		{
			m_value = value;
			if (!HasSetDbType && value is not null)
			{
				var typeMapping = TypeMapper.Instance.GetDbTypeMapping(value.GetType());
				if (typeMapping is not null)
				{
					m_dbType = typeMapping.DbTypes[0];
					m_mySqlDbType = TypeMapper.Instance.GetMySqlDbTypeForDbType(m_dbType);
				}
			}
		}
	}

	public override void ResetDbType()
	{
		m_mySqlDbType = MySqlDbType.VarChar;
		m_dbType = DbType.String;
		HasSetDbType = false;
	}

	public MySqlParameter Clone() => new MySqlParameter(this);

	object ICloneable.Clone() => Clone();

	internal MySqlParameter WithParameterName(string parameterName) => new MySqlParameter(this, parameterName);

	private MySqlParameter(MySqlParameter other)
	{
		m_dbType = other.m_dbType;
		m_mySqlDbType = other.m_mySqlDbType;
		m_direction = other.m_direction;
		HasSetDbType = other.HasSetDbType;
		IsNullable = other.IsNullable;
		Size = other.Size;
		m_name = other.m_name;
		NormalizedParameterName = other.NormalizedParameterName;
		m_value = other.m_value;
		Precision = other.Precision;
		Scale = other.Scale;
		m_sourceColumn = other.m_sourceColumn;
		SourceColumnNullMapping = other.SourceColumnNullMapping;
		SourceVersion = other.SourceVersion;
	}

	private MySqlParameter(MySqlParameter other, string parameterName)
		: this(other)
	{
		ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
	}

	internal bool HasSetDirection => m_direction.HasValue;

	internal bool HasSetDbType { get; set; }

	internal string NormalizedParameterName { get; private set; }

	internal MySqlParameterCollection? ParameterCollection { get; set; }

	internal void AppendSqlString(ByteBufferWriter writer, StatementPreparerOptions options)
	{
		var noBackslashEscapes = (options & StatementPreparerOptions.NoBackslashEscapes) == StatementPreparerOptions.NoBackslashEscapes;

		if (Value is null || Value == DBNull.Value)
		{
			ReadOnlySpan<byte> nullBytes = "NULL"u8;
			writer.Write(nullBytes);
		}
		else if (Value is string stringValue)
		{
			WriteString(writer, noBackslashEscapes, writeDelimiters: true, stringValue.AsSpan());
		}
		else if (Value is ReadOnlyMemory<char> readOnlyMemoryChar)
		{
			WriteString(writer, noBackslashEscapes, writeDelimiters: true, readOnlyMemoryChar.Span);
		}
		else if (Value is Memory<char> memoryChar)
		{
			WriteString(writer, noBackslashEscapes, writeDelimiters: true, memoryChar.Span);
		}
		else if (Value is char charValue)
		{
			writer.Write((byte) '\'');
			switch (charValue)
			{
			case '\0' when !noBackslashEscapes:
				writer.Write((ushort) 0x305C); // \0
				break;

			case '\'':
				writer.Write((ushort) 0x2727); // ''
				break;

			case '\\' when !noBackslashEscapes:
				writer.Write((ushort) 0x5C5C); // \\
				break;

			default:
				writer.Write(charValue.ToString());
				break;
			}
			writer.Write((byte) '\'');
		}
		else if (Value is byte byteValue)
		{
			Utf8Formatter.TryFormat(byteValue, writer.GetSpan(3), out var bytesWritten);
			writer.Advance(bytesWritten);
		}
		else if (Value is sbyte sbyteValue)
		{
			Utf8Formatter.TryFormat(sbyteValue, writer.GetSpan(4), out var bytesWritten);
			writer.Advance(bytesWritten);
		}
		else if (Value is decimal decimalValue)
		{
#if NET8_0_OR_GREATER
			decimalValue.TryFormat(writer.GetSpan(31), out var bytesWritten, default, CultureInfo.InvariantCulture);
			writer.Advance(bytesWritten);
#else
			writer.WriteAscii(decimalValue.ToString(CultureInfo.InvariantCulture));
#endif
		}
		else if (Value is short shortValue)
		{
			writer.WriteString(shortValue);
		}
		else if (Value is ushort ushortValue)
		{
			writer.WriteString(ushortValue);
		}
		else if (Value is int intValue)
		{
			writer.WriteString(intValue);
		}
		else if (Value is uint uintValue)
		{
			writer.WriteString(uintValue);
		}
		else if (Value is long longValue)
		{
			writer.WriteString(longValue);
		}
		else if (Value is ulong ulongValue)
		{
			writer.WriteString(ulongValue);
		}
		else if (Value is byte[] or ReadOnlyMemory<byte> or Memory<byte> or ArraySegment<byte> or MySqlGeometry or MemoryStream)
		{
			var inputSpan = Value switch
			{
				byte[] byteArray => byteArray.AsSpan(),
				ArraySegment<byte> arraySegment => arraySegment.AsSpan(),
				Memory<byte> memory => memory.Span,
				MySqlGeometry geometry => geometry.ValueSpan,
				MemoryStream memoryStream => memoryStream.TryGetBuffer(out var streamBuffer) ? streamBuffer.AsSpan() : memoryStream.ToArray().AsSpan(),
				_ => ((ReadOnlyMemory<byte>) Value).Span,
			};

			// determine the number of bytes to be written
			var length = inputSpan.Length + BinaryBytes.Length + 1;
			foreach (var by in inputSpan)
			{
				if (by is 0x27 || by is 0x00 or 0x5C && !noBackslashEscapes)
					length++;
			}

			var outputSpan = writer.GetSpan(length);
			BinaryBytes.CopyTo(outputSpan);
			var index = BinaryBytes.Length;
			foreach (var by in inputSpan)
			{
				if (by == 0x00 && !noBackslashEscapes)
				{
					// \0
					outputSpan[index++] = 0x5C;
					outputSpan[index++] = 0x30;
				}
				else
				{
					if (by is 0x27 || by is 0x5C && !noBackslashEscapes)
						outputSpan[index++] = by;
					outputSpan[index++] = by;
				}
			}
			outputSpan[index++] = 0x27;
			Debug.Assert(index == length, "index == length");
			writer.Advance(index);
		}
		else if (Value is bool boolValue)
		{
			ReadOnlySpan<byte> trueBytes = "true"u8;
			ReadOnlySpan<byte> falseBytes = "false"u8;
			writer.Write(boolValue ? trueBytes : falseBytes);
		}
		else if (Value is float floatValue)
		{
#if NET8_0_OR_GREATER
			floatValue.TryFormat(writer.GetSpan(14), out var bytesWritten, "R", CultureInfo.InvariantCulture);
			writer.Advance(bytesWritten);
#else
			// NOTE: Utf8Formatter doesn't support "R"
			writer.WriteAscii(floatValue.ToString("R", CultureInfo.InvariantCulture));
#endif
		}
		else if (Value is double doubleValue)
		{
#if NET8_0_OR_GREATER
			doubleValue.TryFormat(writer.GetSpan(24), out var bytesWritten, "R", CultureInfo.InvariantCulture);
			writer.Advance(bytesWritten);
#else
			// NOTE: Utf8Formatter doesn't support "R"
			writer.WriteAscii(doubleValue.ToString("R", CultureInfo.InvariantCulture));
#endif
		}
		else if (Value is BigInteger bigInteger)
		{
			writer.WriteAscii(bigInteger.ToString(CultureInfo.InvariantCulture));
		}
		else if (Value is MySqlDecimal mySqlDecimal)
		{
			writer.WriteAscii(mySqlDecimal.ToString());
		}
		else if (Value is MySqlDateTime mySqlDateTimeValue)
		{
			if (mySqlDateTimeValue.IsValidDateTime)
			{
#if NET8_0_OR_GREATER
				Utf8.TryWrite(writer.GetSpan(39), CultureInfo.InvariantCulture, $"timestamp('{mySqlDateTimeValue.GetDateTime():yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')", out var bytesWritten);
				writer.Advance(bytesWritten);
#else
#if NET6_0_OR_GREATER
				var str = string.Create(CultureInfo.InvariantCulture, stackalloc char[39], $"timestamp('{mySqlDateTimeValue.GetDateTime():yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')");
#else
				var str = FormattableString.Invariant($"timestamp('{mySqlDateTimeValue.GetDateTime():yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')");
#endif
				writer.WriteAscii(str);
#endif
			}
			else
			{
				writer.Write("timestamp('0000-00-00')"u8);
			}
		}
#if NET6_0_OR_GREATER
		else if (Value is DateOnly dateOnlyValue)
		{
#if NET8_0_OR_GREATER
			Utf8.TryWrite(writer.GetSpan(23), CultureInfo.InvariantCulture, $"timestamp('{dateOnlyValue:yyyy'-'MM'-'dd}')", out var bytesWritten);
			writer.Advance(bytesWritten);
#else
			writer.WriteAscii(string.Create(CultureInfo.InvariantCulture, stackalloc char[23], $"timestamp('{dateOnlyValue:yyyy'-'MM'-'dd}')"));
#endif
		}
#endif
		else if (Value is DateTime dateTimeValue)
		{
			if ((options & StatementPreparerOptions.DateTimeUtc) != 0 && dateTimeValue.Kind == DateTimeKind.Local)
				throw new MySqlException($"DateTime.Kind must not be Local when DateTimeKind setting is Utc (parameter name: {ParameterName})");
			else if ((options & StatementPreparerOptions.DateTimeLocal) != 0 && dateTimeValue.Kind == DateTimeKind.Utc)
				throw new MySqlException($"DateTime.Kind must not be Utc when DateTimeKind setting is Local (parameter name: {ParameterName})");

#if NET8_0_OR_GREATER
			Utf8.TryWrite(writer.GetSpan(39), CultureInfo.InvariantCulture, $"timestamp('{dateTimeValue:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')", out var bytesWritten);
			writer.Advance(bytesWritten);
#else
#if NET6_0_OR_GREATER
			var str = string.Create(CultureInfo.InvariantCulture, stackalloc char[39], $"timestamp('{dateTimeValue:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')");
#else
			var str = FormattableString.Invariant($"timestamp('{dateTimeValue:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')");
#endif
			writer.WriteAscii(str);
#endif
		}
		else if (Value is DateTimeOffset dateTimeOffsetValue)
		{
			// store as UTC as it will be read as such when deserialized from a timespan column
#if NET8_0_OR_GREATER
			Utf8.TryWrite(writer.GetSpan(39), CultureInfo.InvariantCulture, $"timestamp('{dateTimeOffsetValue.UtcDateTime:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')", out var bytesWritten);
			writer.Advance(bytesWritten);
#else
#if NET6_0_OR_GREATER
			var str = string.Create(CultureInfo.InvariantCulture, stackalloc char[39], $"timestamp('{dateTimeOffsetValue.UtcDateTime:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')");
#else
			var str = FormattableString.Invariant($"timestamp('{dateTimeOffsetValue.UtcDateTime:yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff}')");
#endif
			writer.WriteAscii(str);
#endif
		}
#if NET6_0_OR_GREATER
		else if (Value is TimeOnly timeOnlyValue)
		{
#if NET8_0_OR_GREATER
			Utf8.TryWrite(writer.GetSpan(22), CultureInfo.InvariantCulture, $"time '{timeOnlyValue:HH':'mm':'ss'.'ffffff}'", out var bytesWritten);
			writer.Advance(bytesWritten);
#else
			writer.WriteAscii(string.Create(CultureInfo.InvariantCulture, stackalloc char[22], $"time '{timeOnlyValue:HH':'mm':'ss'.'ffffff}'"));
#endif
		}
#endif
		else if (Value is TimeSpan ts)
		{
			writer.Write("time '"u8);
			if (ts.Ticks < 0)
			{
				writer.Write((byte) '-');
				ts = TimeSpan.FromTicks(-ts.Ticks);
			}
#if NET8_0_OR_GREATER
			Utf8.TryWrite(writer.GetSpan(17), CultureInfo.InvariantCulture, $"{ts.Days * 24 + ts.Hours}:{ts:mm':'ss'.'ffffff}'", out var bytesWritten);
			writer.Advance(bytesWritten);
#else
#if NET6_0_OR_GREATER
			var str = string.Create(CultureInfo.InvariantCulture, stackalloc char[17], $"{ts.Days * 24 + ts.Hours}:{ts:mm':'ss'.'ffffff}'");
#else
			var str = FormattableString.Invariant($"{ts.Days * 24 + ts.Hours}:{ts:mm':'ss'.'ffffff}'");
#endif
			writer.WriteAscii(str);
#endif
		}
		else if (Value is Guid guidValue)
		{
			StatementPreparerOptions guidOptions = options & StatementPreparerOptions.GuidFormatMask;
			if (guidOptions is StatementPreparerOptions.GuidFormatBinary16 or StatementPreparerOptions.GuidFormatTimeSwapBinary16 or StatementPreparerOptions.GuidFormatLittleEndianBinary16)
			{
				var bytes = guidValue.ToByteArray();
				if (guidOptions != StatementPreparerOptions.GuidFormatLittleEndianBinary16)
				{
					Utility.SwapBytes(bytes, 0, 3);
					Utility.SwapBytes(bytes, 1, 2);
					Utility.SwapBytes(bytes, 4, 5);
					Utility.SwapBytes(bytes, 6, 7);

					if (guidOptions == StatementPreparerOptions.GuidFormatTimeSwapBinary16)
					{
						Utility.SwapBytes(bytes, 0, 4);
						Utility.SwapBytes(bytes, 1, 5);
						Utility.SwapBytes(bytes, 2, 6);
						Utility.SwapBytes(bytes, 3, 7);
						Utility.SwapBytes(bytes, 0, 2);
						Utility.SwapBytes(bytes, 1, 3);
					}
				}
				writer.Write(BinaryBytes);
				foreach (var by in bytes)
				{
					if (by is 0x00 or 0x27 or 0x5C)
						writer.Write((byte) 0x5C);
					writer.Write(by == 0 ? (byte) 0x30 : by);
				}
				writer.Write((byte) '\'');
			}
			else
			{
				var is32Characters = guidOptions == StatementPreparerOptions.GuidFormatChar32;
				var guidLength = is32Characters ? 34 : 38;
				var span = writer.GetSpan(guidLength);
				span[0] = 0x27;
				Utf8Formatter.TryFormat(guidValue, span[1..], out _, is32Characters ? 'N' : 'D');
				span[guidLength - 1] = 0x27;
				writer.Advance(guidLength);
			}
		}
		else if (Value is StringBuilder stringBuilder)
		{
#if NETCOREAPP3_1_OR_GREATER
			writer.Write((byte) '\'');
			foreach (var chunk in stringBuilder.GetChunks())
				WriteString(writer, noBackslashEscapes, writeDelimiters: false, chunk.Span);
			if (stringBuilder.Length != 0)
				writer.Write("".AsSpan(), flush: true);
			writer.Write((byte) '\'');
#else
			WriteString(writer, noBackslashEscapes, writeDelimiters: true, stringBuilder.ToString().AsSpan());
#endif
		}
		else if (MySqlDbType == MySqlDbType.Int16)
		{
			writer.WriteString((short) Value);
		}
		else if (MySqlDbType == MySqlDbType.UInt16)
		{
			writer.WriteString((ushort) Value);
		}
		else if (MySqlDbType == MySqlDbType.Int32)
		{
			writer.WriteString((int) Value);
		}
		else if (MySqlDbType == MySqlDbType.UInt32)
		{
			writer.WriteString((uint) Value);
		}
		else if (MySqlDbType == MySqlDbType.Int64)
		{
			writer.WriteString((long) Value);
		}
		else if (MySqlDbType == MySqlDbType.UInt64)
		{
			writer.WriteString((ulong) Value);
		}
		else if ((MySqlDbType is MySqlDbType.String or MySqlDbType.VarChar) && HasSetDbType && Value is Enum stringEnumValue)
		{
			writer.Write((byte) '\'');
			writer.Write(stringEnumValue.ToString("G"));
			writer.Write((byte) '\'');
		}
		else if (Value is Enum enumValue)
		{
			writer.Write(enumValue.ToString("d"));
		}
		else
		{
			throw new NotSupportedException($"Parameter type {Value.GetType().Name} is not supported; see https://fl.vu/mysql-param-type. Value: {Value}");
		}

		static void WriteString(ByteBufferWriter writer, bool noBackslashEscapes, bool writeDelimiters, ReadOnlySpan<char> value)
		{
			if (writeDelimiters)
				writer.Write((byte) '\'');

			var charsWritten = 0;
			while (charsWritten < value.Length)
			{
				var remainingValue = value[charsWritten..];
				var nextDelimiterIndex = remainingValue.IndexOfAny('\0', '\'', '\\');
				if (nextDelimiterIndex == -1)
				{
					// write the rest of the string
					writer.Write(remainingValue, flush: writeDelimiters);
					charsWritten += remainingValue.Length;
				}
				else
				{
					// write up to (and including) the delimiter, then double it
					writer.Write(remainingValue[..nextDelimiterIndex], flush: true);
					if (remainingValue[nextDelimiterIndex] == '\\' && !noBackslashEscapes)
						writer.Write((ushort) 0x5C5C); // \\
					else if (remainingValue[nextDelimiterIndex] == '\\' && noBackslashEscapes)
						writer.Write((byte) 0x5C); // \
					else if (remainingValue[nextDelimiterIndex] == '\'')
						writer.Write((ushort) 0x2727); // ''
					else if (remainingValue[nextDelimiterIndex] == '\0' && !noBackslashEscapes)
						writer.Write((ushort) 0x305C); // \0
					else if (remainingValue[nextDelimiterIndex] == '\0' && noBackslashEscapes)
						writer.Write((byte) 0x00); // (nul)
					charsWritten += nextDelimiterIndex + 1;
				}
			}

			if (writeDelimiters)
				writer.Write((byte) '\'');
		}
	}

	internal void AppendBinary(ByteBufferWriter writer, StatementPreparerOptions options)
	{
		if (Value is null || Value == DBNull.Value)
		{
			// stored in "null bitmap" only
		}
		else if (Value is string stringValue)
		{
			writer.WriteLengthEncodedString(stringValue);
		}
		else if (Value is char charValue)
		{
			writer.WriteLengthEncodedString(charValue.ToString());
		}
		else if (Value is sbyte sbyteValue)
		{
			writer.Write(unchecked((byte) sbyteValue));
		}
		else if (Value is byte byteValue)
		{
			writer.Write(byteValue);
		}
		else if (Value is bool boolValue)
		{
			writer.Write((byte) (boolValue ? 1 : 0));
		}
		else if (Value is short shortValue)
		{
			writer.Write(unchecked((ushort) shortValue));
		}
		else if (Value is ushort ushortValue)
		{
			writer.Write(ushortValue);
		}
		else if (Value is int intValue)
		{
			writer.Write(intValue);
		}
		else if (Value is uint uintValue)
		{
			writer.Write(uintValue);
		}
		else if (Value is long longValue)
		{
			writer.Write(unchecked((ulong) longValue));
		}
		else if (Value is ulong ulongValue)
		{
			writer.Write(ulongValue);
		}
		else if (Value is byte[] byteArrayValue)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) byteArrayValue.Length));
			writer.Write(byteArrayValue);
		}
		else if (Value is ReadOnlyMemory<byte> readOnlyMemoryValue)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) readOnlyMemoryValue.Length));
			writer.Write(readOnlyMemoryValue.Span);
		}
		else if (Value is Memory<byte> memoryValue)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) memoryValue.Length));
			writer.Write(memoryValue.Span);
		}
		else if (Value is ArraySegment<byte> arraySegmentValue)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) arraySegmentValue.Count));
			writer.Write(arraySegmentValue);
		}
		else if (Value is MySqlGeometry geometry)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) geometry.ValueSpan.Length));
			writer.Write(geometry.ValueSpan);
		}
		else if (Value is MemoryStream memoryStream)
		{
			if (!memoryStream.TryGetBuffer(out var streamBuffer))
				streamBuffer = new ArraySegment<byte>(memoryStream.ToArray());
			writer.WriteLengthEncodedInteger(unchecked((ulong) streamBuffer.Count));
			writer.Write(streamBuffer);
		}
		else if (Value is float floatValue)
		{
			writer.Write(BitConverter.GetBytes(floatValue));
		}
		else if (Value is double doubleValue)
		{
			writer.Write(unchecked((ulong) BitConverter.DoubleToInt64Bits(doubleValue)));
		}
		else if (Value is decimal decimalValue)
		{
			writer.WriteLengthEncodedAsciiString(decimalValue.ToString(CultureInfo.InvariantCulture));
		}
		else if (Value is BigInteger bigInteger)
		{
			writer.WriteLengthEncodedAsciiString(bigInteger.ToString(CultureInfo.InvariantCulture));
		}
		else if (Value is MySqlDateTime mySqlDateTimeValue)
		{
			if (mySqlDateTimeValue.IsValidDateTime)
				WriteDateTime(writer, mySqlDateTimeValue.GetDateTime());
			else
				writer.Write((byte) 0);
		}
		else if (Value is MySqlDecimal mySqlDecimal)
		{
			writer.WriteLengthEncodedAsciiString(mySqlDecimal.ToString());
		}
#if NET6_0_OR_GREATER
		else if (Value is DateOnly dateOnlyValue)
		{
			WriteDateOnly(writer, dateOnlyValue);
		}
#endif
		else if (Value is DateTime dateTimeValue)
		{
			if ((options & StatementPreparerOptions.DateTimeUtc) != 0 && dateTimeValue.Kind == DateTimeKind.Local)
				throw new MySqlException($"DateTime.Kind must not be Local when DateTimeKind setting is Utc (parameter name: {ParameterName})");
			else if ((options & StatementPreparerOptions.DateTimeLocal) != 0 && dateTimeValue.Kind == DateTimeKind.Utc)
				throw new MySqlException($"DateTime.Kind must not be Utc when DateTimeKind setting is Local (parameter name: {ParameterName})");

			WriteDateTime(writer, dateTimeValue);
		}
		else if (Value is DateTimeOffset dateTimeOffsetValue)
		{
			// store as UTC as it will be read as such when deserialized from a timespan column
			WriteDateTime(writer, dateTimeOffsetValue.UtcDateTime);
		}
#if NET6_0_OR_GREATER
		else if (Value is TimeOnly timeOnlyValue)
		{
			WriteTime(writer, timeOnlyValue.ToTimeSpan());
		}
#endif
		else if (Value is TimeSpan ts)
		{
			WriteTime(writer, ts);
		}
		else if (Value is Guid guidValue)
		{
			StatementPreparerOptions guidOptions = options & StatementPreparerOptions.GuidFormatMask;
			if (guidOptions is StatementPreparerOptions.GuidFormatBinary16 or StatementPreparerOptions.GuidFormatTimeSwapBinary16 or StatementPreparerOptions.GuidFormatLittleEndianBinary16)
			{
				var bytes = guidValue.ToByteArray();
				if (guidOptions != StatementPreparerOptions.GuidFormatLittleEndianBinary16)
				{
					Utility.SwapBytes(bytes, 0, 3);
					Utility.SwapBytes(bytes, 1, 2);
					Utility.SwapBytes(bytes, 4, 5);
					Utility.SwapBytes(bytes, 6, 7);

					if (guidOptions == StatementPreparerOptions.GuidFormatTimeSwapBinary16)
					{
						Utility.SwapBytes(bytes, 0, 4);
						Utility.SwapBytes(bytes, 1, 5);
						Utility.SwapBytes(bytes, 2, 6);
						Utility.SwapBytes(bytes, 3, 7);
						Utility.SwapBytes(bytes, 0, 2);
						Utility.SwapBytes(bytes, 1, 3);
					}
				}
				writer.Write((byte) 16);
				writer.Write(bytes);
			}
			else
			{
				var is32Characters = guidOptions == StatementPreparerOptions.GuidFormatChar32;
				var guidLength = is32Characters ? 32 : 36;
				writer.Write((byte) guidLength);
				var span = writer.GetSpan(guidLength);
				Utf8Formatter.TryFormat(guidValue, span, out _, is32Characters ? 'N' : 'D');
				writer.Advance(guidLength);
			}
		}
		else if (Value is ReadOnlyMemory<char> readOnlyMemoryChar)
		{
			writer.WriteLengthEncodedString(readOnlyMemoryChar.Span);
		}
		else if (Value is Memory<char> memoryChar)
		{
			writer.WriteLengthEncodedString(memoryChar.Span);
		}
		else if (Value is StringBuilder stringBuilder)
		{
			writer.WriteLengthEncodedString(stringBuilder);
		}
		else if (MySqlDbType == MySqlDbType.Int16)
		{
			writer.Write((ushort) (short) Value);
		}
		else if (MySqlDbType == MySqlDbType.UInt16)
		{
			writer.Write((ushort) Value);
		}
		else if (MySqlDbType == MySqlDbType.Int32)
		{
			writer.Write((int) Value);
		}
		else if (MySqlDbType == MySqlDbType.UInt32)
		{
			writer.Write((uint) Value);
		}
		else if (MySqlDbType == MySqlDbType.Int64)
		{
			writer.Write((ulong) (long) Value);
		}
		else if (MySqlDbType == MySqlDbType.UInt64)
		{
			writer.Write((ulong) Value);
		}
		else if ((MySqlDbType is MySqlDbType.String or MySqlDbType.VarChar) && HasSetDbType && Value is Enum stringEnumValue)
		{
			writer.WriteLengthEncodedString(stringEnumValue.ToString("G"));
		}
		else if (Value is Enum)
		{
			writer.Write(Convert.ToInt32(Value, CultureInfo.InvariantCulture));
		}
		else
		{
			throw new NotSupportedException($"Parameter type {Value.GetType().Name} is not supported; see https://fl.vu/mysql-param-type. Value: {Value}");
		}
	}

	internal static string NormalizeParameterName(string name) =>
		name.Trim() switch
		{
			['@' or '?', '`', .. var middle, '`'] => middle.Replace("``", "`"),
			['@' or '?', '\'', .. var middle, '\''] => middle.Replace("''", "'"),
			['@' or '?', '"', .. var middle, '"'] => middle.Replace("\"\"", "\""),
			['@' or '?', .. var rest] => rest,
			{ } other => other,
		};

#if NET6_0_OR_GREATER
	private static void WriteDateOnly(ByteBufferWriter writer, DateOnly dateOnly)
	{
		writer.Write((byte) 4);
		writer.Write((ushort) dateOnly.Year);
		writer.Write((byte) dateOnly.Month);
		writer.Write((byte) dateOnly.Day);
	}
#endif

	private static void WriteDateTime(ByteBufferWriter writer, DateTime dateTime)
	{
		byte length;
		var microseconds = (int) (dateTime.Ticks % 10_000_000) / 10;
		if (microseconds != 0)
			length = 11;
		else if (dateTime.Hour != 0 || dateTime.Minute != 0 || dateTime.Second != 0)
			length = 7;
		else
			length = 4;
		writer.Write(length);
		writer.Write((ushort) dateTime.Year);
		writer.Write((byte) dateTime.Month);
		writer.Write((byte) dateTime.Day);
		if (length > 4)
		{
			writer.Write((byte) dateTime.Hour);
			writer.Write((byte) dateTime.Minute);
			writer.Write((byte) dateTime.Second);
			if (length > 7)
			{
				writer.Write(microseconds);
			}
		}
	}

	private static void WriteTime(ByteBufferWriter writer, TimeSpan timeSpan)
	{
		var ticks = timeSpan.Ticks;
		if (ticks == 0)
		{
			writer.Write((byte) 0);
		}
		else
		{
			if (ticks < 0)
				timeSpan = TimeSpan.FromTicks(-ticks);
			var microseconds = (int) (timeSpan.Ticks % 10_000_000) / 10;
			writer.Write((byte) (microseconds == 0 ? 8 : 12));
			writer.Write((byte) (ticks < 0 ? 1 : 0));
			writer.Write(timeSpan.Days);
			writer.Write((byte) timeSpan.Hours);
			writer.Write((byte) timeSpan.Minutes);
			writer.Write((byte) timeSpan.Seconds);
			if (microseconds != 0)
				writer.Write(microseconds);
		}
	}

	private static ReadOnlySpan<byte> BinaryBytes => "_binary'"u8;

	private DbType m_dbType;
	private MySqlDbType m_mySqlDbType;
	private string m_name;
	private ParameterDirection? m_direction;
	private string m_sourceColumn;
	private object? m_value;
}
