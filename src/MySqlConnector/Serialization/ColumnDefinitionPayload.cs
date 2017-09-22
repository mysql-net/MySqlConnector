using System;
using System.Text;

namespace MySql.Data.Serialization
{
	internal class ColumnDefinitionPayload
	{
		public string Name { get; private set; }

		public CharacterSet CharacterSet { get; private set; }

		public int ColumnLength { get; private set; }

		public ColumnType ColumnType { get; private set; }

		public ColumnFlags ColumnFlags { get; private set; }

		public string SchemaName { get; private set; }

		public string CatelogName { get; private set; }

		public string Table { get; private set; }

		public string PhysicalTable { get; private set; }

		public string PhysicalName { get; private set; }

		public byte Decimals { get; private set; }

		public static ColumnDefinitionPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			var catalog = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			var schema = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			var table = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			var physicalTable = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			var name = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			var physicalName = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			reader.ReadByte(0x0C); // length of fixed-length fields, always 0x0C
			var characterSet = (CharacterSet) reader.ReadUInt16();
			var columnLength = (int) reader.ReadUInt32();
			var columnType = (ColumnType) reader.ReadByte();
			var columnFlags = (ColumnFlags) reader.ReadUInt16();
			var decimals = reader.ReadByte(); // 0x00 for integers and static strings, 0x1f for dynamic strings, double, float, 0x00 to 0x51 for decimals
			reader.ReadByte(0);
			if (reader.BytesRemaining > 0)
			{
				int defaultValuesCount = checked((int) reader.ReadLengthEncodedInteger());
				for (int i = 0; i < defaultValuesCount; i++)
					reader.ReadLengthEncodedByteString();
			}

			if (reader.BytesRemaining != 0)
			{
				throw new FormatException("Extra bytes at end of payload.");
			}

			return new ColumnDefinitionPayload
			{
				Name = name,
				CharacterSet = characterSet,
				ColumnLength = columnLength,
				ColumnType = columnType,
				ColumnFlags = columnFlags,
				SchemaName = schema,
				CatelogName = catalog,
				Table = table,
				PhysicalTable = physicalTable,
				PhysicalName = physicalName,
				Decimals = decimals
			};

		}

		public Type GetDataType(
			bool treadTinyAsBoolean = false,
			bool oldGuids = false)
		{
			var isUnsigned = (ColumnFlags & ColumnFlags.Unsigned) != 0;
			switch (ColumnType)
			{
				case ColumnType.Tiny:
					if (treadTinyAsBoolean && ColumnLength == 1)
					{
						return typeof(bool);
					}

					return isUnsigned ? typeof(byte) : typeof(sbyte);
				case ColumnType.Short:
					return isUnsigned ? typeof(ushort) : typeof(short);
				case ColumnType.Int24:
				case ColumnType.Long:
					return isUnsigned ? typeof(uint) : typeof(int);
				case ColumnType.Longlong:
					return isUnsigned ? typeof(ulong) : typeof(ulong);
				case ColumnType.Bit:
					return typeof(ulong);
				case ColumnType.String:
					if (!oldGuids && ColumnLength / SerializationUtility.GetBytesPerCharacter(CharacterSet) == 36)
					{
						return typeof(Guid);
					}

					goto case ColumnType.LongBlob;

				case ColumnType.VarString:
				case ColumnType.VarChar:
				case ColumnType.TinyBlob:
				case ColumnType.Blob:
				case ColumnType.MediumBlob:
				case ColumnType.LongBlob:
					if (CharacterSet == CharacterSet.Binary && oldGuids && ColumnLength == 16)
					{
						return typeof(Guid);
					}

					return typeof(string);
				case ColumnType.Json:
					return typeof(string);
				case ColumnType.Date:
				case ColumnType.DateTime:
				case ColumnType.Timestamp:
					return typeof(DateTime);
				case ColumnType.Time:
					return typeof(TimeSpan);
				case ColumnType.Year:
					return typeof(int);
				case ColumnType.Float:
					return typeof(float);
				case ColumnType.Double:
					return typeof(double);
				case ColumnType.Decimal:
				case ColumnType.NewDecimal:
					return typeof(decimal);
				default:
					return typeof(object);
			}
		}
	}
}
