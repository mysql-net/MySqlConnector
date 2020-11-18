using System;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class BinaryRow : Row
	{
		public BinaryRow(ResultSet resultSet)
			: base(resultSet)
		{
		}

		protected override Row CloneCore() => new BinaryRow(ResultSet);

		protected override void GetDataOffsets(ReadOnlySpan<byte> data, int[] dataOffsets, int[] dataLengths)
		{
			Array.Clear(dataOffsets, 0, dataOffsets.Length);
			for (var column = 0; column < dataOffsets.Length; column++)
			{
				if ((data[(column + 2) / 8 + 1] & (1 << ((column + 2) % 8))) != 0)
				{
					// column is NULL
					dataOffsets[column] = -1;
				}
			}

			var reader = new ByteArrayReader(data);

			// skip packet header (1 byte) and NULL bitmap (formula for length at https://dev.mysql.com/doc/internals/en/null-bitmap.html)
			reader.Offset += 1 + (dataOffsets.Length + 7 + 2) / 8;
			for (var column = 0; column < dataOffsets.Length; column++)
			{
				if (dataOffsets[column] == -1)
				{
					dataLengths[column] = 0;
				}
				else
				{
					var columnDefinition = ResultSet.ColumnDefinitions![column];
					var length = columnDefinition.ColumnType switch
					{
						ColumnType.Longlong or ColumnType.Double => 8,
						ColumnType.Long or ColumnType.Int24 or ColumnType.Float => 4,
						ColumnType.Short or ColumnType.Year => 2,
						ColumnType.Tiny => 1,
						ColumnType.Date or ColumnType.DateTime or ColumnType.Timestamp or ColumnType.Time => reader.ReadByte(),
						ColumnType.DateTime2 or ColumnType.NewDate or ColumnType.Timestamp2 => throw new NotSupportedException("ColumnType {0} is not supported".FormatInvariant(columnDefinition.ColumnType)),
						_ => checked((int) reader.ReadLengthEncodedInteger()),
					};

					dataLengths[column] = length;
					dataOffsets[column] = reader.Offset;
				}

				reader.Offset += dataLengths[column];
			}
		}

		protected override int GetInt32Core(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
		{
			var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
			return columnDefinition.ColumnType switch
			{
				ColumnType.Tiny => isUnsigned ? (int) data[0] : (sbyte) data[0],
				ColumnType.Decimal or ColumnType.NewDecimal => Utf8Parser.TryParse(data, out decimal decimalValue, out int bytesConsumed) && bytesConsumed == data.Length ? checked((int) decimalValue) : throw new FormatException(),
				ColumnType.Int24 or ColumnType.Long => isUnsigned ? checked((int) MemoryMarshal.Read<uint>(data)) : MemoryMarshal.Read<int>(data),
				ColumnType.Longlong => isUnsigned ? checked((int) MemoryMarshal.Read<ulong>(data)) : checked((int) MemoryMarshal.Read<long>(data)),
				ColumnType.Short => isUnsigned ? (int) MemoryMarshal.Read<ushort>(data) : MemoryMarshal.Read<short>(data),
				ColumnType.Year => MemoryMarshal.Read<short>(data),
				_ => throw new FormatException(),
			};
		}

		protected override object GetValueCore(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
		{
			var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
			switch (columnDefinition.ColumnType)
			{
			case ColumnType.Tiny:
				if (Connection.TreatTinyAsBoolean && columnDefinition.ColumnLength == 1 && !isUnsigned)
					return data[0] != 0;
				return isUnsigned ? (object) data[0] : (sbyte) data[0];

			case ColumnType.Int24:
			case ColumnType.Long:
				return isUnsigned ? (object) MemoryMarshal.Read<uint>(data) : MemoryMarshal.Read<int>(data);

			case ColumnType.Longlong:
				return isUnsigned ? (object) MemoryMarshal.Read<ulong>(data) : MemoryMarshal.Read<long>(data);

			case ColumnType.Bit:
				return ReadBit(data, columnDefinition);

			case ColumnType.String:
				if (Connection.GuidFormat == MySqlGuidFormat.Char36 && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
					return Utf8Parser.TryParse(data, out Guid guid, out int guid36BytesConsumed, 'D') && guid36BytesConsumed == 36 ? guid : throw new FormatException();
				if (Connection.GuidFormat == MySqlGuidFormat.Char32 && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 32)
					return Utf8Parser.TryParse(data, out Guid guid, out int guid32BytesConsumed, 'N') && guid32BytesConsumed == 32 ? guid : throw new FormatException();
				goto case ColumnType.VarString;

			case ColumnType.VarString:
			case ColumnType.VarChar:
			case ColumnType.TinyBlob:
			case ColumnType.Blob:
			case ColumnType.MediumBlob:
			case ColumnType.LongBlob:
			case ColumnType.Enum:
			case ColumnType.Set:
				if (columnDefinition.CharacterSet == CharacterSet.Binary)
				{
					var guidFormat = Connection.GuidFormat;
					if ((guidFormat is MySqlGuidFormat.Binary16 or MySqlGuidFormat.TimeSwapBinary16 or MySqlGuidFormat.LittleEndianBinary16) && columnDefinition.ColumnLength == 16)
						return CreateGuidFromBytes(guidFormat, data);

					return data.ToArray();
				}
				return Encoding.UTF8.GetString(data);

			case ColumnType.Json:
				return Encoding.UTF8.GetString(data);

			case ColumnType.Short:
				return isUnsigned ? (object) MemoryMarshal.Read<ushort>(data) : MemoryMarshal.Read<short>(data);

			case ColumnType.Date:
			case ColumnType.DateTime:
			case ColumnType.Timestamp:
				return ReadDateTime(data);

			case ColumnType.Time:
				return ReadTimeSpan(data);

			case ColumnType.Year:
				return (int) MemoryMarshal.Read<short>(data);

			case ColumnType.Float:
				return MemoryMarshal.Read<float>(data);

			case ColumnType.Double:
				return MemoryMarshal.Read<double>(data);

			case ColumnType.Decimal:
			case ColumnType.NewDecimal:
				return Utf8Parser.TryParse(data, out decimal decimalValue, out int bytesConsumed) && bytesConsumed == data.Length ? decimalValue : throw new FormatException();

			case ColumnType.Geometry:
				return data.ToArray();

			default:
				throw new NotImplementedException("Reading {0} not implemented".FormatInvariant(columnDefinition.ColumnType));
			}
		}

		private object ReadDateTime(ReadOnlySpan<byte> value)
		{
			if (value.Length == 0)
			{
				if (Connection.ConvertZeroDateTime)
					return DateTime.MinValue;
				if (Connection.AllowZeroDateTime)
					return new MySqlDateTime();
				throw new InvalidCastException("Unable to convert MySQL date/time to System.DateTime.");
			}

			int year = value[0] + value[1] * 256;
			int month = value[2];
			int day = value[3];

			int hour, minute, second;
			if (value.Length <= 4)
			{
				hour = 0;
				minute = 0;
				second = 0;
			}
			else
			{
				hour = value[4];
				minute = value[5];
				second = value[6];
			}

			var microseconds = value.Length <= 7 ? 0 : MemoryMarshal.Read<int>(value.Slice(7));

			try
			{
				return Connection.AllowZeroDateTime ? (object) new MySqlDateTime(year, month, day, hour, minute, second, microseconds) :
					new DateTime(year, month, day, hour, minute, second, microseconds / 1000, Connection.DateTimeKind).AddTicks(microseconds % 1000 * 10);
			}
			catch (Exception ex)
			{
				throw new FormatException("Couldn't interpret value as a valid DateTime".FormatInvariant(Encoding.UTF8.GetString(value)), ex);
			}
		}

		private static TimeSpan ReadTimeSpan(ReadOnlySpan<byte> value)
		{
			if (value.Length == 0)
				return TimeSpan.Zero;

			var isNegative = value[0];
			var days = MemoryMarshal.Read<int>(value.Slice(1));
			var hours = (int) value[5];
			var minutes = (int) value[6];
			var seconds = (int) value[7];
			var microseconds = value.Length == 8 ? 0 : MemoryMarshal.Read<int>(value.Slice(8));

			if (isNegative != 0)
			{
				days = -days;
				hours = -hours;
				minutes = -minutes;
				seconds = -seconds;
				microseconds = -microseconds;
			}

			return new TimeSpan(days, hours, minutes, seconds) + TimeSpan.FromTicks(microseconds * 10);
		}
	}
}
