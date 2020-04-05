#if !NETSTANDARD1_3
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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
				{ "DataSourceInformation", FillDataSourceInformation},
				{ "MetaDataCollections", FillMetadataCollections },
				{ "CharacterSets", FillCharacterSets },
				{ "Collations", FillCollations },
				{ "CollationCharacterSetApplicability", FillCollationCharacterSetApplicability },
				{ "Columns", FillColumns },
				{ "Databases", FillDatabases },
				{ "DataTypes", FillDataTypes },
				{ "Engines", FillEngines },
				{ "KeyColumnUsage", FillKeyColumnUsage },
				{ "KeyWords", FillKeyWords },
				{ "Parameters", FillParameters },
				{ "Partitions", FillPartitions },
				{ "Plugins", FillPlugins },
				{ "Procedures", FillProcedures },
				{ "ProcessList", FillProcessList },
				{ "Profiling", FillProfiling },
				{ "ReferentialConstraints", FillReferentialConstraints },
				{ "ReservedWords", FillReservedWords },
				{ "ResourceGroups", FillResourceGroups },
				{ "SchemaPrivileges", FillSchemaPrivileges },
				{ "Tables", FillTables },
				{ "TableConstraints", FillTableConstraints },
				{ "TablePrivileges", FillTablePrivileges },
				{ "TableSpaces", FillTableSpaces },
				{ "Triggers", FillTriggers },
				{ "UserPrivileges", FillUserPrivileges },
				{ "Views", FillViews },
			};
		}

		public DataTable GetSchema() => GetSchema("MetaDataCollections");

		public DataTable GetSchema(string collectionName)
		{
			if (collectionName is null)
				throw new ArgumentNullException(nameof(collectionName));
			if (!m_schemaCollections.TryGetValue(collectionName, out var fillAction))
				throw new ArgumentException("Invalid collection name.", nameof(collectionName));

			var dataTable = new DataTable(collectionName);
			fillAction(dataTable);
			return dataTable;
		}

		private void FillDataSourceInformation(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new [] {
				new DataColumn("CompositeIdentifierSeparatorPattern", typeof(string)),
				new DataColumn("DataSourceProductName", typeof(string)),
				new DataColumn("DataSourceProductVersion", typeof(string)),
				new DataColumn("DataSourceProductVersionNormalized", typeof(string)),
				new DataColumn("GroupByBehavior", typeof(GroupByBehavior)),
				new DataColumn("IdentifierPattern", typeof(string)),
				new DataColumn("IdentifierCase", typeof(IdentifierCase)),
				new DataColumn("OrderByColumnsInSelect", typeof(bool)),
				new DataColumn("ParameterMarkerFormat", typeof(string)),
				new DataColumn("ParameterMarkerPattern", typeof(string)),
				new DataColumn("ParameterNameMaxLength", typeof(int)),
				new DataColumn("QuotedIdentifierPattern", typeof(string)),
				new DataColumn("QuotedIdentifierCase", typeof(IdentifierCase)),
				new DataColumn("ParameterNamePattern", typeof(string)),
				new DataColumn("StatementSeparatorPattern", typeof(string)),
				new DataColumn("StringLiteralPattern", typeof(string)),
				new DataColumn("SupportedJoinOperators", typeof(SupportedJoinOperators))
			});

			var row = dataTable.NewRow();
			row["CompositeIdentifierSeparatorPattern"] = @"\.";
			row["DataSourceProductName"] = "MysqlConnector";
			row["DataSourceProductVersion"] = m_connection.ServerVersion;
			row["DataSourceProductVersionNormalized"] = m_connection.Session.ServerVersion.Version.ToString();
			row["GroupByBehavior"] = GroupByBehavior.Unrelated;
			row["IdentifierPattern"] = @"(^\[\p{Lo}\p{Lu}\p{Ll}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Nd}@$#_]*$)|(^\[[^\]\0]|\]\]+\]$)|(^\""[^\""\0]|\""\""+\""$)";
			row["IdentifierCase"] = IdentifierCase.Insensitive;
			row["OrderByColumnsInSelect"] = false;
			row["ParameterMarkerFormat"] = @"{0}";
			row["ParameterMarkerPattern"] = @"(@[A-Za-z0-9_$#]*)";
			row["ParameterNameMaxLength"] = 128; // For function out parameters
			row["QuotedIdentifierPattern"] = @"(([^\`]|\`\`)*)";
			row["QuotedIdentifierCase"] = IdentifierCase.Sensitive;
			row["ParameterNamePattern"] = @"^[\p{Lo}\p{Lu}\p{Ll}\p{Lm}_@#][\p{Lo}\p{Lu}\p{Ll}\p{Lm}\p{Nd}\uff3f_@#\$]*(?=\s+|$)";
			row["StatementSeparatorPattern"] = ";";
			row["StringLiteralPattern"] = @"'(([^']|'')*)'";
			row["SupportedJoinOperators"] =
				SupportedJoinOperators.FullOuter |
				SupportedJoinOperators.Inner |
				SupportedJoinOperators.LeftOuter |
				SupportedJoinOperators.RightOuter;
			dataTable.Rows.Add(row);
		}


		private void FillMetadataCollections(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[] {
				new DataColumn("CollectionName", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("NumberOfRestrictions", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("NumberOfIdentifierParts", typeof(int)) // lgtm[cs/local-not-disposed]
			});

			foreach (var collectionName in m_schemaCollections.Keys)
				dataTable.Rows.Add(collectionName, 0, 0);
		}

		private void FillCharacterSets(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DEFAULT_COLLATE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DESCRIPTION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("MAXLEN", typeof(int)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "CHARACTER_SETS");
		}

		private void FillCollations(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("COLLATION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ID", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("IS_DEFAULT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("IS_COMPILED", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SORTLEN", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("PAD_ATTRIBUTE", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "COLLATIONS");
		}

		private void FillCollationCharacterSetApplicability(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("COLLATION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "COLLATION_CHARACTER_SET_APPLICABILITY");
		}

		private void FillColumns(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COLUMN_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ORDINAL_POSITION", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("COLUMN_DEFAULT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("IS_NULLABLE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DATA_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHARACTER_MAXIMUM_LENGTH", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHARACTER_OCTET_LENGTH", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("NUMERIC_PRECISION", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("NUMERIC_SCALE", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("DATETIME_PRECISION", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COLLATION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COLUMN_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COLUMN_KEY", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("EXTRA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PRIVILEGES", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COLUMN_COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("GENERATION_EXPRESSION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SRS_ID", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "COLUMNS");
		}

		private void FillDatabases(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("CATALOG_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SCHEMA_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DEFAULT_CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DEFAULT_COLLATION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SQL_PATH", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "SCHEMATA");
		}

		private void FillDataTypes(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("TypeName", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ProviderDbType", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("ColumnSize", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("CreateFormat", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CreateParameters", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DataType", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsAutoIncrementable", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsBestMatch", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsCaseSensitive", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsFixedLength", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsFixedPrecisionScale", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsLong", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsNullable", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsSearchable", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsSearchableWithLike", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsUnsigned", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("MaximumScale", typeof(short)), // lgtm[cs/local-not-disposed]
				new DataColumn("MinimumScale", typeof(short)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsConcurrencyType", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("IsLiteralSupported", typeof(bool)), // lgtm[cs/local-not-disposed]
				new DataColumn("LiteralPrefix", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("LiteralSuffix", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("NativeDataType", typeof(string)), // lgtm[cs/local-not-disposed]
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
					(int)mySqlDbType,
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

		private void FillEngines(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("ENGINE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SUPPORT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TRANSACTIONS", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("XA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SAVEPOINTS", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "ENGINES");
		}

		private void FillKeyColumnUsage(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("CONSTRAINT_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CONSTRAINT_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CONSTRAINT_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COLUMN_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ORDINAL_POSITION", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("POSITION_IN_UNIQUE_CONSTRAINT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("REFERENCED_TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("REFERENCED_TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("REFERENCED_COLUMN_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "KEY_COLUMN_USAGE");
		}

		private void FillKeyWords(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("WORD", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("RESERVED", typeof(int)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "KEYWORDS");
		}

		private void FillParameters(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("SPECIFIC_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SPECIFIC_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SPECIFIC_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ORDINAL_POSITION", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("PARAMETER_MODE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PARAMETER_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DATA_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHARACTER_MAXIMUM_LENGTH", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHARACTER_OCTET_LENGTH", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("NUMERIC_PRECISION", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("NUMERIC_SCALE", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("DATETIME_PRECISION", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COLLATION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DTD_IDENTIFIER", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ROUTINE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "PARAMETERS");
		}

		private void FillPartitions(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PARTITION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SUBPARTITION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PARTITION_ORDINAL_POSITION", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("SUBPARTITION_ORDINAL_POSITION", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("PARTITION_METHOD", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SUBPARTITION_METHOD", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PARTITION_EXPRESSION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SUBPARTITION_EXPRESSION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PARTITION_DESCRIPTION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_ROWS", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("AVG_ROW_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("DATA_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("MAX_DATA_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("INDEX_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("DATA_FREE", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("CREATE_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
				new DataColumn("UPDATE_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHECK_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHECKSUM", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("PARTITION_COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("NODEGROUP", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLESPACE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "PARTITIONS");
		}

		private void FillPlugins(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("PLUGIN_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PLUGIN_VERSION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PLUGIN_STATUS", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PLUGIN_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PLUGIN_TYPE_VERSION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PLUGIN_LIBRARY", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PLUGIN_LIBRARY_VERSION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PLUGIN_AUTHOR", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PLUGIN_DESCRIPTION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PLUGIN_LICENSE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("LOAD_OPTION", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "PLUGINS");
		}

		private void FillProcedures(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("SPECIFIC_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ROUTINE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ROUTINE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ROUTINE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ROUTINE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DTD_IDENTIFIER", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ROUTINE_BODY", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ROUTINE_DEFINITION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("EXTERNAL_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("EXTERNAL_LANGUAGE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PARAMETER_STYLE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("IS_DETERMINISTIC", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SQL_DATA_ACCESS", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SQL_PATH", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SECURITY_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CREATED", typeof(DateTime)), // lgtm[cs/local-not-disposed]
				new DataColumn("LAST_ALTERED", typeof(DateTime)), // lgtm[cs/local-not-disposed]
				new DataColumn("SQL_MODE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ROUTINE_COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DEFINER", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "ROUTINES");
		}

		private void FillProcessList(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("ID", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("USER", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("HOST", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DB", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COMMAND", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TIME", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("STATE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("INFO", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "PROCESSLIST");
		}

		private void FillProfiling(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("QUERY_ID", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("SEQ", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("STATE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DURATION", typeof(decimal)), // lgtm[cs/local-not-disposed]
				new DataColumn("CPU_USER", typeof(decimal)), // lgtm[cs/local-not-disposed]
				new DataColumn("CPU_SYSTEM", typeof(decimal)), // lgtm[cs/local-not-disposed]
				new DataColumn("CONTEXT_VOLUNTARY", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("CONTEXT_INVOLUNTARY", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("BLOCK_OPS_IN", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("BLOCK_OPS_OUT", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("MESSAGES_SENT", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("MESSAGES_RECEIVED", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("PAGE_FAULTS_MAJOR", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("PAGE_FAULTS_MINOR", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("SWAPS", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("SOURCE_FUNCTION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SOURCE_FILE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SOURCE_LINE", typeof(int)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "PROFILING");
		}

		private void FillReferentialConstraints(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("CONSTRAINT_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CONSTRAINT_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CONSTRAINT_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("UNIQUE_CONSTRAINT_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("UNIQUE_CONSTRAINT_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("UNIQUE_CONSTRAINT_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("MATCH_OPTION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("UPDATE_RULE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DELETE_RULE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("REFERENCED_TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "REFERENTIAL_CONSTRAINTS");
		}


		private void FillReservedWords(DataTable dataTable)
		{
			dataTable.Columns.Add(new DataColumn("ReservedWord", typeof(string))); // lgtm[cs/local-not-disposed]

			// Note:
			// For MySQL 8.0, the INFORMATION_SCHEMA.KEYWORDS table could be used to load the list at runtime,
			// unfortunately this bug https://bugs.mysql.com/bug.php?id=90160 makes it impratical to do it
			// (the bug is marked as fixed in MySQL 8.0.13, not published yet at the time of writing this note).
			//
			// Note:
			// Once the previously mentioned bug will be fixed, for versions >= 8.0.13 reserved words could be
			// loaded at runtime form INFORMATION_SCHEMA.KEYWORDS, and for other versions the hard coded list
			// could be used (notice the list could change with the release, adopting the 8.0.12 list is a
			// suboptimal one-size-fits-it-all solution.
			// To get the current MySQL version at runtime one could query SELECT VERSION(); which returns a
			// version followed by a suffix. The problem is that MariaDB 10.0 is only compatible with MySQL 5.6
			// (but has a higher version number)

			// select word from information_schema.keywords where reserved = 1; on MySQL Server 8.0.18
			var reservedWords = new[]
			{
				"ACCESSIBLE",
				"ADD",
				"ALL",
				"ALTER",
				"ANALYZE",
				"AND",
				"AS",
				"ASC",
				"ASENSITIVE",
				"BEFORE",
				"BETWEEN",
				"BIGINT",
				"BINARY",
				"BLOB",
				"BOTH",
				"BY",
				"CALL",
				"CASCADE",
				"CASE",
				"CHANGE",
				"CHAR",
				"CHARACTER",
				"CHECK",
				"COLLATE",
				"COLUMN",
				"CONDITION",
				"CONSTRAINT",
				"CONTINUE",
				"CONVERT",
				"CREATE",
				"CROSS",
				"CUBE",
				"CUME_DIST",
				"CURRENT_DATE",
				"CURRENT_TIME",
				"CURRENT_TIMESTAMP",
				"CURRENT_USER",
				"CURSOR",
				"DATABASE",
				"DATABASES",
				"DAY_HOUR",
				"DAY_MICROSECOND",
				"DAY_MINUTE",
				"DAY_SECOND",
				"DEC",
				"DECIMAL",
				"DECLARE",
				"DEFAULT",
				"DELAYED",
				"DELETE",
				"DENSE_RANK",
				"DESC",
				"DESCRIBE",
				"DETERMINISTIC",
				"DISTINCT",
				"DISTINCTROW",
				"DIV",
				"DOUBLE",
				"DROP",
				"DUAL",
				"EACH",
				"ELSE",
				"ELSEIF",
				"EMPTY",
				"ENCLOSED",
				"ESCAPED",
				"EXCEPT",
				"EXISTS",
				"EXIT",
				"EXPLAIN",
				"FALSE",
				"FETCH",
				"FIRST_VALUE",
				"FLOAT",
				"FLOAT4",
				"FLOAT8",
				"FOR",
				"FORCE",
				"FOREIGN",
				"FROM",
				"FULLTEXT",
				"FUNCTION",
				"GENERATED",
				"GET",
				"GRANT",
				"GROUP",
				"GROUPING",
				"GROUPS",
				"HAVING",
				"HIGH_PRIORITY",
				"HOUR_MICROSECOND",
				"HOUR_MINUTE",
				"HOUR_SECOND",
				"IF",
				"IGNORE",
				"IN",
				"INDEX",
				"INFILE",
				"INNER",
				"INOUT",
				"INSENSITIVE",
				"INSERT",
				"INT",
				"INT1",
				"INT2",
				"INT3",
				"INT4",
				"INT8",
				"INTEGER",
				"INTERVAL",
				"INTO",
				"IO_AFTER_GTIDS",
				"IO_BEFORE_GTIDS",
				"IS",
				"ITERATE",
				"JOIN",
				"JSON_TABLE",
				"KEY",
				"KEYS",
				"KILL",
				"LAG",
				"LAST_VALUE",
				"LATERAL",
				"LEAD",
				"LEADING",
				"LEAVE",
				"LEFT",
				"LIKE",
				"LIMIT",
				"LINEAR",
				"LINES",
				"LOAD",
				"LOCALTIME",
				"LOCALTIMESTAMP",
				"LOCK",
				"LONG",
				"LONGBLOB",
				"LONGTEXT",
				"LOOP",
				"LOW_PRIORITY",
				"MASTER_BIND",
				"MASTER_SSL_VERIFY_SERVER_CERT",
				"MATCH",
				"MAXVALUE",
				"MEDIUMBLOB",
				"MEDIUMINT",
				"MEDIUMTEXT",
				"MEMBER",
				"MIDDLEINT",
				"MINUTE_MICROSECOND",
				"MINUTE_SECOND",
				"MOD",
				"MODIFIES",
				"NATURAL",
				"NOT",
				"NO_WRITE_TO_BINLOG",
				"NTH_VALUE",
				"NTILE",
				"NULL",
				"NUMERIC",
				"OF",
				"ON",
				"OPTIMIZE",
				"OPTIMIZER_COSTS",
				"OPTION",
				"OPTIONALLY",
				"OR",
				"ORDER",
				"OUT",
				"OUTER",
				"OUTFILE",
				"OVER",
				"PARTITION",
				"PERCENT_RANK",
				"PRECISION",
				"PRIMARY",
				"PROCEDURE",
				"PURGE",
				"RANGE",
				"RANK",
				"READ",
				"READS",
				"READ_WRITE",
				"REAL",
				"RECURSIVE",
				"REFERENCES",
				"REGEXP",
				"RELEASE",
				"RENAME",
				"REPEAT",
				"REPLACE",
				"REQUIRE",
				"RESIGNAL",
				"RESTRICT",
				"RETURN",
				"REVOKE",
				"RIGHT",
				"RLIKE",
				"ROW",
				"ROWS",
				"ROW_NUMBER",
				"SCHEMA",
				"SCHEMAS",
				"SECOND_MICROSECOND",
				"SELECT",
				"SENSITIVE",
				"SEPARATOR",
				"SET",
				"SHOW",
				"SIGNAL",
				"SMALLINT",
				"SPATIAL",
				"SPECIFIC",
				"SQL",
				"SQLEXCEPTION",
				"SQLSTATE",
				"SQLWARNING",
				"SQL_BIG_RESULT",
				"SQL_CALC_FOUND_ROWS",
				"SQL_SMALL_RESULT",
				"SSL",
				"STARTING",
				"STORED",
				"STRAIGHT_JOIN",
				"SYSTEM",
				"TABLE",
				"TERMINATED",
				"THEN",
				"TINYBLOB",
				"TINYINT",
				"TINYTEXT",
				"TO",
				"TRAILING",
				"TRIGGER",
				"TRUE",
				"UNDO",
				"UNION",
				"UNIQUE",
				"UNLOCK",
				"UNSIGNED",
				"UPDATE",
				"USAGE",
				"USE",
				"USING",
				"UTC_DATE",
				"UTC_TIME",
				"UTC_TIMESTAMP",
				"VALUES",
				"VARBINARY",
				"VARCHAR",
				"VARCHARACTER",
				"VARYING",
				"VIRTUAL",
				"WHEN",
				"WHERE",
				"WHILE",
				"WINDOW",
				"WITH",
				"WRITE",
				"XOR",
				"YEAR_MONTH",
				"ZEROFILL",
			};
			foreach (string word in reservedWords)
				dataTable.Rows.Add(word);
		}

		private void FillResourceGroups(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("RESOURCE_GROUP_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("RESOURCE_GROUP_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("RESOURCE_GROUP_ENABLED", typeof(int)), // lgtm[cs/local-not-disposed]
				new DataColumn("VCPU_IDS", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("THREAD_PRIORITY", typeof(int)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "RESOURCE_GROUPS");
		}

		private void FillSchemaPrivileges(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("GRANTEE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PRIVILEGE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("IS_GRANTABLE", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "SCHEMA_PRIVILEGES");
		}

		private void FillTables(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ENGINE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("VERSION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ROW_FORMAT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_ROWS", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("AVG_ROW_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("DATA_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("MAX_DATA_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("INDEX_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("DATA_FREE", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("AUTO_INCREMENT", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("CREATE_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
				new DataColumn("UPDATE_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHECK_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_COLLATION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHECKSUM", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CREATE_OPTIONS", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "TABLES");
		}

		private void FillTableConstraints(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("CONSTRAINT_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CONSTRAINT_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CONSTRAINT_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CONSTRAINT_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "TABLE_CONSTRAINTS");
		}

		private void FillTablePrivileges(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("GRANTEE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PRIVILEGE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("IS_GRANTABLE", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "TABLE_PRIVILEGES");
		}

		private void FillTableSpaces(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("TABLESPACE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ENGINE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLESPACE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("LOGFILE_GROUP_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("EXTENT_SIZE", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("AUTOEXTEND_SIZE", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("MAXIMUM_SIZE", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("NODEGROUP_ID", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLESPACE_COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "TABLESPACES");
		}

		private void FillTriggers(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("TRIGGER_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TRIGGER_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TRIGGER_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("EVENT_MANIPULATION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("EVENT_OBJECT_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("EVENT_OBJECT_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("EVENT_OBJECT_TABLE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ACTION_ORDER", typeof(long)), // lgtm[cs/local-not-disposed]
				new DataColumn("ACTION_CONDITION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ACTION_STATEMENT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ACTION_ORIENTATION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ACTION_TIMING", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ACTION_REFERENCE_OLD_TABLE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ACTION_REFERENCE_NEW_TABLE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ACTION_REFERENCE_OLD_ROW", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("ACTION_REFERENCE_NEW_ROW", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CREATED", typeof(DateTime)), // lgtm[cs/local-not-disposed]
				new DataColumn("SQL_MODE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DEFINER", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHARACTER_SET_CLIENT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COLLATION_CONNECTION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DATABASE_COLLATION", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "TRIGGERS");
		}

		private void FillUserPrivileges(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("GRANTEE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("PRIVILEGE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("IS_GRANTABLE", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "USER_PRIVILEGES");
		}

		private void FillViews(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[]
			{
				new DataColumn("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("VIEW_DEFINITION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHECK_OPTION", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("IS_UPDATABLE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("DEFINER", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("SECURITY_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("CHARACTER_SET_CLIENT", typeof(string)), // lgtm[cs/local-not-disposed]
				new DataColumn("COLLATION_CONNECTION", typeof(string)), // lgtm[cs/local-not-disposed]
			});

			FillDataTable(dataTable, "VIEWS");
		}

		private void FillDataTable(DataTable dataTable, string tableName)
		{
			Action? close = null;
			if (m_connection.State != ConnectionState.Open)
			{
				m_connection.Open();
				close = m_connection.Close;
			}

			using (var command = m_connection.CreateCommand())
			{
#pragma warning disable CA2100
				command.CommandText = "SELECT " + string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(x => x.ColumnName)) + " FROM INFORMATION_SCHEMA." + tableName + ";";
#pragma warning restore CA2100
				using var reader = command.ExecuteReader();
				while (reader.Read())
				{
					var rowValues = new object[dataTable.Columns.Count];
					reader.GetValues(rowValues);
					dataTable.Rows.Add(rowValues);
				}
			}

			close?.Invoke();
		}

		readonly MySqlConnection m_connection;
		readonly Dictionary<string, Action<DataTable>> m_schemaCollections;
	}
}
#endif
