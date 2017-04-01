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
			// boolean
			var typeBoolean = AddDbTypeMapping(new DbTypeMapping(typeof(bool), new []{DbType.Boolean}, convert: o => Convert.ToBoolean(o)));
			AddColumnTypeMapping(new ColumnTypeMapping("bit",     typeBoolean, new []{ColumnType.Bit}));
			AddColumnTypeMapping(new ColumnTypeMapping("tinyint", typeBoolean, new []{ColumnType.Tiny}, unsigned: false, length: 1));
			AddColumnTypeMapping(new ColumnTypeMapping("tinyint", typeBoolean, new []{ColumnType.Tiny}, unsigned: true, length: 1));

			// integers
			var typeSbyte    = AddDbTypeMapping(new DbTypeMapping(typeof(sbyte),  new []{DbType.SByte},  convert: o => Convert.ToSByte(o)));
			var typeByte     = AddDbTypeMapping(new DbTypeMapping(typeof(byte),   new []{DbType.Byte},   convert: o => Convert.ToByte(o)));
			var typeShort    = AddDbTypeMapping(new DbTypeMapping(typeof(short),  new []{DbType.Int16},  convert: o => Convert.ToInt16(o)));
			var typeUshort   = AddDbTypeMapping(new DbTypeMapping(typeof(ushort), new []{DbType.UInt16}, convert: o => Convert.ToUInt16(o)));
			var typeInt      = AddDbTypeMapping(new DbTypeMapping(typeof(int),    new []{DbType.Int32},  convert: o => Convert.ToInt32(o)));
			var typeUint     = AddDbTypeMapping(new DbTypeMapping(typeof(uint),   new []{DbType.UInt32}, convert: o => Convert.ToUInt32(o)));
			var typeLong     = AddDbTypeMapping(new DbTypeMapping(typeof(long),   new []{DbType.Int64},  convert: o => Convert.ToInt64(o)));
			var typeUlong    = AddDbTypeMapping(new DbTypeMapping(typeof(ulong),  new []{DbType.UInt64}, convert: o => Convert.ToUInt64(o)));
			AddColumnTypeMapping(new ColumnTypeMapping("tinyint",    typeSbyte,	  new []{ColumnType.Tiny},     unsigned: false));
			AddColumnTypeMapping(new ColumnTypeMapping("tinyint",    typeByte,    new []{ColumnType.Tiny},     unsigned: true));
			AddColumnTypeMapping(new ColumnTypeMapping("smallint",   typeShort,   new []{ColumnType.Short},    unsigned: false));
			AddColumnTypeMapping(new ColumnTypeMapping("smallint",   typeUshort,  new []{ColumnType.Short},    unsigned: true));
			AddColumnTypeMapping(new ColumnTypeMapping("mediumint",  typeInt,     new []{ColumnType.Int24},    unsigned: false));
			AddColumnTypeMapping(new ColumnTypeMapping("mediumint",  typeUint,    new []{ColumnType.Int24},    unsigned: true));
			AddColumnTypeMapping(new ColumnTypeMapping("int",        typeInt,     new []{ColumnType.Long},     unsigned: false));
			AddColumnTypeMapping(new ColumnTypeMapping("int",        typeUint,    new []{ColumnType.Long},     unsigned: true));
			AddColumnTypeMapping(new ColumnTypeMapping("bigint",     typeLong,    new []{ColumnType.Longlong}, unsigned: false));
			AddColumnTypeMapping(new ColumnTypeMapping("bigint",     typeUlong,   new []{ColumnType.Longlong}, unsigned: true));

			// decimals
			var typeDecimal    = AddDbTypeMapping(new DbTypeMapping(typeof(decimal), new []{DbType.Decimal}, convert: o => Convert.ToDecimal(o)));
			var typeDouble     = AddDbTypeMapping(new DbTypeMapping(typeof(double),  new []{DbType.Double},  convert: o => Convert.ToDouble(o)));
			var typeFloat      = AddDbTypeMapping(new DbTypeMapping(typeof(float),   new []{DbType.Single},  convert: o => Convert.ToSingle(o)));
			AddColumnTypeMapping(new ColumnTypeMapping("decimal",  typeDecimal, new []{ColumnType.Decimal, ColumnType.NewDecimal}));
			AddColumnTypeMapping(new ColumnTypeMapping("double",   typeDouble,  new []{ColumnType.Double}));
			AddColumnTypeMapping(new ColumnTypeMapping("float",    typeFloat,   new []{ColumnType.Float}));

			// string
			var typeString          = AddDbTypeMapping(new DbTypeMapping(typeof(string), new []{DbType.String, DbType.StringFixedLength, DbType.AnsiString, DbType.AnsiStringFixedLength}, convert: o => Convert.ToString(o)));
			AddColumnTypeMapping(new ColumnTypeMapping("char",       typeString, new []{ColumnType.String}));
			AddColumnTypeMapping(new ColumnTypeMapping("varchar",    typeString, new []{ColumnType.VarChar, ColumnType.VarString}));
			AddColumnTypeMapping(new ColumnTypeMapping("tinytext",   typeString, new []{ColumnType.TinyBlob}));
			AddColumnTypeMapping(new ColumnTypeMapping("text",       typeString, new []{ColumnType.Blob}));
			AddColumnTypeMapping(new ColumnTypeMapping("mediumtext", typeString, new []{ColumnType.MediumBlob}));
			AddColumnTypeMapping(new ColumnTypeMapping("longtext",   typeString, new []{ColumnType.LongBlob}));
			AddColumnTypeMapping(new ColumnTypeMapping("enum",       typeString, new []{ColumnType.Enum}));
			AddColumnTypeMapping(new ColumnTypeMapping("set",        typeString, new []{ColumnType.Set}));
			AddColumnTypeMapping(new ColumnTypeMapping("json",       typeString, new []{ColumnType.Json}));

			// binary
			var typeBinary = AddDbTypeMapping(new DbTypeMapping(typeof(byte[]), new []{DbType.Binary}));
			AddColumnTypeMapping(new ColumnTypeMapping("binary",     typeBinary, new []{ColumnType.String},     binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("varbinary",  typeBinary, new []{ColumnType.VarString},  binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("tinyblob",   typeBinary, new []{ColumnType.TinyBlob},   binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("blob",       typeBinary, new []{ColumnType.Blob},       binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("mediumblob", typeBinary, new []{ColumnType.MediumBlob}, binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("longblob",   typeBinary, new []{ColumnType.LongBlob},   binary: true));

			// spatial
			AddColumnTypeMapping(new ColumnTypeMapping("point",              typeBinary, new []{ColumnType.Geometry}, binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("linestring",         typeBinary, new []{ColumnType.Geometry}, binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("polygon",            typeBinary, new []{ColumnType.Geometry}, binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("geometry",           typeBinary, new []{ColumnType.Geometry}, binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("multipoint",         typeBinary, new []{ColumnType.Geometry}, binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("multilinestring",    typeBinary, new []{ColumnType.Geometry}, binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("multipolygon",       typeBinary, new []{ColumnType.Geometry}, binary: true));
			AddColumnTypeMapping(new ColumnTypeMapping("geometrycollection", typeBinary, new []{ColumnType.Geometry}, binary: true));

			// date/time
			var typeDateTime       = AddDbTypeMapping(new DbTypeMapping(typeof(DateTime), new []{DbType.DateTime, DbType.Date, DbType.DateTime2}));
			var typeTime           = AddDbTypeMapping(new DbTypeMapping(typeof(TimeSpan), new []{DbType.Time}));
			var typeDateTimeOffset = AddDbTypeMapping(new DbTypeMapping(typeof(DateTimeOffset), new []{DbType.DateTimeOffset}));
			AddColumnTypeMapping(new ColumnTypeMapping("datetime",  typeDateTime,       new []{ColumnType.DateTime}));
			AddColumnTypeMapping(new ColumnTypeMapping("date",      typeDateTime,       new []{ColumnType.Date}));
			AddColumnTypeMapping(new ColumnTypeMapping("time",      typeTime,           new []{ColumnType.Time}));
			AddColumnTypeMapping(new ColumnTypeMapping("timestamp", typeDateTimeOffset, new []{ColumnType.Timestamp}));
			AddColumnTypeMapping(new ColumnTypeMapping("year",      typeInt,            new []{ColumnType.Year}));

			// guid
			AddDbTypeMapping(new DbTypeMapping(typeof(Guid), new[]{DbType.Guid}, convert: o => Guid.Parse(Convert.ToString(o))));
		}

		private DbTypeMapping AddDbTypeMapping(DbTypeMapping dbTypeMapping)
		{
			m_dbTypeMappingsByClrType[dbTypeMapping.ClrType] = dbTypeMapping;

			if (dbTypeMapping.DbTypes != null)
				foreach (var dbType in dbTypeMapping.DbTypes)
					m_dbTypeMappingsByDbType[dbType] = dbTypeMapping;

			return dbTypeMapping;
		}

		private void AddColumnTypeMapping(ColumnTypeMapping columnTypeMapping)
		{
			m_columnTypeMappingLookup[columnTypeMapping.LookupKey] = columnTypeMapping;
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

		private Dictionary<Type, DbTypeMapping> m_dbTypeMappingsByClrType = new Dictionary<Type, DbTypeMapping>();
		private Dictionary<DbType, DbTypeMapping> m_dbTypeMappingsByDbType = new Dictionary<DbType, DbTypeMapping>();
		private Dictionary<string, ColumnTypeMapping> m_columnTypeMappingLookup = new Dictionary<string, ColumnTypeMapping>();
	}
}
