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
	}
}
