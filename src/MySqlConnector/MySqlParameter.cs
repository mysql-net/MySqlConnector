using System.Buffers.Binary;
using System.Buffers.Text;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
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
		SourceColumn = "";
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
		SourceColumn = sourceColumn ?? "";
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

	[DbProviderSpecificTypeProperty(true)]
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
			if (value is not (ParameterDirection.Input or ParameterDirection.Output or
				ParameterDirection.InputOutput or ParameterDirection.ReturnValue))
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
		get;
		set => field = value ?? "";
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

	public MySqlParameter Clone() => new(this);

	object ICloneable.Clone() => Clone();

	internal MySqlParameter WithParameterName(string parameterName) => new(this, parameterName);

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
		SourceColumn = other.SourceColumn;
		SourceColumnNullMapping = other.SourceColumnNullMapping;
		SourceVersion = other.SourceVersion;
	}

	private MySqlParameter(MySqlParameter other, string parameterName)
		: this(other)
	{
		ArgumentNullException.ThrowIfNull(parameterName);
		ParameterName = parameterName;
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
			writer.Write("NULL"u8);
		}
		else if (Value is string stringValue)
		{
			WriteString(writer, noBackslashEscapes, stringValue.AsSpan());
		}
		else if (Value is ReadOnlyMemory<char> readOnlyMemoryChar)
		{
			WriteString(writer, noBackslashEscapes, readOnlyMemoryChar.Span);
		}
		else if (Value is Memory<char> memoryChar)
		{
			WriteString(writer, noBackslashEscapes, memoryChar.Span);
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
		else if (Value is byte[] or ReadOnlyMemory<byte> or Memory<byte> or ArraySegment<byte> or MySqlGeometry or MemoryStream or float[] or ReadOnlyMemory<float> or Memory<float>)
		{
			var inputSpan = Value switch
			{
				byte[] byteArray => byteArray.AsSpan(),
				ArraySegment<byte> arraySegment => arraySegment.AsSpan(),
				Memory<byte> memory => memory.Span,
				MySqlGeometry geometry => geometry.ValueSpan,
				MemoryStream memoryStream => memoryStream.TryGetBuffer(out var streamBuffer) ? streamBuffer.AsSpan() : memoryStream.ToArray().AsSpan(),
				float[] floatArray => ConvertFloatsToBytes(floatArray.AsSpan()),
				Memory<float> memory => ConvertFloatsToBytes(memory.Span),
				ReadOnlyMemory<float> memory => ConvertFloatsToBytes(memory.Span),
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
		else if (Value is Stream)
		{
			throw new NotSupportedException($"Parameter type {Value.GetType().Name} can only be used after calling MySqlCommand.Prepare.");
		}
		else if (Value is bool boolValue)
		{
			writer.Write(boolValue ? "true"u8 : "false"u8);
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
				Span<byte> bytes = stackalloc byte[16];
#if NET8_0_OR_GREATER
				guidValue.TryWriteBytes(bytes, bigEndian: guidOptions != StatementPreparerOptions.GuidFormatLittleEndianBinary16, out _);
#else
				guidValue.TryWriteBytes(bytes);
#endif
#else
				var bytes = guidValue.ToByteArray();
#endif
				if (guidOptions != StatementPreparerOptions.GuidFormatLittleEndianBinary16)
				{
#if !NET8_0_OR_GREATER
					Utility.SwapBytes(bytes, 0, 3);
					Utility.SwapBytes(bytes, 1, 2);
					Utility.SwapBytes(bytes, 4, 5);
					Utility.SwapBytes(bytes, 6, 7);
#endif

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
				WriteStringChunk(writer, noBackslashEscapes, chunk.Span);
			if (stringBuilder.Length != 0)
				writer.Write("".AsSpan(), flush: true);
			writer.Write((byte) '\'');
#else
			WriteString(writer, noBackslashEscapes, stringBuilder.ToString().AsSpan());
#endif
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
		else
		{
			throw new NotSupportedException($"Parameter type {Value.GetType().Name} is not supported; see https://mysqlconnector.net/param-type. Value: {Value}");
		}

		static void WriteString(ByteBufferWriter writer, bool noBackslashEscapes, ReadOnlySpan<char> value)
		{
			writer.Write((byte) '\'');

			var charsWritten = 0;
			while (charsWritten < value.Length)
			{
				var remainingValue = value[charsWritten..];
				var nextDelimiterIndex = remainingValue.IndexOfAny('\0', '\'', '\\');
				if (nextDelimiterIndex == -1)
				{
					// write the rest of the string
					writer.Write(remainingValue);
					charsWritten += remainingValue.Length;
				}
				else
				{
					// write up to (and including) the delimiter, then double it
					writer.Write(remainingValue[..nextDelimiterIndex]);
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

			writer.Write((byte) '\'');
		}

#if NETCOREAPP3_1_OR_GREATER
		// Writes a partial chunk of a string (that may end with half of a surrogate pair), escaping any delimiter characters.
		static void WriteStringChunk(ByteBufferWriter writer, bool noBackslashEscapes, ReadOnlySpan<char> value)
		{
			var charsWritten = 0;
			while (charsWritten < value.Length)
			{
				var remainingValue = value[charsWritten..];
				var nextDelimiterIndex = remainingValue.IndexOfAny('\0', '\'', '\\');
				if (nextDelimiterIndex == -1)
				{
					// write the rest of the string
					writer.Write(remainingValue, flush: false);
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
		}
#endif
	}

	internal void AppendBinary(ByteBufferWriter writer, StatementPreparerOptions options)
	{
		if (Value is null || Value == DBNull.Value)
		{
			// stored in "null bitmap" only
		}
		else
		{
			AppendBinary(writer, Value, options);
		}
	}

	private void AppendBinary(ByteBufferWriter writer, object value, StatementPreparerOptions options)
	{
		if (value is string stringValue)
		{
			writer.WriteLengthEncodedString(stringValue);
		}
		else if (value is char charValue)
		{
			writer.WriteLengthEncodedString(charValue.ToString());
		}
		else if (value is sbyte sbyteValue)
		{
			writer.Write(unchecked((byte) sbyteValue));
		}
		else if (value is byte byteValue)
		{
			writer.Write(byteValue);
		}
		else if (value is bool boolValue)
		{
			writer.Write((byte) (boolValue ? 1 : 0));
		}
		else if (value is short shortValue)
		{
			writer.Write(unchecked((ushort) shortValue));
		}
		else if (value is ushort ushortValue)
		{
			writer.Write(ushortValue);
		}
		else if (value is int intValue)
		{
			writer.Write(intValue);
		}
		else if (value is uint uintValue)
		{
			writer.Write(uintValue);
		}
		else if (value is long longValue)
		{
			writer.Write(unchecked((ulong) longValue));
		}
		else if (value is ulong ulongValue)
		{
			writer.Write(ulongValue);
		}
		else if (value is byte[] byteArrayValue)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) byteArrayValue.Length));
			writer.Write(byteArrayValue);
		}
		else if (value is ReadOnlyMemory<byte> readOnlyMemoryValue)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) readOnlyMemoryValue.Length));
			writer.Write(readOnlyMemoryValue.Span);
		}
		else if (value is Memory<byte> memoryValue)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) memoryValue.Length));
			writer.Write(memoryValue.Span);
		}
		else if (value is ArraySegment<byte> arraySegmentValue)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) arraySegmentValue.Count));
			writer.Write(arraySegmentValue);
		}
		else if (value is MySqlGeometry geometry)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) geometry.ValueSpan.Length));
			writer.Write(geometry.ValueSpan);
		}
		else if (value is MemoryStream memoryStream)
		{
			if (!memoryStream.TryGetBuffer(out var streamBuffer))
				streamBuffer = new ArraySegment<byte>(memoryStream.ToArray());
			writer.WriteLengthEncodedInteger(unchecked((ulong) streamBuffer.Count));
			writer.Write(streamBuffer);
		}
		else if (value is Stream)
		{
			// do nothing; this will be sent via CommandKind.StatementSendLongData
		}
		else if (value is float floatValue)
		{
#if NET5_0_OR_GREATER
			Span<byte> bytes = stackalloc byte[4];
			BinaryPrimitives.WriteSingleLittleEndian(bytes, floatValue);
			writer.Write(bytes);
#else
			// convert float to bytes with correct endianness (MySQL uses little-endian)
			var bytes = BitConverter.GetBytes(floatValue);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			writer.Write(bytes);
#endif
		}
		else if (value is double doubleValue)
		{
#if NET5_0_OR_GREATER
			Span<byte> bytes = stackalloc byte[8];
			BinaryPrimitives.WriteDoubleLittleEndian(bytes, doubleValue);
			writer.Write(bytes);
#else
			if (BitConverter.IsLittleEndian)
			{
				writer.Write(unchecked((ulong) BitConverter.DoubleToInt64Bits(doubleValue)));
			}
			else
			{
				// convert double to bytes with correct endianness (MySQL uses little-endian)
				var bytes = BitConverter.GetBytes(doubleValue);
				Array.Reverse(bytes);
				writer.Write(bytes);
			}
#endif
		}
		else if (value is float[] floatArrayValue)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) floatArrayValue.Length * 4));
			writer.Write(ConvertFloatsToBytes(floatArrayValue.AsSpan()));
		}
		else if (value is Memory<float> floatMemory)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) floatMemory.Length * 4));
			writer.Write(ConvertFloatsToBytes(floatMemory.Span));
		}
		else if (value is ReadOnlyMemory<float> floatReadOnlyMemory)
		{
			writer.WriteLengthEncodedInteger(unchecked((ulong) floatReadOnlyMemory.Length * 4));
			writer.Write(ConvertFloatsToBytes(floatReadOnlyMemory.Span));
		}
		else if (value is decimal decimalValue)
		{
			writer.WriteLengthEncodedAsciiString(decimalValue.ToString(CultureInfo.InvariantCulture));
		}
		else if (value is BigInteger bigInteger)
		{
			writer.WriteLengthEncodedAsciiString(bigInteger.ToString(CultureInfo.InvariantCulture));
		}
		else if (value is MySqlDateTime mySqlDateTimeValue)
		{
			if (mySqlDateTimeValue.IsValidDateTime)
				WriteDateTime(writer, mySqlDateTimeValue.GetDateTime());
			else
				writer.Write((byte) 0);
		}
		else if (value is MySqlDecimal mySqlDecimal)
		{
			writer.WriteLengthEncodedAsciiString(mySqlDecimal.ToString());
		}
#if NET6_0_OR_GREATER
		else if (value is DateOnly dateOnlyValue)
		{
			WriteDateOnly(writer, dateOnlyValue);
		}
#endif
		else if (value is DateTime dateTimeValue)
		{
			if ((options & StatementPreparerOptions.DateTimeUtc) != 0 && dateTimeValue.Kind == DateTimeKind.Local)
				throw new MySqlException($"DateTime.Kind must not be Local when DateTimeKind setting is Utc (parameter name: {ParameterName})");
			else if ((options & StatementPreparerOptions.DateTimeLocal) != 0 && dateTimeValue.Kind == DateTimeKind.Utc)
				throw new MySqlException($"DateTime.Kind must not be Utc when DateTimeKind setting is Local (parameter name: {ParameterName})");

			WriteDateTime(writer, dateTimeValue);
		}
		else if (value is DateTimeOffset dateTimeOffsetValue)
		{
			// store as UTC as it will be read as such when deserialized from a timespan column
			WriteDateTime(writer, dateTimeOffsetValue.UtcDateTime);
		}
#if NET6_0_OR_GREATER
		else if (value is TimeOnly timeOnlyValue)
		{
			WriteTime(writer, timeOnlyValue.ToTimeSpan());
		}
#endif
		else if (value is TimeSpan ts)
		{
			WriteTime(writer, ts);
		}
		else if (value is Guid guidValue)
		{
			StatementPreparerOptions guidOptions = options & StatementPreparerOptions.GuidFormatMask;
			if (guidOptions is StatementPreparerOptions.GuidFormatBinary16 or StatementPreparerOptions.GuidFormatTimeSwapBinary16 or StatementPreparerOptions.GuidFormatLittleEndianBinary16)
			{
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
				Span<byte> bytes = stackalloc byte[16];
#if NET8_0_OR_GREATER
				guidValue.TryWriteBytes(bytes, bigEndian: guidOptions != StatementPreparerOptions.GuidFormatLittleEndianBinary16, out _);
#else
				guidValue.TryWriteBytes(bytes);
#endif
#else
				var bytes = guidValue.ToByteArray();
#endif
				if (guidOptions != StatementPreparerOptions.GuidFormatLittleEndianBinary16)
				{
#if !NET8_0_OR_GREATER
					Utility.SwapBytes(bytes, 0, 3);
					Utility.SwapBytes(bytes, 1, 2);
					Utility.SwapBytes(bytes, 4, 5);
					Utility.SwapBytes(bytes, 6, 7);
#endif

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
		else if (value is ReadOnlyMemory<char> readOnlyMemoryChar)
		{
			writer.WriteLengthEncodedString(readOnlyMemoryChar.Span);
		}
		else if (value is Memory<char> memoryChar)
		{
			writer.WriteLengthEncodedString(memoryChar.Span);
		}
		else if (value is StringBuilder stringBuilder)
		{
			writer.WriteLengthEncodedString(stringBuilder);
		}
		else if ((MySqlDbType is MySqlDbType.String or MySqlDbType.VarChar) && HasSetDbType && value is Enum stringEnumValue)
		{
			writer.WriteLengthEncodedString(stringEnumValue.ToString("G"));
		}
		else if (value is Enum)
		{
			// using the underlying type matches the log in TypeMapper.GetDbTypeMapping, which controls the column type value that was sent to the server
			var underlyingValue = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()), CultureInfo.InvariantCulture);
			AppendBinary(writer, underlyingValue, options);
		}
		else if (MySqlDbType == MySqlDbType.Int16)
		{
			writer.Write((ushort) (short) value);
		}
		else if (MySqlDbType == MySqlDbType.UInt16)
		{
			writer.Write((ushort) value);
		}
		else if (MySqlDbType == MySqlDbType.Int32)
		{
			writer.Write((int) value);
		}
		else if (MySqlDbType == MySqlDbType.UInt32)
		{
			writer.Write((uint) value);
		}
		else if (MySqlDbType == MySqlDbType.Int64)
		{
			writer.Write((ulong) (long) value);
		}
		else if (MySqlDbType == MySqlDbType.UInt64)
		{
			writer.Write((ulong) value);
		}
		else
		{
			throw new NotSupportedException($"Parameter type {value.GetType().Name} is not supported; see https://mysqlconnector.net/param-type. Value: {value}");
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

	internal static ReadOnlySpan<byte> ConvertFloatsToBytes(ReadOnlySpan<float> floats)
	{
		if (BitConverter.IsLittleEndian)
		{
			return MemoryMarshal.AsBytes(floats);
		}
		else
		{
			// for big-endian platforms, we need to convert each float individually
			var bytes = new byte[floats.Length * 4];

			for (var i = 0; i < floats.Length; i++)
			{
#if NET5_0_OR_GREATER
				BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(i * 4), floats[i]);
#else
				var floatBytes = BitConverter.GetBytes(floats[i]);
				Array.Reverse(floatBytes);
				floatBytes.CopyTo(bytes, i * 4);
#endif
			}

			return bytes;
		}
	}

	private static ReadOnlySpan<byte> BinaryBytes => "_binary'"u8;

	private DbType m_dbType;
	private MySqlDbType m_mySqlDbType;
	private string m_name;
	private ParameterDirection? m_direction;
	private object? m_value;
}
