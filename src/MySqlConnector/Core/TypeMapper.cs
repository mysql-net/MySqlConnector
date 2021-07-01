using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using MySqlConnector.Protocol;
using MySqlConnector.Protocol.Payloads;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class TypeMapper
	{
		public static TypeMapper Instance { get; } = new();

		private TypeMapper()
		{
			m_columnTypeMetadata = new();
			m_dbTypeMappingsByClrType = new();
			m_dbTypeMappingsByDbType = new();
			m_columnTypeMetadataLookup = new(StringComparer.OrdinalIgnoreCase);
			m_mySqlDbTypeToColumnTypeMetadata = new();

			// boolean
			var typeBoolean = AddDbTypeMapping(new(typeof(bool), new[] { DbType.Boolean }, convert: static o => Convert.ToBoolean(o)));
			AddColumnTypeMetadata(new("TINYINT", typeBoolean, MySqlDbType.Bool, isUnsigned: false, length: 1, columnSize: 1, simpleDataTypeName: "BOOL", createFormat: "BOOL"));

			// integers
			var typeSbyte = AddDbTypeMapping(new(typeof(sbyte), new[] { DbType.SByte }, convert: static o => Convert.ToSByte(o)));
			var typeByte = AddDbTypeMapping(new(typeof(byte), new[] { DbType.Byte }, convert: static o => Convert.ToByte(o)));
			var typeShort = AddDbTypeMapping(new(typeof(short), new[] { DbType.Int16 }, convert: static o => Convert.ToInt16(o)));
			var typeUshort = AddDbTypeMapping(new(typeof(ushort), new[] { DbType.UInt16 }, convert: static o => Convert.ToUInt16(o)));
			var typeInt = AddDbTypeMapping(new(typeof(int), new[] { DbType.Int32 }, convert: static o => Convert.ToInt32(o)));
			var typeUint = AddDbTypeMapping(new(typeof(uint), new[] { DbType.UInt32 }, convert: static o => Convert.ToUInt32(o)));
			var typeLong = AddDbTypeMapping(new(typeof(long), new[] { DbType.Int64 }, convert: static o => Convert.ToInt64(o)));
			var typeUlong = AddDbTypeMapping(new(typeof(ulong), new[] { DbType.UInt64 }, convert: static o => Convert.ToUInt64(o)));
			AddColumnTypeMetadata(new("TINYINT", typeSbyte, MySqlDbType.Byte, isUnsigned: false));
			AddColumnTypeMetadata(new("TINYINT", typeByte, MySqlDbType.UByte, isUnsigned: true, length: 1));
			AddColumnTypeMetadata(new("TINYINT", typeByte, MySqlDbType.UByte, isUnsigned: true));
			AddColumnTypeMetadata(new("SMALLINT", typeShort, MySqlDbType.Int16, isUnsigned: false));
			AddColumnTypeMetadata(new("SMALLINT", typeUshort, MySqlDbType.UInt16, isUnsigned: true));
			AddColumnTypeMetadata(new("INT", typeInt, MySqlDbType.Int32, isUnsigned: false));
			AddColumnTypeMetadata(new("INT", typeUint, MySqlDbType.UInt32, isUnsigned: true));
			AddColumnTypeMetadata(new("MEDIUMINT", typeInt, MySqlDbType.Int24, isUnsigned: false));
			AddColumnTypeMetadata(new("MEDIUMINT", typeUint, MySqlDbType.UInt24, isUnsigned: true));
			AddColumnTypeMetadata(new("BIGINT", typeLong, MySqlDbType.Int64, isUnsigned: false));
			AddColumnTypeMetadata(new("BIGINT", typeUlong, MySqlDbType.UInt64, isUnsigned: true));
			AddColumnTypeMetadata(new("BIT", typeUlong, MySqlDbType.Bit));

			// decimals
			var typeDecimal = AddDbTypeMapping(new(typeof(decimal), new[] { DbType.Decimal, DbType.Currency, DbType.VarNumeric }, convert: static o => Convert.ToDecimal(o)));
			var typeDouble = AddDbTypeMapping(new(typeof(double), new[] { DbType.Double }, convert: static o => Convert.ToDouble(o)));
			var typeFloat = AddDbTypeMapping(new(typeof(float), new[] { DbType.Single }, convert: static o => Convert.ToSingle(o)));
			AddColumnTypeMetadata(new("DECIMAL", typeDecimal, MySqlDbType.NewDecimal, createFormat: "DECIMAL({0},{1});precision,scale"));
			AddColumnTypeMetadata(new("DECIMAL", typeDecimal, MySqlDbType.Decimal));
			AddColumnTypeMetadata(new("DOUBLE", typeDouble, MySqlDbType.Double));
			AddColumnTypeMetadata(new("FLOAT", typeFloat, MySqlDbType.Float));

			// string
			var typeFixedString = AddDbTypeMapping(new(typeof(string), new[] { DbType.StringFixedLength, DbType.AnsiStringFixedLength }, convert: Convert.ToString!));
			var typeString = AddDbTypeMapping(new(typeof(string), new[] { DbType.String, DbType.AnsiString, DbType.Xml }, convert: Convert.ToString!));
			AddColumnTypeMetadata(new("VARCHAR", typeString, MySqlDbType.VarChar, createFormat: "VARCHAR({0});size"));
			AddColumnTypeMetadata(new("VARCHAR", typeString, MySqlDbType.VarString));
			AddColumnTypeMetadata(new("CHAR", typeFixedString, MySqlDbType.String, createFormat: "CHAR({0});size"));
			AddColumnTypeMetadata(new("TINYTEXT", typeString, MySqlDbType.TinyText, columnSize: byte.MaxValue, simpleDataTypeName: "VARCHAR"));
			AddColumnTypeMetadata(new("TEXT", typeString, MySqlDbType.Text, columnSize: ushort.MaxValue, simpleDataTypeName: "VARCHAR"));
			AddColumnTypeMetadata(new("MEDIUMTEXT", typeString, MySqlDbType.MediumText, columnSize: 16777215, simpleDataTypeName: "VARCHAR"));
			AddColumnTypeMetadata(new("LONGTEXT", typeString, MySqlDbType.LongText, columnSize: uint.MaxValue, simpleDataTypeName: "VARCHAR"));
			AddColumnTypeMetadata(new("ENUM", typeString, MySqlDbType.Enum));
			AddColumnTypeMetadata(new("SET", typeString, MySqlDbType.Set));
			AddColumnTypeMetadata(new("JSON", typeString, MySqlDbType.JSON));

			// binary
			var typeBinary = AddDbTypeMapping(new(typeof(byte[]), new[] { DbType.Binary }));
			AddColumnTypeMetadata(new("BLOB", typeBinary, MySqlDbType.Blob, binary: true, columnSize: ushort.MaxValue, simpleDataTypeName: "BLOB"));
			AddColumnTypeMetadata(new("BINARY", typeBinary, MySqlDbType.Binary, binary: true, simpleDataTypeName: "BLOB", createFormat: "BINARY({0});length"));
			AddColumnTypeMetadata(new("VARBINARY", typeBinary, MySqlDbType.VarBinary, binary: true, simpleDataTypeName: "BLOB", createFormat: "VARBINARY({0});length"));
			AddColumnTypeMetadata(new("TINYBLOB", typeBinary, MySqlDbType.TinyBlob, binary: true, columnSize: byte.MaxValue, simpleDataTypeName: "BLOB"));
			AddColumnTypeMetadata(new("MEDIUMBLOB", typeBinary, MySqlDbType.MediumBlob, binary: true, columnSize: 16777215, simpleDataTypeName: "BLOB"));
			AddColumnTypeMetadata(new("LONGBLOB", typeBinary, MySqlDbType.LongBlob, binary: true, columnSize: uint.MaxValue, simpleDataTypeName: "BLOB"));

			// spatial
			AddColumnTypeMetadata(new("GEOMETRY", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new("POINT", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new("LINESTRING", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new("POLYGON", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new("MULTIPOINT", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new("MULTILINESTRING", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new("MULTIPOLYGON", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new("GEOMETRYCOLLECTION", typeBinary, MySqlDbType.Geometry, binary: true));
			AddColumnTypeMetadata(new("GEOMCOLLECTION", typeBinary, MySqlDbType.Geometry, binary: true));

			// date/time
			var typeDate = AddDbTypeMapping(new(typeof(DateTime), new[] { DbType.Date }));
			var typeDateTime = AddDbTypeMapping(new(typeof(DateTime), new[] { DbType.DateTime, DbType.DateTime2, DbType.DateTimeOffset }));
			AddDbTypeMapping(new(typeof(DateTimeOffset), new[] { DbType.DateTimeOffset }));
			var typeTime = AddDbTypeMapping(new(typeof(TimeSpan), new[] { DbType.Time }, convert: static o => o is string s ? Utility.ParseTimeSpan(Encoding.UTF8.GetBytes(s)) : Convert.ChangeType(o, typeof(TimeSpan))));
			AddColumnTypeMetadata(new("DATETIME", typeDateTime, MySqlDbType.DateTime));
			AddColumnTypeMetadata(new("DATE", typeDate, MySqlDbType.Date));
			AddColumnTypeMetadata(new("DATE", typeDate, MySqlDbType.Newdate));
			AddColumnTypeMetadata(new("TIME", typeTime, MySqlDbType.Time));
			AddColumnTypeMetadata(new("TIMESTAMP", typeDateTime, MySqlDbType.Timestamp));
			AddColumnTypeMetadata(new("YEAR", typeInt, MySqlDbType.Year));

			// guid
				var typeGuid = AddDbTypeMapping(new(typeof(Guid), new[] { DbType.Guid }, convert: static o => Guid.Parse(Convert.ToString(o)!)));
			AddColumnTypeMetadata(new("CHAR", typeGuid, MySqlDbType.Guid, length: 36, simpleDataTypeName: "CHAR(36)", createFormat: "CHAR(36)"));

			// null
			var typeNull = AddDbTypeMapping(new(typeof(object), new[] { DbType.Object }));
			AddColumnTypeMetadata(new("NULL", typeNull, MySqlDbType.Null));
		}

		public IReadOnlyList<ColumnTypeMetadata> GetColumnTypeMetadata() => m_columnTypeMetadata.AsReadOnly();

		public ColumnTypeMetadata GetColumnTypeMetadata(MySqlDbType mySqlDbType) => m_mySqlDbTypeToColumnTypeMetadata[mySqlDbType];

		public DbType GetDbTypeForMySqlDbType(MySqlDbType mySqlDbType) => m_mySqlDbTypeToColumnTypeMetadata[mySqlDbType].DbTypeMapping.DbTypes[0];

		public MySqlDbType GetMySqlDbTypeForDbType(DbType dbType)
		{
			foreach (var pair in m_mySqlDbTypeToColumnTypeMetadata)
			{
				if (pair.Value.DbTypeMapping.DbTypes.Contains(dbType))
					return pair.Key;
			}
			return MySqlDbType.VarChar;
		}

		private DbTypeMapping AddDbTypeMapping(DbTypeMapping dbTypeMapping)
		{
			m_dbTypeMappingsByClrType[dbTypeMapping.ClrType] = dbTypeMapping;

			if (dbTypeMapping.DbTypes is not null)
				foreach (var dbType in dbTypeMapping.DbTypes)
					m_dbTypeMappingsByDbType[dbType] = dbTypeMapping;

			return dbTypeMapping;
		}

		private void AddColumnTypeMetadata(ColumnTypeMetadata columnTypeMetadata)
		{
			m_columnTypeMetadata.Add(columnTypeMetadata);
			var lookupKey = columnTypeMetadata.CreateLookupKey();
			if (!m_columnTypeMetadataLookup.ContainsKey(lookupKey))
				m_columnTypeMetadataLookup.Add(lookupKey, columnTypeMetadata);
			if (!m_mySqlDbTypeToColumnTypeMetadata.ContainsKey(columnTypeMetadata.MySqlDbType))
				m_mySqlDbTypeToColumnTypeMetadata.Add(columnTypeMetadata.MySqlDbType, columnTypeMetadata);
		}

		internal DbTypeMapping? GetDbTypeMapping(Type clrType)
		{
#if !NETSTANDARD1_3
			if (clrType.IsEnum)
				clrType = Enum.GetUnderlyingType(clrType);
#endif
			m_dbTypeMappingsByClrType.TryGetValue(clrType, out var dbTypeMapping);
			return dbTypeMapping;
		}

		internal DbTypeMapping? GetDbTypeMapping(DbType dbType)
		{
			m_dbTypeMappingsByDbType.TryGetValue(dbType, out var dbTypeMapping);
			return dbTypeMapping;
		}

		public DbTypeMapping? GetDbTypeMapping(string columnTypeName, bool unsigned = false, int length = 0)
		{
			return GetColumnTypeMetadata(columnTypeName, unsigned, length)?.DbTypeMapping;
		}

		public MySqlDbType GetMySqlDbType(string typeName, bool unsigned, int length) => GetColumnTypeMetadata(typeName, unsigned, length)!.MySqlDbType;

		private ColumnTypeMetadata? GetColumnTypeMetadata(string columnTypeName, bool unsigned, int length)
		{
			if (!m_columnTypeMetadataLookup.TryGetValue(ColumnTypeMetadata.CreateLookupKey(columnTypeName, unsigned, length), out var columnTypeMetadata) && length != 0)
				m_columnTypeMetadataLookup.TryGetValue(ColumnTypeMetadata.CreateLookupKey(columnTypeName, unsigned, 0), out columnTypeMetadata);
			return columnTypeMetadata;
		}

		public static MySqlDbType ConvertToMySqlDbType(ColumnDefinitionPayload columnDefinition, bool treatTinyAsBoolean, MySqlGuidFormat guidFormat)
		{
			var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
			switch (columnDefinition.ColumnType)
			{
			case ColumnType.Tiny:
				return treatTinyAsBoolean && columnDefinition.ColumnLength == 1 && !isUnsigned ? MySqlDbType.Bool :
					isUnsigned ? MySqlDbType.UByte : MySqlDbType.Byte;

			case ColumnType.Int24:
				return isUnsigned ? MySqlDbType.UInt24 : MySqlDbType.Int24;

			case ColumnType.Long:
				return isUnsigned ? MySqlDbType.UInt32 : MySqlDbType.Int32;

			case ColumnType.Longlong:
				return isUnsigned ? MySqlDbType.UInt64 : MySqlDbType.Int64;

			case ColumnType.Bit:
				return MySqlDbType.Bit;

			case ColumnType.String:
				if (guidFormat == MySqlGuidFormat.Char36 && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
					return MySqlDbType.Guid;
				if (guidFormat == MySqlGuidFormat.Char32 && columnDefinition.ColumnLength / ProtocolUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 32)
					return MySqlDbType.Guid;
				if ((columnDefinition.ColumnFlags & ColumnFlags.Enum) != 0)
					return MySqlDbType.Enum;
				if ((columnDefinition.ColumnFlags & ColumnFlags.Set) != 0)
					return MySqlDbType.Set;
				goto case ColumnType.VarString;

			case ColumnType.VarChar:
			case ColumnType.VarString:
			case ColumnType.TinyBlob:
			case ColumnType.Blob:
			case ColumnType.MediumBlob:
			case ColumnType.LongBlob:
				var type = columnDefinition.ColumnType;
				if (columnDefinition.CharacterSet == CharacterSet.Binary)
				{
					if ((guidFormat is MySqlGuidFormat.Binary16 or MySqlGuidFormat.TimeSwapBinary16 or MySqlGuidFormat.LittleEndianBinary16) && columnDefinition.ColumnLength == 16)
						return MySqlDbType.Guid;

					return type switch
					{
						ColumnType.String => MySqlDbType.Binary,
						ColumnType.VarString => MySqlDbType.VarBinary,
						ColumnType.TinyBlob => MySqlDbType.TinyBlob,
						ColumnType.Blob => MySqlDbType.Blob,
						ColumnType.MediumBlob => MySqlDbType.MediumBlob,
						_ => MySqlDbType.LongBlob,
					};
				}
				return type switch
				{
					ColumnType.String => MySqlDbType.String,
					ColumnType.VarString => MySqlDbType.VarChar,
					ColumnType.TinyBlob => MySqlDbType.TinyText,
					ColumnType.Blob => MySqlDbType.Text,
					ColumnType.MediumBlob => MySqlDbType.MediumText,
					_ => MySqlDbType.LongText,
				};

			case ColumnType.Json:
				return MySqlDbType.JSON;

			case ColumnType.Short:
				return isUnsigned ? MySqlDbType.UInt16 : MySqlDbType.Int16;

			case ColumnType.Date:
			case ColumnType.NewDate:
				return MySqlDbType.Date;

			case ColumnType.DateTime:
				return MySqlDbType.DateTime;

			case ColumnType.Timestamp:
				return MySqlDbType.Timestamp;

			case ColumnType.Time:
				return MySqlDbType.Time;

			case ColumnType.Year:
				return MySqlDbType.Year;

			case ColumnType.Float:
				return MySqlDbType.Float;

			case ColumnType.Double:
				return MySqlDbType.Double;

			case ColumnType.Decimal:
				return MySqlDbType.Decimal;

			case ColumnType.NewDecimal:
				return MySqlDbType.NewDecimal;

			case ColumnType.Geometry:
				return MySqlDbType.Geometry;

			case ColumnType.Null:
				return MySqlDbType.Null;

			case ColumnType.Enum:
				return MySqlDbType.Enum;

			case ColumnType.Set:
				return MySqlDbType.Set;

			default:
				throw new NotImplementedException("ConvertToMySqlDbType for {0} is not implemented".FormatInvariant(columnDefinition.ColumnType));
			}
		}

		public static ushort ConvertToColumnTypeAndFlags(MySqlDbType dbType, MySqlGuidFormat guidFormat)
		{
			var isUnsigned = dbType is MySqlDbType.UByte or MySqlDbType.UInt16 or MySqlDbType.UInt24 or MySqlDbType.UInt32 or MySqlDbType.UInt64;
			var columnType = dbType switch
			{
				MySqlDbType.Bool or MySqlDbType.Byte or MySqlDbType.UByte => ColumnType.Tiny,
				MySqlDbType.Int16 or MySqlDbType.UInt16 => ColumnType.Short,
				MySqlDbType.Int24 or MySqlDbType.UInt24 => ColumnType.Int24,
				MySqlDbType.Int32 or MySqlDbType.UInt32 => ColumnType.Long,
				MySqlDbType.Int64 or MySqlDbType.UInt64 => ColumnType.Longlong,
				MySqlDbType.Bit => ColumnType.Bit,
				MySqlDbType.Guid => (guidFormat is MySqlGuidFormat.Char36 or MySqlGuidFormat.Char32) ? ColumnType.String : ColumnType.Blob,
				MySqlDbType.Enum or MySqlDbType.Set => ColumnType.String,
				MySqlDbType.Binary or MySqlDbType.String => ColumnType.String,
				MySqlDbType.VarBinary or MySqlDbType.VarChar or MySqlDbType.VarString => ColumnType.VarString,
				MySqlDbType.TinyBlob or MySqlDbType.TinyText => ColumnType.TinyBlob,
				MySqlDbType.Blob or MySqlDbType.Text => ColumnType.Blob,
				MySqlDbType.MediumBlob or MySqlDbType.MediumText => ColumnType.MediumBlob,
				MySqlDbType.LongBlob or MySqlDbType.LongText => ColumnType.LongBlob,
				MySqlDbType.JSON => ColumnType.Json, // TODO: test
				MySqlDbType.Date or MySqlDbType.Newdate => ColumnType.Date,
				MySqlDbType.DateTime => ColumnType.DateTime,
				MySqlDbType.Timestamp => ColumnType.Timestamp,
				MySqlDbType.Time => ColumnType.Time,
				MySqlDbType.Year => ColumnType.Year,
				MySqlDbType.Float => ColumnType.Float,
				MySqlDbType.Double => ColumnType.Double,
				MySqlDbType.Decimal => ColumnType.Decimal,
				MySqlDbType.NewDecimal => ColumnType.NewDecimal,
				MySqlDbType.Geometry => ColumnType.Geometry,
				MySqlDbType.Null => ColumnType.Null,
				_ => throw new NotImplementedException("ConvertToColumnTypeAndFlags for {0} is not implemented".FormatInvariant(dbType)),
			};
			return (ushort) ((byte) columnType | (isUnsigned ? 0x8000 : 0));
		}

		internal IEnumerable<ColumnTypeMetadata> GetColumnMappings()
		{
			return m_columnTypeMetadataLookup.Values.AsEnumerable();
		}

		readonly List<ColumnTypeMetadata> m_columnTypeMetadata;
		readonly Dictionary<Type, DbTypeMapping> m_dbTypeMappingsByClrType;
		readonly Dictionary<DbType, DbTypeMapping> m_dbTypeMappingsByDbType;
		readonly Dictionary<string, ColumnTypeMetadata> m_columnTypeMetadataLookup;
		readonly Dictionary<MySqlDbType, ColumnTypeMetadata> m_mySqlDbTypeToColumnTypeMetadata;
	}
}
