using System.Runtime.InteropServices;
using System.Text;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Utilities;

namespace MySqlConnector.ColumnReaders;

internal sealed class BinaryDateTimeColumnReader : DateTimeColumnReader
{
	public BinaryDateTimeColumnReader(MySqlConnection connection)
	{
		m_allowZeroDateTime = connection.AllowZeroDateTime;
		m_convertZeroDateTime = connection.ConvertZeroDateTime;
		m_dateTimeKind = connection.DateTimeKind;
	}

	public override object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ParseDateTime(data, out var mySqlDateTime) is { } dateTime ? dateTime : mySqlDateTime;

	public override DateTime ReadDateTime(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ParseDateTime(data, out var mySqlDateTime) ?? mySqlDateTime.GetDateTime();

	public override MySqlDateTime ReadMySqlDateTime(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		ParseDateTime(data, out var mySqlDateTime) is { } dateTime ? new(dateTime) : mySqlDateTime;

	// Returns the value as a DateTime, or returns null and sets 'mySqlDateTime' if it should be returned as a MySqlDateTime.
	private DateTime? ParseDateTime(ReadOnlySpan<byte> data, out MySqlDateTime mySqlDateTime)
	{
		mySqlDateTime = default;
		if (data.Length == 0)
		{
			if (m_convertZeroDateTime)
				return DateTime.MinValue;
			if (m_allowZeroDateTime)
				return null;
			throw new InvalidCastException("Unable to convert MySQL date/time to System.DateTime.");
		}

		int year = data[0] + data[1] * 256;
		int month = data[2];
		int day = data[3];

		int hour, minute, second;
		if (data.Length <= 4)
		{
			hour = 0;
			minute = 0;
			second = 0;
		}
		else
		{
			hour = data[4];
			minute = data[5];
			second = data[6];
		}

		var microseconds = data.Length <= 7 ? 0 : MemoryMarshal.Read<int>(data[7..]);

		try
		{
			if (m_allowZeroDateTime)
			{
				mySqlDateTime = new MySqlDateTime(year, month, day, hour, minute, second, microseconds);
				return null;
			}
#if NET7_0_OR_GREATER
			return new DateTime(year, month, day, hour, minute, second, microseconds / 1000, microseconds % 1000, m_dateTimeKind);
#else
			return new DateTime(year, month, day, hour, minute, second, microseconds / 1000, m_dateTimeKind).AddTicks(microseconds % 1000 * 10);
#endif
		}
		catch (Exception ex)
		{
			throw new FormatException($"Couldn't interpret value as a valid DateTime: {Encoding.UTF8.GetString(data)}", ex);
		}
	}

	private readonly bool m_allowZeroDateTime;
	private readonly bool m_convertZeroDateTime;
	private readonly DateTimeKind m_dateTimeKind;
}
