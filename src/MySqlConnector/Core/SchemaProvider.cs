#if !NETSTANDARD1_3
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient;

namespace MySqlConnector.Core
{
	internal sealed class SchemaProvider
	{
		public SchemaProvider(MySqlConnection connection)
		{
			m_connection = connection;
			m_schemaCollections = new Dictionary<string, Action<DataTable>>
			{
				{ "MetaDataCollections", FillMetadataCollections },
				{ "DataTypes", FillDataTypes },
				{ "Procedures", FillProcedures },
				{ "ReservedWords", FillReservedWords }
			};
		}

		public DataTable GetSchema() => GetSchema("MetaDataCollections");

		public DataTable GetSchema(string collectionName)
		{
			if (!m_schemaCollections.TryGetValue(collectionName, out var fillAction))
				throw new ArgumentException("Invalid collection name.", nameof(collectionName));

			var dataTable = new DataTable(collectionName);
			fillAction(dataTable);
			return dataTable;
		}

		private void FillMetadataCollections(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[] {
				new DataColumn("CollectionName", typeof(string)),
				new DataColumn("NumberOfRestrictions", typeof(int)),
				new DataColumn("NumberOfIdentifierParts", typeof(int))
			});

			foreach (var collectionName in m_schemaCollections.Keys)
				dataTable.Rows.Add(collectionName, 0, 0);
		}

		private void FillDataTypes(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("TypeName", typeof(string)),
				new DataColumn("ProviderDbType", typeof(int)),
				new DataColumn("ColumnSize", typeof(long)),
				new DataColumn("CreateFormat", typeof(string)),
				new DataColumn("CreateParameters", typeof(string)),
				new DataColumn("DataType", typeof(string)),
				new DataColumn("IsAutoIncrementable", typeof(bool)),
				new DataColumn("IsBestMatch", typeof(bool)),
				new DataColumn("IsCaseSensitive", typeof(bool)),
				new DataColumn("IsFixedLength", typeof(bool)),
				new DataColumn("IsFixedPrecisionScale", typeof(bool)),
				new DataColumn("IsLong", typeof(bool)),
				new DataColumn("IsNullable", typeof(bool)),
				new DataColumn("IsSearchable", typeof(bool)),
				new DataColumn("IsSearchableWithLike", typeof(bool)),
				new DataColumn("IsUnsigned", typeof(bool)),
				new DataColumn("MaximumScale", typeof(short)),
				new DataColumn("MinimumScale", typeof(short)),
				new DataColumn("IsConcurrencyType", typeof(bool)),
				new DataColumn("IsLiteralSupported", typeof(bool)),
				new DataColumn("LiteralPrefix", typeof(string)),
				new DataColumn("LiteralSuffix", typeof(string)),
				new DataColumn("NativeDataType", typeof(string)),
			});

			var clrTypes = new HashSet<string>();
			foreach (var columnType in TypeMapper.Instance.GetColumnTypeMetadata())
			{
				// hard-code a few types to not appear in the schema table
				var mySqlDbType = columnType.MySqlDbType;
				if (mySqlDbType == MySqlDbType.Decimal || mySqlDbType == MySqlDbType.Newdate || mySqlDbType == MySqlDbType.Null || mySqlDbType == MySqlDbType.VarString)
					continue;
				if (mySqlDbType == MySqlDbType.Bool && columnType.IsUnsigned)
					continue;

				// set miscellaneous properties in code (rather than being data-driven)
				var clrType = columnType.DbTypeMapping.ClrType;
				var clrTypeName = clrType.ToString();
				var dataTypeName = mySqlDbType == MySqlDbType.Guid ? "GUID" :
					mySqlDbType == MySqlDbType.Bool ? "BOOL" : columnType.DataTypeName;
				var isAutoIncrementable = mySqlDbType == MySqlDbType.Byte || mySqlDbType == MySqlDbType.Int16 || mySqlDbType == MySqlDbType.Int24 || mySqlDbType == MySqlDbType.Int32 || mySqlDbType == MySqlDbType.Int64 ||
					mySqlDbType == MySqlDbType.UByte || mySqlDbType == MySqlDbType.UInt16 || mySqlDbType == MySqlDbType.UInt24 || mySqlDbType == MySqlDbType.UInt32 || mySqlDbType == MySqlDbType.UInt64;
				var isBestMatch = clrTypes.Add(clrTypeName);
				var isFixedLength = isAutoIncrementable ||
					mySqlDbType == MySqlDbType.Date || mySqlDbType == MySqlDbType.DateTime || mySqlDbType == MySqlDbType.Time || mySqlDbType == MySqlDbType.Timestamp ||
					mySqlDbType == MySqlDbType.Double || mySqlDbType == MySqlDbType.Float || mySqlDbType == MySqlDbType.Year || mySqlDbType == MySqlDbType.Guid || mySqlDbType == MySqlDbType.Bool;
				var isFixedPrecisionScale = isFixedLength ||
					mySqlDbType == MySqlDbType.Bit || mySqlDbType == MySqlDbType.NewDecimal;
				var isLong = mySqlDbType == MySqlDbType.Blob || mySqlDbType == MySqlDbType.MediumBlob || mySqlDbType == MySqlDbType.LongBlob;

				// map ColumnTypeMetadata to the row for this data type
				var createFormatParts = columnType.CreateFormat.Split(';');
				dataTable.Rows.Add(
					dataTypeName,
					(int) mySqlDbType,
					columnType.ColumnSize,
					createFormatParts[0],
					createFormatParts.Length == 1 ? null : createFormatParts[1],
					clrTypeName,
					isAutoIncrementable,
					isBestMatch,
					false,
					isFixedLength,
					isFixedPrecisionScale,
					isLong,
					true,
					clrType != typeof(byte[]),
					clrType == typeof(string),
					columnType.IsUnsigned,
					DBNull.Value,
					DBNull.Value,
					DBNull.Value,
					true,
					DBNull.Value,
					DBNull.Value,
					null
				);
			}
		}

		private void FillProcedures(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("SPECIFIC_NAME", typeof(string)),
				new DataColumn("ROUTINE_CATALOG", typeof(string)),
				new DataColumn("ROUTINE_SCHEMA", typeof(string)),
				new DataColumn("ROUTINE_NAME", typeof(string)),
				new DataColumn("ROUTINE_TYPE", typeof(string)),
				new DataColumn("DTD_IDENTIFIER", typeof(string)),
				new DataColumn("ROUTINE_BODY", typeof(string)),
				new DataColumn("ROUTINE_DEFINITION", typeof(string)),
				new DataColumn("EXTERNAL_NAME", typeof(string)),
				new DataColumn("EXTERNAL_LANGUAGE", typeof(string)),
				new DataColumn("PARAMETER_STYLE", typeof(string)),
				new DataColumn("IS_DETERMINISTIC", typeof(string)),
				new DataColumn("SQL_DATA_ACCESS", typeof(string)),
				new DataColumn("SQL_PATH", typeof(string)),
				new DataColumn("SECURITY_TYPE", typeof(string)),
				new DataColumn("CREATED", typeof(DateTime)),
				new DataColumn("LAST_ALTERED", typeof(DateTime)),
				new DataColumn("SQL_MODE", typeof(string)),
				new DataColumn("ROUTINE_COMMENT", typeof(string)),
				new DataColumn("DEFINER", typeof(string)),
			});

			Action close = null;
			if (m_connection.State != ConnectionState.Open)
			{
				m_connection.Open();
				close = m_connection.Close;
			}

			using (var command = m_connection.CreateCommand())
			{
				command.CommandText = "SELECT " + string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName)) + " FROM INFORMATION_SCHEMA.ROUTINES;";
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						var rowValues = new object[dataTable.Columns.Count];
						reader.GetValues(rowValues);
						dataTable.Rows.Add(rowValues);
					}
				}
			}

			close?.Invoke();
		}

		private void FillReservedWords(DataTable dataTable)
		{
			dataTable.Columns.Add(new DataColumn("ReservedWord", typeof(string)));

			dataTable.Rows.Add("ABSOLUTE");
			dataTable.Rows.Add("ACTION");
			dataTable.Rows.Add("ADA");
			dataTable.Rows.Add("ADD");
			dataTable.Rows.Add("ALL");
			dataTable.Rows.Add("ALLOCATE");
			dataTable.Rows.Add("ALTER");
			dataTable.Rows.Add("AND");
			dataTable.Rows.Add("ANY");
			dataTable.Rows.Add("ARE");
			dataTable.Rows.Add("AS");
			dataTable.Rows.Add("ASC");
			dataTable.Rows.Add("ASSERTION");
			dataTable.Rows.Add("AT");
			dataTable.Rows.Add("AUTHORIZATION");
			dataTable.Rows.Add("AVG");
			dataTable.Rows.Add("BEGIN");
			dataTable.Rows.Add("BETWEEN");
			dataTable.Rows.Add("BIT");
			dataTable.Rows.Add("BIT_LENGTH");
			dataTable.Rows.Add("BOTH");
			dataTable.Rows.Add("BY");
			dataTable.Rows.Add("CASCADE");
			dataTable.Rows.Add("CASCADED");
			dataTable.Rows.Add("CASE");
			dataTable.Rows.Add("CAST");
			dataTable.Rows.Add("CATALOG");
			dataTable.Rows.Add("CHAR");
			dataTable.Rows.Add("CHAR_LENGTH");
			dataTable.Rows.Add("CHARACTER");
			dataTable.Rows.Add("CHARACTER_LENGTH");
			dataTable.Rows.Add("CHECK");
			dataTable.Rows.Add("CLOSE");
			dataTable.Rows.Add("COALESCE");
			dataTable.Rows.Add("COLLATE");
			dataTable.Rows.Add("COLLATION");
			dataTable.Rows.Add("COLUMN");
			dataTable.Rows.Add("COMMIT");
			dataTable.Rows.Add("CONNECT");
			dataTable.Rows.Add("CONNECTION");
			dataTable.Rows.Add("CONSTRAINT");
			dataTable.Rows.Add("CONSTRAINTS");
			dataTable.Rows.Add("CONTINUE");
			dataTable.Rows.Add("CONVERT");
			dataTable.Rows.Add("CORRESPONDING");
			dataTable.Rows.Add("COUNT");
			dataTable.Rows.Add("CREATE");
			dataTable.Rows.Add("CROSS");
			dataTable.Rows.Add("CURRENT");
			dataTable.Rows.Add("CURRENT_DATE");
			dataTable.Rows.Add("CURRENT_TIME");
			dataTable.Rows.Add("CURRENT_TIMESTAMP");
			dataTable.Rows.Add("CURRENT_USER");
			dataTable.Rows.Add("CURSOR");
			dataTable.Rows.Add("DATE");
			dataTable.Rows.Add("DAY");
			dataTable.Rows.Add("DEALLOCATE");
			dataTable.Rows.Add("DEC");
			dataTable.Rows.Add("DECIMAL");
			dataTable.Rows.Add("DECLARE");
			dataTable.Rows.Add("DEFAULT");
			dataTable.Rows.Add("DEFERRABLE");
			dataTable.Rows.Add("DEFERRED");
			dataTable.Rows.Add("DELETE");
			dataTable.Rows.Add("DESC");
			dataTable.Rows.Add("DESCRIBE");
			dataTable.Rows.Add("DESCRIPTOR");
			dataTable.Rows.Add("DIAGNOSTICS");
			dataTable.Rows.Add("DISCONNECT");
			dataTable.Rows.Add("DISTINCT");
			dataTable.Rows.Add("DOMAIN");
			dataTable.Rows.Add("DOUBLE");
			dataTable.Rows.Add("DROP");
			dataTable.Rows.Add("ELSE");
			dataTable.Rows.Add("END");
			dataTable.Rows.Add("END-EXEC");
			dataTable.Rows.Add("ESCAPE");
			dataTable.Rows.Add("EXCEPT");
			dataTable.Rows.Add("EXCEPTION");
			dataTable.Rows.Add("EXEC");
			dataTable.Rows.Add("EXECUTE");
			dataTable.Rows.Add("EXISTS");
			dataTable.Rows.Add("EXTERNAL");
			dataTable.Rows.Add("EXTRACT");
			dataTable.Rows.Add("FALSE");
			dataTable.Rows.Add("FETCH");
			dataTable.Rows.Add("FIRST");
			dataTable.Rows.Add("FLOAT");
			dataTable.Rows.Add("FOR");
			dataTable.Rows.Add("FOREIGN");
			dataTable.Rows.Add("FORTRAN");
			dataTable.Rows.Add("FOUND");
			dataTable.Rows.Add("FROM");
			dataTable.Rows.Add("FULL");
			dataTable.Rows.Add("GET");
			dataTable.Rows.Add("GLOBAL");
			dataTable.Rows.Add("GO");
			dataTable.Rows.Add("GOTO");
			dataTable.Rows.Add("GRANT");
			dataTable.Rows.Add("GROUP");
			dataTable.Rows.Add("HAVING");
			dataTable.Rows.Add("HOUR");
			dataTable.Rows.Add("IDENTITY");
			dataTable.Rows.Add("IMMEDIATE");
			dataTable.Rows.Add("IN");
			dataTable.Rows.Add("INCLUDE");
			dataTable.Rows.Add("INDEX");
			dataTable.Rows.Add("INDICATOR");
			dataTable.Rows.Add("INITIALLY");
			dataTable.Rows.Add("INNER");
			dataTable.Rows.Add("INPUT");
			dataTable.Rows.Add("INSENSITIVE");
			dataTable.Rows.Add("INSERT");
			dataTable.Rows.Add("INT");
			dataTable.Rows.Add("INTEGER");
			dataTable.Rows.Add("INTERSECT");
			dataTable.Rows.Add("INTERVAL");
			dataTable.Rows.Add("INTO");
			dataTable.Rows.Add("IS");
			dataTable.Rows.Add("ISOLATION");
			dataTable.Rows.Add("JOIN");
			dataTable.Rows.Add("KEY");
			dataTable.Rows.Add("LANGUAGE");
			dataTable.Rows.Add("LAST");
			dataTable.Rows.Add("LEADING");
			dataTable.Rows.Add("LEFT");
			dataTable.Rows.Add("LEVEL");
			dataTable.Rows.Add("LIKE");
			dataTable.Rows.Add("LOCAL");
			dataTable.Rows.Add("LOWER");
			dataTable.Rows.Add("MATCH");
			dataTable.Rows.Add("MAX");
			dataTable.Rows.Add("MIN");
			dataTable.Rows.Add("MINUTE");
			dataTable.Rows.Add("MODULE");
			dataTable.Rows.Add("MONTH");
			dataTable.Rows.Add("NAMES");
			dataTable.Rows.Add("NATIONAL");
			dataTable.Rows.Add("NATURAL");
			dataTable.Rows.Add("NCHAR");
			dataTable.Rows.Add("NEXT");
			dataTable.Rows.Add("NO");
			dataTable.Rows.Add("NONE");
			dataTable.Rows.Add("NOT");
			dataTable.Rows.Add("NULL");
			dataTable.Rows.Add("NULLIF");
			dataTable.Rows.Add("NUMERIC");
			dataTable.Rows.Add("OCTET_LENGTH");
			dataTable.Rows.Add("OF");
			dataTable.Rows.Add("ON");
			dataTable.Rows.Add("ONLY");
			dataTable.Rows.Add("OPEN");
			dataTable.Rows.Add("OPTION");
			dataTable.Rows.Add("OR");
			dataTable.Rows.Add("ORDER");
			dataTable.Rows.Add("OUTER");
			dataTable.Rows.Add("OUTPUT");
			dataTable.Rows.Add("OVERLAPS");
			dataTable.Rows.Add("PAD");
			dataTable.Rows.Add("PARTIAL");
			dataTable.Rows.Add("PASCAL");
			dataTable.Rows.Add("POSITION");
			dataTable.Rows.Add("PRECISION");
			dataTable.Rows.Add("PREPARE");
			dataTable.Rows.Add("PRESERVE");
			dataTable.Rows.Add("PRIMARY");
			dataTable.Rows.Add("PRIOR");
			dataTable.Rows.Add("PRIVILEGES");
			dataTable.Rows.Add("PROCEDURE");
			dataTable.Rows.Add("PUBLIC");
			dataTable.Rows.Add("READ");
			dataTable.Rows.Add("REAL");
			dataTable.Rows.Add("REFERENCES");
			dataTable.Rows.Add("RELATIVE");
			dataTable.Rows.Add("RESTRICT");
			dataTable.Rows.Add("REVOKE");
			dataTable.Rows.Add("RIGHT");
			dataTable.Rows.Add("ROLLBACK");
			dataTable.Rows.Add("ROWS");
			dataTable.Rows.Add("SCHEMA");
			dataTable.Rows.Add("SCROLL");
			dataTable.Rows.Add("SECOND");
			dataTable.Rows.Add("SECTION");
			dataTable.Rows.Add("SELECT");
			dataTable.Rows.Add("SESSION");
			dataTable.Rows.Add("SESSION_USER");
			dataTable.Rows.Add("SET");
			dataTable.Rows.Add("SIZE");
			dataTable.Rows.Add("SMALLINT");
			dataTable.Rows.Add("SOME");
			dataTable.Rows.Add("SPACE");
			dataTable.Rows.Add("SQL");
			dataTable.Rows.Add("SQLCA");
			dataTable.Rows.Add("SQLCODE");
			dataTable.Rows.Add("SQLERROR");
			dataTable.Rows.Add("SQLSTATE");
			dataTable.Rows.Add("SQLWARNING");
			dataTable.Rows.Add("SUBSTRING");
			dataTable.Rows.Add("SUM");
			dataTable.Rows.Add("SYSTEM_USER");
			dataTable.Rows.Add("TABLE");
			dataTable.Rows.Add("TEMPORARY");
			dataTable.Rows.Add("THEN");
			dataTable.Rows.Add("TIME");
			dataTable.Rows.Add("TIMESTAMP");
			dataTable.Rows.Add("TIMEZONE_HOUR");
			dataTable.Rows.Add("TIMEZONE_MINUTE");
			dataTable.Rows.Add("TO");
			dataTable.Rows.Add("TRAILING");
			dataTable.Rows.Add("TRANSACTION");
			dataTable.Rows.Add("TRANSLATE");
			dataTable.Rows.Add("TRANSLATION");
			dataTable.Rows.Add("TRIM");
			dataTable.Rows.Add("TRUE");
			dataTable.Rows.Add("UNION");
			dataTable.Rows.Add("UNIQUE");
			dataTable.Rows.Add("UNKNOWN");
			dataTable.Rows.Add("UPDATE");
			dataTable.Rows.Add("UPPER");
			dataTable.Rows.Add("USAGE");
			dataTable.Rows.Add("USER");
			dataTable.Rows.Add("USING");
			dataTable.Rows.Add("VALUE");
			dataTable.Rows.Add("VALUES");
			dataTable.Rows.Add("VARCHAR");
			dataTable.Rows.Add("VARYING");
			dataTable.Rows.Add("VIEW");
			dataTable.Rows.Add("WHEN");
			dataTable.Rows.Add("WHENEVER");
			dataTable.Rows.Add("WHERE");
			dataTable.Rows.Add("WITH");
			dataTable.Rows.Add("WORK");
			dataTable.Rows.Add("WRITE");
			dataTable.Rows.Add("YEAR");
			dataTable.Rows.Add("ZONE");
			dataTable.Rows.Add("ACCESSIBLE");
			dataTable.Rows.Add("ANALYZE");
			dataTable.Rows.Add("ASENSITIVE");
			dataTable.Rows.Add("BEFORE");
			dataTable.Rows.Add("BIGINT");
			dataTable.Rows.Add("BINARY");
			dataTable.Rows.Add("BLOB");
			dataTable.Rows.Add("CALL");
			dataTable.Rows.Add("CHANGE");
			dataTable.Rows.Add("CONDITION");
			dataTable.Rows.Add("DATABASE");
			dataTable.Rows.Add("DATABASES");
			dataTable.Rows.Add("DAY_HOUR");
			dataTable.Rows.Add("DAY_MICROSECOND");
			dataTable.Rows.Add("DAY_MINUTE");
			dataTable.Rows.Add("DAY_SECOND");
			dataTable.Rows.Add("DELAYED");
			dataTable.Rows.Add("DETERMINISTIC");
			dataTable.Rows.Add("DISTINCTROW");
			dataTable.Rows.Add("DIV");
			dataTable.Rows.Add("DUAL");
			dataTable.Rows.Add("EACH");
			dataTable.Rows.Add("ELSEIF");
			dataTable.Rows.Add("ENCLOSED");
			dataTable.Rows.Add("ESCAPED");
			dataTable.Rows.Add("EXIT");
			dataTable.Rows.Add("EXPLAIN");
			dataTable.Rows.Add("FLOAT4");
			dataTable.Rows.Add("FLOAT8");
			dataTable.Rows.Add("FORCE");
			dataTable.Rows.Add("FULLTEXT");
			dataTable.Rows.Add("GENERAL");
			dataTable.Rows.Add("GET");
			dataTable.Rows.Add("HIGH_PRIORITY");
			dataTable.Rows.Add("HOUR_MICROSECOND");
			dataTable.Rows.Add("HOUR_MINUTE");
			dataTable.Rows.Add("HOUR_SECOND");
			dataTable.Rows.Add("IF");
			dataTable.Rows.Add("IGNORE");
			dataTable.Rows.Add("IGNORE_SERVER_IDS");
			dataTable.Rows.Add("INFILE");
			dataTable.Rows.Add("INOUT");
			dataTable.Rows.Add("INT1");
			dataTable.Rows.Add("INT2");
			dataTable.Rows.Add("INT3");
			dataTable.Rows.Add("INT4");
			dataTable.Rows.Add("INT8");
			dataTable.Rows.Add("IO_AFTER_GTIDS");
			dataTable.Rows.Add("IO_BEFORE_GTIDS");
			dataTable.Rows.Add("ITERATE");
			dataTable.Rows.Add("KEYS");
			dataTable.Rows.Add("KILL");
			dataTable.Rows.Add("LEAVE");
			dataTable.Rows.Add("LIMIT");
			dataTable.Rows.Add("LINEAR");
			dataTable.Rows.Add("LINES");
			dataTable.Rows.Add("LOAD");
			dataTable.Rows.Add("LOCALTIME");
			dataTable.Rows.Add("LOCALTIMESTAMP");
			dataTable.Rows.Add("LOCK");
			dataTable.Rows.Add("LONG");
			dataTable.Rows.Add("LONGBLOB");
			dataTable.Rows.Add("LONGTEXT");
			dataTable.Rows.Add("LOOP");
			dataTable.Rows.Add("LOW_PRIORITY");
			dataTable.Rows.Add("MASTER_BIND");
			dataTable.Rows.Add("MASTER_HEARTBEAT_PERIOD");
			dataTable.Rows.Add("MASTER_SSL_VERIFY_SERVER_CERT");
			dataTable.Rows.Add("MAXVALUE");
			dataTable.Rows.Add("MEDIUMBLOB");
			dataTable.Rows.Add("MEDIUMINT");
			dataTable.Rows.Add("MEDIUMTEXT");
			dataTable.Rows.Add("MIDDLEINT");
			dataTable.Rows.Add("MINUTE_MICROSECOND");
			dataTable.Rows.Add("MINUTE_SECOND");
			dataTable.Rows.Add("MOD");
			dataTable.Rows.Add("MODIFIES");
			dataTable.Rows.Add("NO_WRITE_TO_BINLOG");
			dataTable.Rows.Add("ONE_SHOT");
			dataTable.Rows.Add("OPTIMIZE");
			dataTable.Rows.Add("OPTIONALLY");
			dataTable.Rows.Add("OUT");
			dataTable.Rows.Add("OUTFILE");
			dataTable.Rows.Add("PARTITION");
			dataTable.Rows.Add("PURGE");
			dataTable.Rows.Add("RANGE");
			dataTable.Rows.Add("READ_ONLY");
			dataTable.Rows.Add("READS");
			dataTable.Rows.Add("READ_WRITE");
			dataTable.Rows.Add("REGEXP");
			dataTable.Rows.Add("RELEASE");
			dataTable.Rows.Add("RENAME");
			dataTable.Rows.Add("REPEAT");
			dataTable.Rows.Add("REPLACE");
			dataTable.Rows.Add("REQUIRE");
			dataTable.Rows.Add("RESIGNAL");
			dataTable.Rows.Add("RETURN");
			dataTable.Rows.Add("RLIKE");
			dataTable.Rows.Add("SCHEMAS");
			dataTable.Rows.Add("SECOND_MICROSECOND");
			dataTable.Rows.Add("SENSITIVE");
			dataTable.Rows.Add("SEPARATOR");
			dataTable.Rows.Add("SHOW");
			dataTable.Rows.Add("SIGNAL");
			dataTable.Rows.Add("SLOW");
			dataTable.Rows.Add("SPATIAL");
			dataTable.Rows.Add("SPECIFIC");
			dataTable.Rows.Add("SQL_AFTER_GTIDS");
			dataTable.Rows.Add("SQL_BEFORE_GTIDSSQL_BIG_RESULT");
			dataTable.Rows.Add("SQL_CALC_FOUND_ROWS");
			dataTable.Rows.Add("SQLEXCEPTION");
			dataTable.Rows.Add("SQL_SMALL_RESULT");
			dataTable.Rows.Add("SSL");
			dataTable.Rows.Add("STARTING");
			dataTable.Rows.Add("STRAIGHT_JOIN");
			dataTable.Rows.Add("TERMINATED");
			dataTable.Rows.Add("TINYBLOB");
			dataTable.Rows.Add("TINYINT");
			dataTable.Rows.Add("TINYTEXT");
			dataTable.Rows.Add("TRIGGER");
			dataTable.Rows.Add("UNDO");
			dataTable.Rows.Add("UNLOCK");
			dataTable.Rows.Add("UNSIGNED");
			dataTable.Rows.Add("USE");
			dataTable.Rows.Add("UTC_DATE");
			dataTable.Rows.Add("UTC_TIME");
			dataTable.Rows.Add("UTC_TIMESTAMP");
			dataTable.Rows.Add("VARBINARY");
			dataTable.Rows.Add("VARCHARACTER");
			dataTable.Rows.Add("WHILE");
			dataTable.Rows.Add("X509");
			dataTable.Rows.Add("XOR");
			dataTable.Rows.Add("YEAR_MONTH");
			dataTable.Rows.Add("ZEROFILL");
		}

		readonly MySqlConnection m_connection;
		readonly Dictionary<string, Action<DataTable>> m_schemaCollections;
	}
}
#endif
