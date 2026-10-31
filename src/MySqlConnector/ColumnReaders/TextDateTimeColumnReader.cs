using System.Runtime.CompilerServices;
using System.Text;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Utilities;

namespace MySqlConnector.ColumnReaders;

internal sealed class TextDateTimeColumnReader : ColumnReader
{
	public TextDateTimeColumnReader(MySqlConnection connection)
		: base(ColumnReaderKind.DateTime)
	{
		m_allowZeroDateTime = connection.AllowZeroDateTime;
		m_convertZeroDateTime = connection.ConvertZeroDateTime;
		m_dateTimeKind = connection.DateTimeKind;
	}

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ParseDateTime(data, m_convertZeroDateTime, m_allowZeroDateTime, m_dateTimeKind);

	public override DateTime ReadDateTime(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ParseDateTime(data, m_convertZeroDateTime, m_allowZeroDateTime, m_dateTimeKind, out var mySqlDateTime) ?? mySqlDateTime.GetDateTime();

	public override MySqlDateTime ReadMySqlDateTime(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ParseDateTime(data, m_convertZeroDateTime, m_allowZeroDateTime, m_dateTimeKind, out var mySqlDateTime) is { } dateTime ? new(dateTime) : mySqlDateTime;

	public static object ParseDateTime(ReadOnlySpan<byte> data, bool convertZeroDateTime, bool allowZeroDateTime, DateTimeKind dateTimeKind) =>
		ParseDateTime(data, convertZeroDateTime, allowZeroDateTime, dateTimeKind, out var mySqlDateTime) is { } dateTime ? dateTime : mySqlDateTime;

	/// <summary>
	/// Parses <paramref name="data"/> as a <see cref="DateTime"/>. If the value should be returned as a <see cref="MySqlDateTime"/> instead
	/// (i.e., <paramref name="allowZeroDateTime"/> is <c>true</c>), returns <c>null</c> and sets <paramref name="mySqlDateTime"/>.
	/// </summary>
	public static DateTime? ParseDateTime(ReadOnlySpan<byte> data, bool convertZeroDateTime, bool allowZeroDateTime, DateTimeKind dateTimeKind, out MySqlDateTime mySqlDateTime)
	{
		mySqlDateTime = default;
		Exception? exception = null;

		// MySQL sends DATE and DATETIME values as 'YYYY-MM-DD[ hh:mm:ss[.ffffff]]'; parse the fixed-width fields directly
		if (data.Length < 10 || data[4] != '-' || data[7] != '-' ||
			!TryParseDigits(data[..4], out var year) || !TryParseDigits(data.Slice(5, 2), out var month) || !TryParseDigits(data.Slice(8, 2), out var day))
		{
			goto InvalidDateTime;
		}

		if (year == 0 && month == 0 && day == 0)
		{
			if (convertZeroDateTime)
				return DateTime.MinValue;
			if (allowZeroDateTime)
				return null;
			throw new InvalidCastException("Unable to convert MySQL date/time to System.DateTime, set AllowZeroDateTime=True or ConvertZeroDateTime=True in the connection string. See https://mysqlconnector.net/connection-options/");
		}

		int hour, minute, second, ticks;
		if (data.Length == 10)
		{
			hour = 0;
			minute = 0;
			second = 0;
			ticks = 0;
		}
		else
		{
			if (data.Length < 19 || data[10] != ' ' || data[13] != ':' || data[16] != ':' ||
				!TryParseDigits(data.Slice(11, 2), out hour) || !TryParseDigits(data.Slice(14, 2), out minute) || !TryParseDigits(data.Slice(17, 2), out second))
			{
				goto InvalidDateTime;
			}

			if (data.Length == 19)
			{
				ticks = 0;
			}
			else
			{
				// up to seven fractional digits (i.e., 100ns ticks) are supported
				if (data[19] != '.' || data.Length is < 21 or > 27 || !TryParseDigits(data[20..], out ticks))
					goto InvalidDateTime;
				for (var digits = data.Length - 20; digits < 7; digits++)
					ticks *= 10;
			}
		}

		try
		{
			if (allowZeroDateTime)
			{
				mySqlDateTime = ticks % 10 == 0 ? new MySqlDateTime(year, month, day, hour, minute, second, ticks / 10) : throw new NotSupportedException("MySqlDateTime does not support sub-microsecond precision");
				return null;
			}
			return new DateTime(year, month, day, hour, minute, second, dateTimeKind).AddTicks(ticks);
		}
		catch (Exception ex)
		{
			exception = ex;
		}

InvalidDateTime:
		throw new FormatException($"Couldn't interpret value as a valid DateTime: {Encoding.UTF8.GetString(data)}", exception);
	}

	// Parses 'data', which must consist entirely of ASCII digits, as an unsigned integer.
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool TryParseDigits(ReadOnlySpan<byte> data, out int value)
	{
		value = 0;
		foreach (var b in data)
		{
			var digit = (uint) (b - '0');
			if (digit > 9)
				return false;
			value = value * 10 + (int) digit;
		}
		return true;
	}

	private readonly bool m_allowZeroDateTime;
	private readonly bool m_convertZeroDateTime;
	private readonly DateTimeKind m_dateTimeKind;
}
