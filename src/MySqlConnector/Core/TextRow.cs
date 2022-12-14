using System.Buffers.Text;
using System.Text;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal sealed class TextRow : Row
{
	public TextRow(ResultSet resultSet)
		: base(resultSet)
	{
	}

	protected override Row CloneCore() => new TextRow(ResultSet);

	protected override void GetDataOffsets(ReadOnlySpan<byte> data, int[] dataOffsets, int[] dataLengths)
	{
		var reader = new ByteArrayReader(data);
		for (var column = 0; column < dataOffsets.Length; column++)
		{
			var length = reader.ReadLengthEncodedIntegerOrNull();
			dataLengths[column] = length == -1 ? 0 : length;
			dataOffsets[column] = length == -1 ? -1 : reader.Offset;
			reader.Offset += dataLengths[column];
		}
	}

	protected override int GetInt32Core(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition) =>
		!Utf8Parser.TryParse(data, out int value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new OverflowException() : value;

	protected override object GetValueCore(ReadOnlySpan<byte> data, ColumnDefinitionPayload columnDefinition)
	{
		var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
		switch (columnDefinition.ColumnType)
		{
		case ColumnType.Tiny:
			var value = ParseInt32(data);
			if (Connection.TreatTinyAsBoolean && columnDefinition.ColumnLength == 1 && !isUnsigned)
				return value != 0;
			return isUnsigned ? (object) (byte) value : (sbyte) value;

		case ColumnType.Int24:
		case ColumnType.Long:
			return isUnsigned ? (object) ParseUInt32(data) : ParseInt32(data);

		case ColumnType.Longlong:
			return isUnsigned ? (object) ParseUInt64(data) : ParseInt64(data);

		case ColumnType.Bit:
			return ReadBit(data, columnDefinition);

		case ColumnType.String:
			if (Connection.GuidFormat == MySqlGuidFormat.Char36 && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
				return Utf8Parser.TryParse(data, out Guid guid, out int guid36BytesConsumed, 'D') && guid36BytesConsumed == 36 ? guid : throw new FormatException($"Could not parse CHAR(36) value as Guid: {Encoding.UTF8.GetString(data)}");
			if (Connection.GuidFormat == MySqlGuidFormat.Char32 && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 32)
				return Utf8Parser.TryParse(data, out Guid guid, out int guid32BytesConsumed, 'N') && guid32BytesConsumed == 32 ? guid : throw new FormatException($"Could not parse CHAR(32) value as Guid: {Encoding.UTF8.GetString(data)}");
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
			return isUnsigned ? (object) ParseUInt16(data) : ParseInt16(data);

		case ColumnType.Date:
		case ColumnType.DateTime:
		case ColumnType.NewDate:
		case ColumnType.Timestamp:
			return ParseDateTime(data);

		case ColumnType.Time:
			return Utility.ParseTimeSpan(data);

		case ColumnType.Year:
			return ParseInt32(data);

		case ColumnType.Float:
			if (Utf8Parser.TryParse(data, out float floatValue, out var floatBytesConsumed) && floatBytesConsumed == data.Length)
				return floatValue;
			ReadOnlySpan<byte> floatInfinity = new byte[] { 0x2D, 0x69, 0x6E, 0x66 }; // "-inf"
			if (data.SequenceEqual(floatInfinity))
				return float.NegativeInfinity;
			if (data.SequenceEqual(floatInfinity.Slice(1)))
				return float.PositiveInfinity;
			throw new FormatException($"Couldn't parse value as float: {Encoding.UTF8.GetString(data)}");

		case ColumnType.Double:
			if (Utf8Parser.TryParse(data, out double doubleValue, out var doubleBytesConsumed) && doubleBytesConsumed == data.Length)
				return doubleValue;
			ReadOnlySpan<byte> doubleInfinity = new byte[] { 0x2D, 0x69, 0x6E, 0x66 }; // "-inf"
			if (data.SequenceEqual(doubleInfinity))
				return double.NegativeInfinity;
			if (data.SequenceEqual(doubleInfinity.Slice(1)))
				return double.PositiveInfinity;
			throw new FormatException($"Couldn't parse value as double: {Encoding.UTF8.GetString(data)}");

		case ColumnType.Decimal:
		case ColumnType.NewDecimal:
			return Utf8Parser.TryParse(data, out decimal decimalValue, out int bytesConsumed) && bytesConsumed == data.Length ? decimalValue : throw new FormatException();

		case ColumnType.Geometry:
			return data.ToArray();

		default:
			throw new NotImplementedException($"Reading {columnDefinition.ColumnType} not implemented");
		}
	}

	private static short ParseInt16(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out short value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

	private static ushort ParseUInt16(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out ushort value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

	private static int ParseInt32(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out int value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

	private static uint ParseUInt32(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out uint value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;

	private static long ParseInt64(ReadOnlySpan<byte> data) =>
		!Utf8Parser.TryParse(data, out long value, out var bytesConsumed) || bytesConsumed != data.Length ? throw new FormatException() : value;
}
