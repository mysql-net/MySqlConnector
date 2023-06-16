namespace MySqlConnector.ColumnReaders;

using System;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using MySqlConnector.Core;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

internal sealed class BinaryDateTimeColumnReader : IColumnReader
{
	private bool allowZeroDateTime;
	private bool convertZeroDateTime;
	private DateTimeKind dateTimeKind;
	internal BinaryDateTimeColumnReader(MySqlConnection connection)
	{
		this.allowZeroDateTime = connection.AllowZeroDateTime;
		this.convertZeroDateTime = connection.ConvertZeroDateTime;
		this.dateTimeKind = connection.DateTimeKind;
	}

	public object ReadValue(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		if (data.Length == 0)
		{
			if (this.convertZeroDateTime)
				return DateTime.MinValue;
			if (this.allowZeroDateTime)
				return default(MySqlDateTime);
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
			return this.allowZeroDateTime ? (object) new MySqlDateTime(year, month, day, hour, minute, second, microseconds) :
#if NET7_0_OR_GREATER
				new DateTime(year, month, day, hour, minute, second, microseconds / 1000, microseconds % 1000, this.dateTimeKind);
#else
				new DateTime(year, month, day, hour, minute, second, microseconds / 1000, this.dateTimeKind).AddTicks(microseconds % 1000 * 10);
#endif
		}
		catch (Exception ex)
		{
			throw new FormatException($"Couldn't interpret value as a valid DateTime: {Encoding.UTF8.GetString(data)}", ex);
		}
	}

	public int ReadInt32(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		throw new InvalidCastException($"Can't convert {columnDefinition.ColumnType} to Int32");
	}
}
