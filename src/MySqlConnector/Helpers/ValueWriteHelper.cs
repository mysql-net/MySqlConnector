using System.Buffers.Text;
using System.Globalization;
using System.Numerics;
using System.Text;
using MySqlConnector.Utilities;

namespace MySqlConnector.Helpers;

internal class ValueWriteHelper
{
	private static readonly char[] s_specialCharacters = ['\t', '\\', '\n'];
	private static readonly UTF8Encoding s_utf8Encoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public static bool WriteValue<T>(MySqlConnection connection, T value, ref int inputIndex, ref Encoder? utf8Encoder, Span<byte> output, out int bytesWritten)
	{
		if (output.Length == 0)
		{
			bytesWritten = 0;
			return false;
		}

		switch (value)
		{
			case null:
			case DBNull dbNull:
			{
				return WriteNull(output, out bytesWritten);
			}

			case char charValue:
			{
				return WriteString(charValue.ToString(), ref utf8Encoder, output, out bytesWritten);
			}

			case byte byteValue:
			{
				return Utf8Formatter.TryFormat(byteValue, output, out bytesWritten);
			}

			case sbyte sbyteValue:
			{
				return Utf8Formatter.TryFormat(sbyteValue, output, out bytesWritten);
			}

			case short shortValue:
			{
				return Utf8Formatter.TryFormat(shortValue, output, out bytesWritten);
			}

			case ushort ushortValue:
			{
				return Utf8Formatter.TryFormat(ushortValue, output, out bytesWritten);
			}

			case int intValue:
			{
				return Utf8Formatter.TryFormat(intValue, output, out bytesWritten);
			}

			case uint uintValue:
			{
				return Utf8Formatter.TryFormat(uintValue, output, out bytesWritten);
			}

			case long longValue:
			{
				return Utf8Formatter.TryFormat(longValue, output, out bytesWritten);
			}

			case ulong ulongValue:
			{
				return Utf8Formatter.TryFormat(ulongValue, output, out bytesWritten);
			}

			case decimal decimalValue:
			{
				return Utf8Formatter.TryFormat(decimalValue, output, out bytesWritten);
			}

			case bool boolValue:
			{
				if (output.Length < 1)
				{
					bytesWritten = 0;
					return false;
				}
				output[0] = boolValue ? (byte) '1' : (byte) '0';
				bytesWritten = 1;
				return true;
			}

			case float floatValue:
			{
				// NOTE: Utf8Formatter doesn't support "R"
				return WriteString(floatValue.ToString("R", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}

			case double doubleValue:
			{
				// NOTE: Utf8Formatter doesn't support "R"
				return WriteString(doubleValue.ToString("R", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}

			case MySqlDateTime mySqlDateTimeValue:
			{
				if (mySqlDateTimeValue.IsValidDateTime)
					return WriteString(mySqlDateTimeValue.GetDateTime().ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
				else
					return WriteString("0000-00-00", ref utf8Encoder, output, out bytesWritten);
			}

			case DateTime dateTimeValue:
			{
				if (connection.DateTimeKind == DateTimeKind.Utc && dateTimeValue.Kind == DateTimeKind.Local)
					throw new MySqlException("DateTime.Kind must not be Local when DateTimeKind setting is Utc");
				else if (connection.DateTimeKind == DateTimeKind.Local && dateTimeValue.Kind == DateTimeKind.Utc)
					throw new MySqlException("DateTime.Kind must not be Utc when DateTimeKind setting is Local");

				return WriteString(dateTimeValue.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}

			case DateTimeOffset dateTimeOffsetValue:
			{
				// store as UTC as it will be read as such when deserialized from a timespan column
				return WriteString(dateTimeOffsetValue.UtcDateTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'ffffff", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}

			case string stringValue:
			{
				return WriteSubstring(stringValue, ref inputIndex, ref utf8Encoder, output, out bytesWritten);
			}

			case byte[] byteArray:
			{
				return WriteBytes(byteArray.AsSpan(), ref inputIndex, output, out bytesWritten);
			}

			case ReadOnlyMemory<byte> memory:
			{
				return WriteBytes(memory.Span, ref inputIndex, output, out bytesWritten);
			}

			case ArraySegment<byte> arraySegment:
			{
				return WriteBytes(arraySegment.AsSpan(), ref inputIndex, output, out bytesWritten);
			}

			case Memory<byte> memory:
			{
				return WriteBytes(memory.Span, ref inputIndex, output, out bytesWritten);
			}

			case MySqlGeometry geometry:
			{
				return WriteBytes(geometry.ValueSpan, ref inputIndex, output, out bytesWritten);
			}

			case float[] floatArray:
			{
				return WriteBytes(MySqlParameter.ConvertFloatsToBytes(floatArray.AsSpan()), ref inputIndex, output, out bytesWritten);
			}

			case Memory<float> memory:
			{
				return WriteBytes(MySqlParameter.ConvertFloatsToBytes(memory.Span), ref inputIndex, output, out bytesWritten);
			}

			case ReadOnlyMemory<float> memory:
			{
				return WriteBytes(MySqlParameter.ConvertFloatsToBytes(memory.Span), ref inputIndex, output, out bytesWritten);
			}

#if NET6_0_OR_GREATER

			case DateOnly dateOnlyValue:
			{
				return WriteString(dateOnlyValue.ToString("yyyy'-'MM'-'dd", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}

			case TimeOnly timeOnlyValue:
			{
				return WriteString(timeOnlyValue.ToString("HH':'mm':'ss'.'ffffff", CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}

#endif
			case TimeSpan ts:
			{
				var isNegative = false;
				if (ts.Ticks < 0)
				{
					isNegative = true;
					ts = TimeSpan.FromTicks(-ts.Ticks);
				}
#if NET6_0_OR_GREATER
				var str = string.Create(CultureInfo.InvariantCulture, $"{(isNegative ? "-" : "")}{ts.Days * 24 + ts.Hours}:{ts:mm':'ss'.'ffffff}");
#else
				var str = FormattableString.Invariant($"{(isNegative ? "-" : "")}{ts.Days * 24 + ts.Hours}:{ts:mm':'ss'.'ffffff}");
#endif
				return WriteString(str, ref utf8Encoder, output, out bytesWritten);
			}

			case Guid guidValue:
			{
				if (connection.GuidFormat is MySqlGuidFormat.Binary16 or MySqlGuidFormat.TimeSwapBinary16 or MySqlGuidFormat.LittleEndianBinary16)
				{
					var bytes = guidValue.ToByteArray();
					if (connection.GuidFormat != MySqlGuidFormat.LittleEndianBinary16)
					{
						Utility.SwapBytes(bytes, 0, 3);
						Utility.SwapBytes(bytes, 1, 2);
						Utility.SwapBytes(bytes, 4, 5);
						Utility.SwapBytes(bytes, 6, 7);

						if (connection.GuidFormat == MySqlGuidFormat.TimeSwapBinary16)
						{
							Utility.SwapBytes(bytes, 0, 4);
							Utility.SwapBytes(bytes, 1, 5);
							Utility.SwapBytes(bytes, 2, 6);
							Utility.SwapBytes(bytes, 3, 7);
							Utility.SwapBytes(bytes, 0, 2);
							Utility.SwapBytes(bytes, 1, 3);
						}
					}
					return WriteBytes(bytes, ref inputIndex, output, out bytesWritten);
				}
				else
				{
					var is32Characters = connection.GuidFormat == MySqlGuidFormat.Char32;
					return Utf8Formatter.TryFormat(guidValue, output, out bytesWritten, is32Characters ? 'N' : 'D');
				}
			}

			case Enum enumValue:
			{
				return WriteString(enumValue.ToString("d"), ref utf8Encoder, output, out bytesWritten);
			}

			case BigInteger bigInteger:
			{
				return WriteString(bigInteger.ToString(CultureInfo.InvariantCulture), ref utf8Encoder, output, out bytesWritten);
			}

			case MySqlDecimal mySqlDecimal:
			{
				return WriteString(mySqlDecimal.ToString(), ref utf8Encoder, output, out bytesWritten);
			}

			default:
			{
				throw new NotSupportedException($"Type {typeof(T).Name} not currently supported. Value: {value}");
			}
		}
	}

	private static bool WriteNull(Span<byte> output, out int bytesWritten)
	{
		ReadOnlySpan<byte> escapedNull = @"\N"u8; // a field value of \N is read as NULL for input
		if (output.Length < escapedNull.Length)
		{
			bytesWritten = 0;
			return false;
		}
		escapedNull.CopyTo(output);
		bytesWritten = escapedNull.Length;
		return true;
	}

	private static bool WriteString(string value, ref Encoder? utf8Encoder, Span<byte> output, out int bytesWritten)
	{
		var inputIndex = 0;
		if (WriteSubstring(value, ref inputIndex, ref utf8Encoder, output, out bytesWritten))
			return true;
		bytesWritten = 0;
		return false;
	}

	// Writes as much of 'value' as possible, starting at 'inputIndex' and writing UTF-8-encoded bytes to 'output'.
	// 'inputIndex' will be updated to the next character to be written, and 'bytesWritten' the number of bytes written to 'output'.
	private static bool WriteSubstring(string value, ref int inputIndex, ref Encoder? utf8Encoder, Span<byte> output, out int bytesWritten)
	{
		bytesWritten = 0;
		while (inputIndex < value.Length)
		{
			if (Array.IndexOf(s_specialCharacters, value[inputIndex]) != -1)
			{
				if (output.Length <= 2)
					return false;

				output[0] = (byte) '\\';
				output[1] = (byte) value[inputIndex];
				output = output[2..];
				bytesWritten += 2;
				inputIndex++;
			}
			else
			{
				var nextIndex = value.IndexOfAny(s_specialCharacters, inputIndex);
				if (nextIndex == -1)
					nextIndex = value.Length;

				utf8Encoder ??= s_utf8Encoding.GetEncoder();
				if (output.Length < 4 && utf8Encoder.GetByteCount(value.AsSpan(inputIndex, Math.Min(2, nextIndex - inputIndex)), flush: false) > output.Length)
					return false;
				utf8Encoder.Convert(value.AsSpan(inputIndex, nextIndex - inputIndex), output, nextIndex == value.Length, out var charsUsed, out var bytesUsed, out var completed);

				bytesWritten += bytesUsed;
				output = output[bytesUsed..];
				inputIndex += charsUsed;

				if (!completed)
					return false;
			}
		}

		return true;
	}

	private static bool WriteBytes(ReadOnlySpan<byte> value, ref int inputIndex, Span<byte> output, out int bytesWritten)
	{
		ReadOnlySpan<byte> hex = "0123456789ABCDEF"u8;
		bytesWritten = 0;
		for (; inputIndex < value.Length && output.Length > 2; inputIndex++)
		{
			var by = value[inputIndex];
			output[0] = hex[(by >> 4) & 0xF];
			output[1] = hex[by & 0xF];
			output = output[2..];
			bytesWritten += 2;
		}

		return inputIndex == value.Length;
	}
}
