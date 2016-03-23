using System;
using System.Text;

namespace MySql.Data.Serialization
{
	internal class ColumnDefinitionPayload
	{
		public string Name { get; }
		public int CharacterSet { get; }
		public ColumnType ColumnType { get; }

		public static ColumnDefinitionPayload Create(PayloadData payload)
		{
			var reader = new ByteArrayReader(payload.ArraySegment);
			var catalog = reader.ReadLengthEncodedByteString();
			var schema = reader.ReadLengthEncodedByteString();
			var table = reader.ReadLengthEncodedByteString();
			var physicalTable = reader.ReadLengthEncodedByteString();
			var name = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			var physicalName = reader.ReadLengthEncodedByteString();
			reader.ReadByte(0x0C); // length of fixed-length fields, always 0x0C
			int characterSet = reader.ReadUInt16();
			var columnLength = reader.ReadUInt32();
			var columnType = (ColumnType) reader.ReadByte();
			var flags = reader.ReadUInt16();
			reader.ReadByte(0);
			reader.ReadByte(0);
			if (reader.BytesRemaining > 0)
			{
				int defaultValuesCount = checked((int) reader.ReadLengthEncodedInteger());
				for (int i = 0; i < defaultValuesCount; i++)
					reader.ReadLengthEncodedByteString();
			}

			if (reader.BytesRemaining != 0)
				throw new FormatException("Extra bytes at end of payload.");
			return new ColumnDefinitionPayload(name, characterSet, columnType);
		}

		private ColumnDefinitionPayload(string name, int characterSet, ColumnType columnType)
		{
			Name = name;
			CharacterSet = characterSet;
			ColumnType = columnType;
		}

	}
}
