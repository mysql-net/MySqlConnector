using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.Serialization;

namespace MySql.Data.MySqlClient.Types
{
	internal class TypeMapper
	{
		internal static TypeMapper Mapper = new TypeMapper();

		private TypeMapper()
		{
			m_dbTypeMappingsByClrType = new Dictionary<Type, DbTypeMapping>();
			m_dbTypeMappingsByDbType = new Dictionary<DbType, DbTypeMapping>();
			m_columnTypeMappingLookup = new Dictionary<string, ColumnTypeMapping>(StringComparer.OrdinalIgnoreCase);
			m_mySqlDbTypeToColumnTypeMapping = new Dictionary<MySqlDbType, ColumnTypeMapping>();

			// boolean
			var typeBoolean = AddDbTypeMapping(new DbTypeMapping(typeof(bool), new[] { DbType.Boolean }, convert: o => Convert.ToBoolean(o)));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("TINYINT", typeBoolean, MySqlDbType.Bool, unsigned: false, length: 1, simpleDataTypeName: "BOOL"));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("TINYINT", typeBoolean, MySqlDbType.Bool, unsigned: true, length: 1));

			// integers
			var typeSbyte = AddDbTypeMapping(new DbTypeMapping(typeof(sbyte), new[] { DbType.SByte }, convert: o => Convert.ToSByte(o)));
			var typeByte = AddDbTypeMapping(new DbTypeMapping(typeof(byte), new[] { DbType.Byte }, convert: o => Convert.ToByte(o)));
			var typeShort = AddDbTypeMapping(new DbTypeMapping(typeof(short), new[] { DbType.Int16 }, convert: o => Convert.ToInt16(o)));
			var typeUshort = AddDbTypeMapping(new DbTypeMapping(typeof(ushort), new[] { DbType.UInt16 }, convert: o => Convert.ToUInt16(o)));
			var typeInt = AddDbTypeMapping(new DbTypeMapping(typeof(int), new[] { DbType.Int32 }, convert: o => Convert.ToInt32(o)));
			var typeUint = AddDbTypeMapping(new DbTypeMapping(typeof(uint), new[] { DbType.UInt32 }, convert: o => Convert.ToUInt32(o)));
			var typeLong = AddDbTypeMapping(new DbTypeMapping(typeof(long), new[] { DbType.Int64 }, convert: o => Convert.ToInt64(o)));
			var typeUlong = AddDbTypeMapping(new DbTypeMapping(typeof(ulong), new[] { DbType.UInt64 }, convert: o => Convert.ToUInt64(o)));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("TINYINT", typeSbyte, MySqlDbType.Byte, unsigned: false));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("TINYINT", typeByte, MySqlDbType.UByte, unsigned: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("SMALLINT", typeShort, MySqlDbType.Int16, unsigned: false));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("SMALLINT", typeUshort, MySqlDbType.UInt16, unsigned: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("INT", typeInt, MySqlDbType.Int32, unsigned: false));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("INT", typeUint, MySqlDbType.UInt32, unsigned: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("MEDIUMINT", typeInt, MySqlDbType.Int24, unsigned: false));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("MEDIUMINT", typeUint, MySqlDbType.UInt24, unsigned: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("BIGINT", typeLong, MySqlDbType.Int64, unsigned: false));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("BIGINT", typeUlong, MySqlDbType.UInt64, unsigned: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("BIT", typeUlong, MySqlDbType.Bit));

			// decimals
			var typeDecimal = AddDbTypeMapping(new DbTypeMapping(typeof(decimal), new[] { DbType.Decimal, DbType.Currency, DbType.VarNumeric }, convert: o => Convert.ToDecimal(o)));
			var typeDouble = AddDbTypeMapping(new DbTypeMapping(typeof(double), new[] { DbType.Double }, convert: o => Convert.ToDouble(o)));
			var typeFloat = AddDbTypeMapping(new DbTypeMapping(typeof(float), new[] { DbType.Single }, convert: o => Convert.ToSingle(o)));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("DECIMAL", typeDecimal, MySqlDbType.NewDecimal));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("DECIMAL", typeDecimal, MySqlDbType.Decimal));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("DOUBLE", typeDouble, MySqlDbType.Double));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("FLOAT", typeFloat, MySqlDbType.Float));

			// string
			var typeFixedString = AddDbTypeMapping(new DbTypeMapping(typeof(string), new[] { DbType.StringFixedLength, DbType.AnsiStringFixedLength }, convert: Convert.ToString));
			var typeString = AddDbTypeMapping(new DbTypeMapping(typeof(string), new[] { DbType.String, DbType.AnsiString, DbType.Xml }, convert: Convert.ToString));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("CHAR", typeFixedString, MySqlDbType.String));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("VARCHAR", typeString, MySqlDbType.VarChar));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("VARCHAR", typeString, MySqlDbType.VarString));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("TINYTEXT", typeString, MySqlDbType.TinyText, simpleDataTypeName: "VARCHAR"));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("TEXT", typeString, MySqlDbType.Text, simpleDataTypeName: "VARCHAR"));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("MEDIUMTEXT", typeString, MySqlDbType.MediumText, simpleDataTypeName: "VARCHAR"));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("LONGTEXT", typeString, MySqlDbType.LongText, simpleDataTypeName: "VARCHAR"));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("ENUM", typeString, MySqlDbType.Enum));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("SET", typeString, MySqlDbType.Set));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("JSON", typeString, MySqlDbType.JSON));

			// binary
			var typeBinary = AddDbTypeMapping(new DbTypeMapping(typeof(byte[]), new[] { DbType.Binary }));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("BLOB", typeBinary, MySqlDbType.Blob, binary: true, simpleDataTypeName: "BLOB"));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("BINARY", typeBinary, MySqlDbType.Binary, binary: true, simpleDataTypeName: "BLOB"));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("VARBINARY", typeBinary, MySqlDbType.VarBinary, binary: true, simpleDataTypeName: "BLOB"));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("TINYBLOB", typeBinary, MySqlDbType.TinyBlob, binary: true, simpleDataTypeName: "BLOB"));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("MEDIUMBLOB", typeBinary, MySqlDbType.MediumBlob, binary: true, simpleDataTypeName: "BLOB"));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("LONGBLOB", typeBinary, MySqlDbType.LongBlob, binary: true, simpleDataTypeName: "BLOB"));

			// spatial
			AddMySqlDbTypeMapping(new ColumnTypeMapping("GEOMETRY", typeBinary, MySqlDbType.Geometry, binary: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("POINT", typeBinary, MySqlDbType.Geometry, binary: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("LINESTRING", typeBinary, MySqlDbType.Geometry, binary: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("POLYGON", typeBinary, MySqlDbType.Geometry, binary: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("MULTIPOINT", typeBinary, MySqlDbType.Geometry, binary: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("MULTILINESTRING", typeBinary, MySqlDbType.Geometry, binary: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("MULTIPOLYGON", typeBinary, MySqlDbType.Geometry, binary: true));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("GEOMETRYCOLLECTION", typeBinary, MySqlDbType.Geometry, binary: true));

			// date/time
			var typeDate = AddDbTypeMapping(new DbTypeMapping(typeof(DateTime), new[] { DbType.Date }));
			var typeDateTime = AddDbTypeMapping(new DbTypeMapping(typeof(DateTime), new[] { DbType.DateTime, DbType.DateTime2, DbType.DateTimeOffset }));
			var typeTime = AddDbTypeMapping(new DbTypeMapping(typeof(TimeSpan), new[] { DbType.Time }));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("DATETIME", typeDateTime, MySqlDbType.DateTime));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("DATE", typeDate, MySqlDbType.Date));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("DATE", typeDate, MySqlDbType.Newdate));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("TIME", typeTime, MySqlDbType.Time));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("TIMESTAMP", typeDateTime, MySqlDbType.Timestamp));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("YEAR", typeInt, MySqlDbType.Year));

			// guid
			var typeGuid = AddDbTypeMapping(new DbTypeMapping(typeof(Guid), new[] { DbType.Guid }, convert: o => Guid.Parse(Convert.ToString(o))));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("CHAR", typeGuid, MySqlDbType.Guid, length: 36, simpleDataTypeName: "CHAR(36)"));

			// null
			var typeNull = AddDbTypeMapping(new DbTypeMapping(typeof(object), new[] { DbType.Object }, convert: o => null));
			AddMySqlDbTypeMapping(new ColumnTypeMapping("NULL", typeNull, MySqlDbType.Null));
		}

		public ColumnTypeMapping GetColumnMapping(MySqlDbType mySqlDbType) => m_mySqlDbTypeToColumnTypeMapping[mySqlDbType];

		public DbType GetDbTypeForMySqlDbType(MySqlDbType mySqlDbType) => m_mySqlDbTypeToColumnTypeMapping[mySqlDbType].DbTypeMapping.DbTypes[0];

		public MySqlDbType GetMySqlDbTypeForDbType(DbType dbType)
		{
			foreach (var pair in m_mySqlDbTypeToColumnTypeMapping)
			{
				if (pair.Value.DbTypeMapping.DbTypes.Contains(dbType))
					return pair.Key;
			}
			return MySqlDbType.VarChar;
		}

		private DbTypeMapping AddDbTypeMapping(DbTypeMapping dbTypeMapping)
		{
			m_dbTypeMappingsByClrType[dbTypeMapping.ClrType] = dbTypeMapping;

			if (dbTypeMapping.DbTypes != null)
				foreach (var dbType in dbTypeMapping.DbTypes)
					m_dbTypeMappingsByDbType[dbType] = dbTypeMapping;

			return dbTypeMapping;
		}

		private void AddMySqlDbTypeMapping(ColumnTypeMapping columnTypeMapping)
		{
			var lookupKey = columnTypeMapping.CreateLookupKey();
			if (!m_columnTypeMappingLookup.ContainsKey(lookupKey))
				m_columnTypeMappingLookup.Add(lookupKey, columnTypeMapping);
			if (!m_mySqlDbTypeToColumnTypeMapping.ContainsKey(columnTypeMapping.MySqlDbType))
				m_mySqlDbTypeToColumnTypeMapping.Add(columnTypeMapping.MySqlDbType, columnTypeMapping);
		}

		internal DbTypeMapping GetDbTypeMapping(Type clrType)
		{
			m_dbTypeMappingsByClrType.TryGetValue(clrType, out var dbTypeMapping);
			return dbTypeMapping;
		}

		internal DbTypeMapping GetDbTypeMapping(DbType dbType)
		{
			m_dbTypeMappingsByDbType.TryGetValue(dbType, out var dbTypeMapping);
			return dbTypeMapping;
		}

		internal DbTypeMapping GetDbTypeMapping(string columnTypeName, bool unsigned=false, int length=0)
		{
			return GetColumnTypeMapping(columnTypeName, unsigned, length)?.DbTypeMapping;
		}

		internal ColumnTypeMapping GetColumnTypeMapping(string columnTypeName, bool unsigned=false, int length=0)
		{
			if (!m_columnTypeMappingLookup.TryGetValue(ColumnTypeMapping.CreateLookupKey(columnTypeName, unsigned, length), out var columnTypeMapping) && length != 0)
				m_columnTypeMappingLookup.TryGetValue(ColumnTypeMapping.CreateLookupKey(columnTypeName, unsigned, 0), out columnTypeMapping);
			return columnTypeMapping;
		}

		public static MySqlDbType ConvertToMySqlDbType(ColumnDefinitionPayload columnDefinition, bool treatTinyAsBoolean, bool oldGuids)
		{
			var isUnsigned = (columnDefinition.ColumnFlags & ColumnFlags.Unsigned) != 0;
			switch (columnDefinition.ColumnType)
			{
			case ColumnType.Tiny:
				return treatTinyAsBoolean && columnDefinition.ColumnLength == 1 ? MySqlDbType.Bool :
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
				if (!oldGuids && columnDefinition.ColumnLength / SerializationUtility.GetBytesPerCharacter(columnDefinition.CharacterSet) == 36)
					return MySqlDbType.Guid;
				if ((columnDefinition.ColumnFlags & ColumnFlags.Enum) != 0)
					return MySqlDbType.Enum;
				if ((columnDefinition.ColumnFlags & ColumnFlags.Set) != 0)
					return MySqlDbType.Set;
				goto case ColumnType.VarString;

			case ColumnType.VarString:
			case ColumnType.TinyBlob:
			case ColumnType.Blob:
			case ColumnType.MediumBlob:
			case ColumnType.LongBlob:
				var type = columnDefinition.ColumnType;
				if (columnDefinition.CharacterSet == CharacterSet.Binary)
				{
					if (oldGuids && columnDefinition.ColumnLength == 16)
						return MySqlDbType.Guid;

					return type == ColumnType.String ? MySqlDbType.Binary :
						type == ColumnType.VarString ? MySqlDbType.VarBinary :
						type == ColumnType.TinyBlob ? MySqlDbType.TinyBlob :
						type == ColumnType.Blob ? MySqlDbType.Blob :
						type == ColumnType.MediumBlob ? MySqlDbType.MediumBlob :
						MySqlDbType.LongBlob;
				}
				return type == ColumnType.String ? MySqlDbType.String :
					type == ColumnType.VarString ? MySqlDbType.VarChar :
					type == ColumnType.TinyBlob ? MySqlDbType.TinyText :
					type == ColumnType.Blob ? MySqlDbType.Text:
					type == ColumnType.MediumBlob ? MySqlDbType.MediumText :
					MySqlDbType.LongText;

			case ColumnType.Json:
				return MySqlDbType.JSON;

			case ColumnType.Short:
				return isUnsigned ? MySqlDbType.UInt16 : MySqlDbType.Int16;

			case ColumnType.Date:
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

			case ColumnType.Null:
				return MySqlDbType.Null;

			default:
				throw new NotImplementedException("ConvertToMySqlDbType for {0} is not implemented".FormatInvariant(columnDefinition.ColumnType));
			}
		}

		internal IEnumerable<ColumnTypeMapping> GetColumnMappings()
		{
			return m_columnTypeMappingLookup.Values.AsEnumerable();
		}

		readonly Dictionary<Type, DbTypeMapping> m_dbTypeMappingsByClrType;
		readonly Dictionary<DbType, DbTypeMapping> m_dbTypeMappingsByDbType;
		readonly Dictionary<string, ColumnTypeMapping> m_columnTypeMappingLookup;
		readonly Dictionary<MySqlDbType, ColumnTypeMapping> m_mySqlDbTypeToColumnTypeMapping;
	}
}
