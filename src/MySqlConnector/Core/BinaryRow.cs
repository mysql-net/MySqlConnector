using System;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
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
					int length;
					switch (columnDefinition.ColumnType)
					{
					case ColumnType.Longlong:
					case ColumnType.Double:
						length = 8;
						break;
					case ColumnType.Long:
					case ColumnType.Int24:
					case ColumnType.Float:
						length = 4;
						break;
					case ColumnType.Short:
					case ColumnType.Year:
						length = 2;
						break;
					case ColumnType.Tiny:
						length = 1;
						break;
					case ColumnType.Date:
					case ColumnType.DateTime:
					case ColumnType.Timestamp:
					case ColumnType.Time:
						length = reader.ReadByte();
						break;
					case ColumnType.DateTime2:
					case ColumnType.NewDate:
					case ColumnType.Timestamp2:
						throw new NotSupportedException("ColumnType {0} is not supported".FormatInvariant(columnDefinition.ColumnType));
					default:
						length = checked((int) reader.ReadLengthEncodedInteger());
						break;
					}

					dataLengths[column] = length;
					dataOffsets[column] = reader.Offset;
				}

				reader.Offset += dataLengths[column];
			}
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
				return ReadBit(data, columnDefinition.ColumnFlags);

			case ColumnType.String:
				switch (Connection.GuidFormat)
				{
				case MySqlGuidFormat.Char36 when columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36:
					{
						return Utf8Parser.TryParse(data, out Guid guid, out int guid36BytesConsumed, 'D') && guid36BytesConsumed == 36 ? guid : throw new FormatException();
					}
				case MySqlGuidFormat.Char32 when columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 32:
					{
						return Utf8Parser.TryParse(data, out Guid guid, out int guid32BytesConsumed, 'N') && guid32BytesConsumed == 32 ? guid : throw new FormatException();
					}
				}

				goto case ColumnType.VarString;

			case ColumnType.VarString:
			case ColumnType.VarChar:
			case ColumnType.TinyBlob:
			case ColumnType.Blob:
			case ColumnType.MediumBlob:
			case ColumnType.LongBlob:
				if (columnDefinition.CharacterSet != CharacterSet.Binary) return Encoding.UTF8.GetString(data);
				var guidFormat = Connection.GuidFormat;
				if ((guidFormat == MySqlGuidFormat.Binary16 || guidFormat == MySqlGuidFormat.TimeSwapBinary16 || guidFormat == MySqlGuidFormat.LittleEndianBinary16) && columnDefinition.ColumnLength == 16)
					return CreateGuidFromBytes(guidFormat, data);

				return data.ToArray();

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

			var year = value[0] + value[1] * 256;
			var month = value[2];
			var day = value[3];

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
