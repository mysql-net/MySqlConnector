using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.ColumnReaders;

internal class ColumnReaderFactory
{
	internal static IColumnReader GetReader(bool binary, ColumnDefinitionPayload columnDefinition, MySqlConnection connection)
	{
		var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
		if (binary)
		{
			switch (columnDefinition.ColumnType)
			{
				case ColumnType.Tiny:
					if (connection.TreatTinyAsBoolean && columnDefinition.ColumnLength == 1 && !isUnsigned)
						return BinaryBooleanColumnReader.Instance;
					return isUnsigned ? BinaryUnsignedInt8ColumnReader.Instance : BinarySignedInt8ColumnReader.Instance;

				case ColumnType.Int24:
				case ColumnType.Long:
					return isUnsigned ? BinaryUnsignedInt32ColumnReader.Instance : BinarySignedInt32ColumnReader.Instance;

				case ColumnType.Longlong:
					return isUnsigned ? BinaryUnsignedInt64ColumnReader.Instance : BinarySignedInt64ColumnReader.Instance;

				case ColumnType.Bit:
					return BitColumnReader.Instance;

				case ColumnType.String:
					if (connection.GuidFormat == MySqlGuidFormat.Char36
					    && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
						return Guid36ColumnReader.Instance;
					if (connection.GuidFormat == MySqlGuidFormat.Char32
					    && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 32)
						return Guid32ColumnReader.Instance;
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
						var guidFormat = connection.GuidFormat;
						if ((guidFormat is MySqlGuidFormat.Binary16 or MySqlGuidFormat.TimeSwapBinary16
							    or MySqlGuidFormat.LittleEndianBinary16) && columnDefinition.ColumnLength == 16)
						{
							switch (guidFormat)
							{
							case MySqlGuidFormat.Binary16:
								return Guid16ColumnReader.Instance;
							case MySqlGuidFormat.TimeSwapBinary16:
								return TimeSwapBinary16ColumnReader.Instance;
							default:
								return GuidBytesColumnReader.Instance;
							}
						}
						return BytesColumnReader.Instance;
					}
					return StringColumnReader.Instance;

				case ColumnType.Json:
					return StringColumnReader.Instance;

				case ColumnType.Short:
					return isUnsigned ?
						BinaryUnsignedInt16ColumnReader.Instance :
						BinarySignedInt16ColumnReader.Instance;

				case ColumnType.Date:
				case ColumnType.DateTime:
				case ColumnType.NewDate:
				case ColumnType.Timestamp:
					return new BinaryDateTimeColumnReader(connection);

				case ColumnType.Time:
					return BinaryTimeColumnReader.Instance;

				case ColumnType.Year:
					return BinaryYearColumnReader.Instance;

				case ColumnType.Float:
					return BinaryFloatColumnReader.Instance;

				case ColumnType.Double:
					return BinaryDoubleColumnReader.Instance;

				case ColumnType.Decimal:
				case ColumnType.NewDecimal:
					return DecimalColumnReader.Instance;

				case ColumnType.Geometry:
					return BytesColumnReader.Instance;

				case ColumnType.Null:
					return NullColumnReader.Instance;

				default:
					throw new NotImplementedException($"Reading {columnDefinition.ColumnType} not implemented");
			}
		}
		else
		{
			switch (columnDefinition.ColumnType)
			{
				case ColumnType.Tiny:
					if (connection.TreatTinyAsBoolean && columnDefinition.ColumnLength == 1 && !isUnsigned)
						return TextBooleanColumnReader.Instance;
					return isUnsigned ? TextUnsignedInt8ColumnReader.Instance : TextSignedInt8ColumnReader.Instance;

				case ColumnType.Int24:
				case ColumnType.Long:
					return isUnsigned ? TextUnsignedInt32ColumnReader.Instance : TextSignedInt32ColumnReader.Instance;

				case ColumnType.Longlong:
					return isUnsigned ?
						TextUnsignedInt64ColumnReader.Instance :
						TextSignedInt64ColumnReader.Instance;

				case ColumnType.Bit:
					return BitColumnReader.Instance;

				case ColumnType.String:
					if (connection.GuidFormat == MySqlGuidFormat.Char36
					    && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
						return Guid36ColumnReader.Instance;
					if (connection.GuidFormat == MySqlGuidFormat.Char32
					    && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 32)
						return Guid32ColumnReader.Instance;
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
						var guidFormat = connection.GuidFormat;
						if ((guidFormat is MySqlGuidFormat.Binary16 or MySqlGuidFormat.TimeSwapBinary16
							    or MySqlGuidFormat.LittleEndianBinary16) && columnDefinition.ColumnLength == 16)
						{
							switch (guidFormat)
							{
								case MySqlGuidFormat.Binary16:
									return Guid16ColumnReader.Instance;
								case MySqlGuidFormat.TimeSwapBinary16:
									return TimeSwapBinary16ColumnReader.Instance;
								default:
									return GuidBytesColumnReader.Instance;
							}
						}
						return BytesColumnReader.Instance;
					}
					return StringColumnReader.Instance;

				case ColumnType.Json:
					return StringColumnReader.Instance;

				case ColumnType.Short:
					return isUnsigned ? TextUnsignedInt16ColumnReader.Instance : TextSignedInt16ColumnReader.Instance;

				case ColumnType.Date:
				case ColumnType.DateTime:
				case ColumnType.NewDate:
				case ColumnType.Timestamp:
					return new TextDateTimeColumnReader(connection);

				case ColumnType.Time:
					return TextTimeColumnReader.Instance;

				case ColumnType.Float:
					return TextFloatColumnReader.Instance;

				case ColumnType.Double:
					return TextDoubleColumnReader.Instance;

				case ColumnType.Decimal:
				case ColumnType.NewDecimal:
					return DecimalColumnReader.Instance;

				case ColumnType.Year:
					return TextSignedInt32ColumnReader.Instance;

				case ColumnType.Geometry:
					return BytesColumnReader.Instance;

				case ColumnType.Null:
					return NullColumnReader.Instance;

				default:
					throw new NotImplementedException($"Reading {columnDefinition.ColumnType} not implemented");
			}
		}
	}
}
