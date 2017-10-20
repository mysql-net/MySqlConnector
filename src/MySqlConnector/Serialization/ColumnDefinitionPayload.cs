using System;
using System.Text;

namespace MySql.Data.Serialization
{
	internal class ColumnDefinitionPayload
	{
		public string Name
		{
			get
			{
				if (!m_readNames)
					ReadNames();
				return m_name;
			}
		}

		public CharacterSet CharacterSet { get; private set; }

		public uint ColumnLength { get; private set; }

		public ColumnType ColumnType { get; private set; }

		public ColumnFlags ColumnFlags { get; private set; }

		public string SchemaName
		{
			get
			{
				if (!m_readNames)
					ReadNames();
				return m_schemaName;
			}
		}

		public string CatalogName
		{
			get
			{
				if (!m_readNames)
					ReadNames();
				return m_catalogName;
			}
		}

		public string Table
		{
			get
			{
				if (!m_readNames)
					ReadNames();
				return m_table;
			}
		}

		public string PhysicalTable
		{
			get
			{
				if (!m_readNames)
					ReadNames();
				return m_physicalTable;
			}
		}

		public string PhysicalName
		{
			get
			{
				if (!m_readNames)
					ReadNames();
				return m_physicalName;
			}
		}

		public byte Decimals { get; private set; }

		public static ColumnDefinitionPayload Create(PayloadData payload)
		{
			var payloadCopy = new PayloadData(new ArraySegment<byte>((byte[]) payload.ArraySegment.Array?.Clone() ?? new byte[0], payload.ArraySegment.Offset, payload.ArraySegment.Count));
			var reader = new ByteArrayReader(payloadCopy.ArraySegment);
			SkipLengthEncodedByteString(ref reader); // catalog
			SkipLengthEncodedByteString(ref reader); // schema
			SkipLengthEncodedByteString(ref reader); // table
			SkipLengthEncodedByteString(ref reader); // physical table
			SkipLengthEncodedByteString(ref reader); // name
			SkipLengthEncodedByteString(ref reader); // physical name
			reader.ReadByte(0x0C); // length of fixed-length fields, always 0x0C
			var characterSet = (CharacterSet) reader.ReadUInt16();
			var columnLength = reader.ReadUInt32();
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
				OriginalPayload = payloadCopy,
				CharacterSet = characterSet,
				ColumnLength = columnLength,
				ColumnType = columnType,
				ColumnFlags = columnFlags,
				Decimals = decimals
			};
		}

		private static void SkipLengthEncodedByteString(ref ByteArrayReader reader)
		{
			var length = checked((int) reader.ReadLengthEncodedInteger());
			reader.Offset += length;
		}

		private void ReadNames()
		{
			var reader = new ByteArrayReader(OriginalPayload.ArraySegment);
			m_catalogName = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			m_schemaName = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			m_table = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			m_physicalTable = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			m_name = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			m_physicalName = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
			m_readNames = true;
		}

		PayloadData OriginalPayload { get; set; }

		bool m_readNames;
		string m_name;
		string m_schemaName;
		string m_catalogName;
		string m_table;
		string m_physicalTable;
		string m_physicalName;
	}
}
