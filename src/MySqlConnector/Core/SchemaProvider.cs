using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal sealed class SchemaProvider
{
	public SchemaProvider(MySqlConnection connection)
	{
		m_connection = connection;
		m_schemaCollections = new(StringComparer.OrdinalIgnoreCase)
		{
			{ "DataSourceInformation", FillDataSourceInformation },
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

	public ValueTask<DataTable> GetSchemaAsync(IOBehavior ioBehavior, CancellationToken cancellationToken) => GetSchemaAsync(ioBehavior, "MetaDataCollections", cancellationToken);

	public async ValueTask<DataTable> GetSchemaAsync(IOBehavior ioBehavior, string collectionName, CancellationToken cancellationToken)
	{
		if (collectionName is null)
			throw new ArgumentNullException(nameof(collectionName));
		if (!m_schemaCollections.TryGetValue(collectionName, out var fillAction))
			throw new ArgumentException("Invalid collection name.", nameof(collectionName));

		var dataTable = new DataTable(collectionName);
		await fillAction(ioBehavior, dataTable, cancellationToken).ConfigureAwait(false);
		return dataTable;
	}

	private Task FillDataSourceInformation(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[] {
			new("CompositeIdentifierSeparatorPattern", typeof(string)),
			new("DataSourceProductName", typeof(string)),
			new("DataSourceProductVersion", typeof(string)),
			new("DataSourceProductVersionNormalized", typeof(string)),
			new("GroupByBehavior", typeof(GroupByBehavior)),
			new("IdentifierPattern", typeof(string)),
			new("IdentifierCase", typeof(IdentifierCase)),
			new("OrderByColumnsInSelect", typeof(bool)),
			new("ParameterMarkerFormat", typeof(string)),
			new("ParameterMarkerPattern", typeof(string)),
			new("ParameterNameMaxLength", typeof(int)),
			new("QuotedIdentifierPattern", typeof(string)),
			new("QuotedIdentifierCase", typeof(IdentifierCase)),
			new("ParameterNamePattern", typeof(string)),
			new("StatementSeparatorPattern", typeof(string)),
			new("StringLiteralPattern", typeof(string)),
			new("SupportedJoinOperators", typeof(SupportedJoinOperators)),
		});

		var row = dataTable.NewRow();
		row["CompositeIdentifierSeparatorPattern"] = @"\.";
		row["DataSourceProductName"] = "MySQL";
		row["DataSourceProductVersion"] = m_connection.ServerVersion;
		row["DataSourceProductVersionNormalized"] = GetVersion(m_connection.Session.ServerVersion.Version);
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

		return Utility.CompletedTask;

		static string GetVersion(Version v) => "{0:00}.{1:00}.{2:0000}".FormatInvariant(v.Major, v.Minor, v.Build);
	}

	private Task FillMetadataCollections(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[] {
			new("CollectionName", typeof(string)),
			new("NumberOfRestrictions", typeof(int)),
			new("NumberOfIdentifierParts", typeof(int)),
		});

		foreach (var collectionName in m_schemaCollections.Keys)
			dataTable.Rows.Add(collectionName, 0, 0);

		return Utility.CompletedTask;
	}

	private async Task FillCharacterSets(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("CHARACTER_SET_NAME", typeof(string)),
			new("DEFAULT_COLLATE_NAME", typeof(string)),
			new("DESCRIPTION", typeof(string)),
			new("MAXLEN", typeof(int)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "CHARACTER_SETS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillCollations(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("COLLATION_NAME", typeof(string)),
			new("CHARACTER_SET_NAME", typeof(string)),
			new("ID", typeof(int)),
			new("IS_DEFAULT", typeof(string)),
			new("IS_COMPILED", typeof(string)),
			new("SORTLEN", typeof(int)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "COLLATIONS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillCollationCharacterSetApplicability(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("COLLATION_NAME", typeof(string)),
			new("CHARACTER_SET_NAME", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "COLLATION_CHARACTER_SET_APPLICABILITY", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillColumns(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TABLE_CATALOG", typeof(string)),
			new("TABLE_SCHEMA", typeof(string)),
			new("TABLE_NAME", typeof(string)),
			new("COLUMN_NAME", typeof(string)),
			new("ORDINAL_POSITION", typeof(uint)),
			new("COLUMN_DEFAULT", typeof(string)),
			new("IS_NULLABLE", typeof(string)),
			new("DATA_TYPE", typeof(string)),
			new("CHARACTER_MAXIMUM_LENGTH", typeof(long)),
			new("NUMERIC_PRECISION", typeof(ulong)),
			new("NUMERIC_SCALE", typeof(ulong)),
			new("DATETIME_PRECISION", typeof(uint)),
			new("CHARACTER_SET_NAME", typeof(string)),
			new("COLLATION_NAME", typeof(string)),
			new("COLUMN_TYPE", typeof(string)),
			new("COLUMN_KEY", typeof(string)),
			new("EXTRA", typeof(string)),
			new("PRIVILEGES", typeof(string)),
			new("COLUMN_COMMENT", typeof(string)),
		});

		using (var command = new MySqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema = 'information_schema' AND table_name = 'COLUMNS' AND column_name = 'GENERATION_EXPRESSION';", m_connection))
		{
			if (await command.ExecuteScalarAsync(ioBehavior, cancellationToken).ConfigureAwait(false) is not null)
				dataTable.Columns.Add(new DataColumn("GENERATION_EXPRESSION", typeof(string)));
		}

		using (var command = new MySqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema = 'information_schema' AND table_name = 'COLUMNS' AND column_name = 'SRS_ID';", m_connection))
		{
			if (await command.ExecuteScalarAsync(ioBehavior, cancellationToken).ConfigureAwait(false) is not null)
				dataTable.Columns.Add(new DataColumn("SRS_ID", typeof(uint)));
		}

		await FillDataTableAsync(ioBehavior, dataTable, "COLUMNS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillDatabases(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("CATALOG_NAME", typeof(string)),
			new("SCHEMA_NAME", typeof(string)),
			new("DEFAULT_CHARACTER_SET_NAME", typeof(string)),
			new("DEFAULT_COLLATION_NAME", typeof(string)),
			new("SQL_PATH", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "SCHEMATA", cancellationToken).ConfigureAwait(false);
	}

	private Task FillDataTypes(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TypeName", typeof(string)),
			new("ProviderDbType", typeof(int)),
			new("ColumnSize", typeof(long)),
			new("CreateFormat", typeof(string)),
			new("CreateParameters", typeof(string)),
			new("DataType", typeof(string)),
			new("IsAutoIncrementable", typeof(bool)),
			new("IsBestMatch", typeof(bool)),
			new("IsCaseSensitive", typeof(bool)),
			new("IsFixedLength", typeof(bool)),
			new("IsFixedPrecisionScale", typeof(bool)),
			new("IsLong", typeof(bool)),
			new("IsNullable", typeof(bool)),
			new("IsSearchable", typeof(bool)),
			new("IsSearchableWithLike", typeof(bool)),
			new("IsUnsigned", typeof(bool)),
			new("MaximumScale", typeof(short)),
			new("MinimumScale", typeof(short)),
			new("IsConcurrencyType", typeof(bool)),
			new("IsLiteralSupported", typeof(bool)),
			new("LiteralPrefix", typeof(string)),
			new("LiteralSuffix", typeof(string)),
			new("NativeDataType", typeof(string)),
		});

		var clrTypes = new HashSet<string>();
		foreach (var columnType in TypeMapper.Instance.GetColumnTypeMetadata())
		{
			// hard-code a few types to not appear in the schema table
			if (columnType.MySqlDbType is MySqlDbType.Decimal or MySqlDbType.Newdate or MySqlDbType.Null or MySqlDbType.VarString)
				continue;
			if (columnType is { MySqlDbType: MySqlDbType.Bool, IsUnsigned: true })
				continue;

			// set miscellaneous properties in code (rather than being data-driven)
			var clrType = columnType.DbTypeMapping.ClrType;
			var clrTypeName = clrType.ToString();
			var mySqlDbType = columnType.MySqlDbType;
			var dataTypeName = mySqlDbType == MySqlDbType.Guid ? "GUID" :
				mySqlDbType == MySqlDbType.Bool ? "BOOL" : columnType.DataTypeName;
			var isAutoIncrementable = mySqlDbType is MySqlDbType.Byte or MySqlDbType.Int16 or MySqlDbType.Int24 or MySqlDbType.Int32 or MySqlDbType.Int64
				or MySqlDbType.UByte or MySqlDbType.UInt16 or MySqlDbType.UInt24 or MySqlDbType.UInt32 or MySqlDbType.UInt64;
			var isBestMatch = clrTypes.Add(clrTypeName);
			var isFixedLength = isAutoIncrementable ||
				mySqlDbType is MySqlDbType.Date or MySqlDbType.DateTime or MySqlDbType.Time or MySqlDbType.Timestamp
				or MySqlDbType.Double or MySqlDbType.Float or MySqlDbType.Year or MySqlDbType.Guid or MySqlDbType.Bool;
			var isFixedPrecisionScale = isFixedLength || mySqlDbType is MySqlDbType.Bit or MySqlDbType.NewDecimal;
			var isLong = mySqlDbType is MySqlDbType.Blob or MySqlDbType.MediumBlob or MySqlDbType.LongBlob;

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
				null);
		}

		return Utility.CompletedTask;
	}

	private async Task FillEngines(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("ENGINE", typeof(string)),
			new("SUPPORT", typeof(string)),
			new("COMMENT", typeof(string)),
			new("TRANSACTIONS", typeof(string)),
			new("XA", typeof(string)),
			new("SAVEPOINTS", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "ENGINES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillKeyColumnUsage(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("CONSTRAINT_CATALOG", typeof(string)),
			new("CONSTRAINT_SCHEMA", typeof(string)),
			new("CONSTRAINT_NAME", typeof(string)),
			new("TABLE_CATALOG", typeof(string)),
			new("TABLE_SCHEMA", typeof(string)),
			new("TABLE_NAME", typeof(string)),
			new("COLUMN_NAME", typeof(string)),
			new("ORDINAL_POSITION", typeof(int)),
			new("POSITION_IN_UNIQUE_CONSTRAINT", typeof(string)),
			new("REFERENCED_TABLE_SCHEMA", typeof(string)),
			new("REFERENCED_TABLE_NAME", typeof(string)),
			new("REFERENCED_COLUMN_NAME", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "KEY_COLUMN_USAGE", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillKeyWords(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("WORD", typeof(string)),
			new("RESERVED", typeof(int)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "KEYWORDS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillParameters(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("SPECIFIC_CATALOG", typeof(string)),
			new("SPECIFIC_SCHEMA", typeof(string)),
			new("SPECIFIC_NAME", typeof(string)),
			new("ORDINAL_POSITION", typeof(int)),
			new("PARAMETER_MODE", typeof(string)),
			new("PARAMETER_NAME", typeof(string)),
			new("DATA_TYPE", typeof(string)),
			new("CHARACTER_MAXIMUM_LENGTH", typeof(int)),
			new("CHARACTER_OCTET_LENGTH", typeof(int)),
			new("NUMERIC_PRECISION", typeof(int)),
			new("NUMERIC_SCALE", typeof(int)),
			new("DATETIME_PRECISION", typeof(int)),
			new("CHARACTER_SET_NAME", typeof(string)),
			new("COLLATION_NAME", typeof(string)),
			new("DTD_IDENTIFIER", typeof(string)),
			new("ROUTINE_TYPE", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "PARAMETERS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillPartitions(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TABLE_CATALOG", typeof(string)),
			new("TABLE_SCHEMA", typeof(string)),
			new("TABLE_NAME", typeof(string)),
			new("PARTITION_NAME", typeof(string)),
			new("SUBPARTITION_NAME", typeof(string)),
			new("PARTITION_ORDINAL_POSITION", typeof(int)),
			new("SUBPARTITION_ORDINAL_POSITION", typeof(int)),
			new("PARTITION_METHOD", typeof(string)),
			new("SUBPARTITION_METHOD", typeof(string)),
			new("PARTITION_EXPRESSION", typeof(string)),
			new("SUBPARTITION_EXPRESSION", typeof(string)),
			new("PARTITION_DESCRIPTION", typeof(string)),
			new("TABLE_ROWS", typeof(long)),
			new("AVG_ROW_LENGTH", typeof(long)),
			new("DATA_LENGTH", typeof(long)),
			new("MAX_DATA_LENGTH", typeof(long)),
			new("INDEX_LENGTH", typeof(long)),
			new("DATA_FREE", typeof(long)),
			new("CREATE_TIME", typeof(DateTime)),
			new("UPDATE_TIME", typeof(DateTime)),
			new("CHECK_TIME", typeof(DateTime)),
			new("CHECKSUM", typeof(long)),
			new("PARTITION_COMMENT", typeof(string)),
			new("NODEGROUP", typeof(string)),
			new("TABLESPACE_NAME", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "PARTITIONS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillPlugins(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("PLUGIN_NAME", typeof(string)),
			new("PLUGIN_VERSION", typeof(string)),
			new("PLUGIN_STATUS", typeof(string)),
			new("PLUGIN_TYPE", typeof(string)),
			new("PLUGIN_TYPE_VERSION", typeof(string)),
			new("PLUGIN_LIBRARY", typeof(string)),
			new("PLUGIN_LIBRARY_VERSION", typeof(string)),
			new("PLUGIN_AUTHOR", typeof(string)),
			new("PLUGIN_DESCRIPTION", typeof(string)),
			new("PLUGIN_LICENSE", typeof(string)),
			new("LOAD_OPTION", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "PLUGINS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillProcedures(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("SPECIFIC_NAME", typeof(string)),
			new("ROUTINE_CATALOG", typeof(string)),
			new("ROUTINE_SCHEMA", typeof(string)),
			new("ROUTINE_NAME", typeof(string)),
			new("ROUTINE_TYPE", typeof(string)),
			new("DTD_IDENTIFIER", typeof(string)),
			new("ROUTINE_BODY", typeof(string)),
			new("ROUTINE_DEFINITION", typeof(string)),
			new("EXTERNAL_NAME", typeof(string)),
			new("EXTERNAL_LANGUAGE", typeof(string)),
			new("PARAMETER_STYLE", typeof(string)),
			new("IS_DETERMINISTIC", typeof(string)),
			new("SQL_DATA_ACCESS", typeof(string)),
			new("SQL_PATH", typeof(string)),
			new("SECURITY_TYPE", typeof(string)),
			new("CREATED", typeof(DateTime)),
			new("LAST_ALTERED", typeof(DateTime)),
			new("SQL_MODE", typeof(string)),
			new("ROUTINE_COMMENT", typeof(string)),
			new("DEFINER", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "ROUTINES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillProcessList(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("ID", typeof(long)),
			new("USER", typeof(string)),
			new("HOST", typeof(string)),
			new("DB", typeof(string)),
			new("COMMAND", typeof(string)),
			new("TIME", typeof(int)),
			new("STATE", typeof(string)),
			new("INFO", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "PROCESSLIST", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillProfiling(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("QUERY_ID", typeof(int)),
			new("SEQ", typeof(int)),
			new("STATE", typeof(string)),
			new("DURATION", typeof(decimal)),
			new("CPU_USER", typeof(decimal)),
			new("CPU_SYSTEM", typeof(decimal)),
			new("CONTEXT_VOLUNTARY", typeof(int)),
			new("CONTEXT_INVOLUNTARY", typeof(int)),
			new("BLOCK_OPS_IN", typeof(int)),
			new("BLOCK_OPS_OUT", typeof(int)),
			new("MESSAGES_SENT", typeof(int)),
			new("MESSAGES_RECEIVED", typeof(int)),
			new("PAGE_FAULTS_MAJOR", typeof(int)),
			new("PAGE_FAULTS_MINOR", typeof(int)),
			new("SWAPS", typeof(int)),
			new("SOURCE_FUNCTION", typeof(string)),
			new("SOURCE_FILE", typeof(string)),
			new("SOURCE_LINE", typeof(int)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "PROFILING", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillReferentialConstraints(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("CONSTRAINT_CATALOG", typeof(string)),
			new("CONSTRAINT_SCHEMA", typeof(string)),
			new("CONSTRAINT_NAME", typeof(string)),
			new("UNIQUE_CONSTRAINT_CATALOG", typeof(string)),
			new("UNIQUE_CONSTRAINT_SCHEMA", typeof(string)),
			new("UNIQUE_CONSTRAINT_NAME", typeof(string)),
			new("MATCH_OPTION", typeof(string)),
			new("UPDATE_RULE", typeof(string)),
			new("DELETE_RULE", typeof(string)),
			new("TABLE_NAME", typeof(string)),
			new("REFERENCED_TABLE_NAME", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "REFERENTIAL_CONSTRAINTS", cancellationToken).ConfigureAwait(false);
	}

	private Task FillReservedWords(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.Add(new DataColumn("ReservedWord", typeof(string)));

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

		return Utility.CompletedTask;
	}

	private async Task FillResourceGroups(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("RESOURCE_GROUP_NAME", typeof(string)),
			new("RESOURCE_GROUP_TYPE", typeof(string)),
			new("RESOURCE_GROUP_ENABLED", typeof(int)),
			new("VCPU_IDS", typeof(string)),
			new("THREAD_PRIORITY", typeof(int)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "RESOURCE_GROUPS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillSchemaPrivileges(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("GRANTEE", typeof(string)),
			new("TABLE_CATALOG", typeof(string)),
			new("TABLE_SCHEMA", typeof(string)),
			new("PRIVILEGE_TYPE", typeof(string)),
			new("IS_GRANTABLE", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "SCHEMA_PRIVILEGES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillTables(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TABLE_CATALOG", typeof(string)),
			new("TABLE_SCHEMA", typeof(string)),
			new("TABLE_NAME", typeof(string)),
			new("TABLE_TYPE", typeof(string)),
			new("ENGINE", typeof(string)),
			new("VERSION", typeof(string)),
			new("ROW_FORMAT", typeof(string)),
			new("TABLE_ROWS", typeof(long)),
			new("AVG_ROW_LENGTH", typeof(long)),
			new("DATA_LENGTH", typeof(long)),
			new("MAX_DATA_LENGTH", typeof(long)),
			new("INDEX_LENGTH", typeof(long)),
			new("DATA_FREE", typeof(long)),
			new("AUTO_INCREMENT", typeof(long)),
			new("CREATE_TIME", typeof(DateTime)),
			new("UPDATE_TIME", typeof(DateTime)),
			new("CHECK_TIME", typeof(DateTime)),
			new("TABLE_COLLATION", typeof(string)),
			new("CHECKSUM", typeof(string)),
			new("CREATE_OPTIONS", typeof(string)),
			new("TABLE_COMMENT", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "TABLES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillTableConstraints(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("CONSTRAINT_CATALOG", typeof(string)),
			new("CONSTRAINT_SCHEMA", typeof(string)),
			new("CONSTRAINT_NAME", typeof(string)),
			new("TABLE_SCHEMA", typeof(string)),
			new("TABLE_NAME", typeof(string)),
			new("CONSTRAINT_TYPE", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "TABLE_CONSTRAINTS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillTablePrivileges(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("GRANTEE", typeof(string)),
			new("TABLE_CATALOG", typeof(string)),
			new("TABLE_SCHEMA", typeof(string)),
			new("TABLE_NAME", typeof(string)),
			new("PRIVILEGE_TYPE", typeof(string)),
			new("IS_GRANTABLE", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "TABLE_PRIVILEGES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillTableSpaces(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TABLESPACE_NAME", typeof(string)),
			new("ENGINE", typeof(string)),
			new("TABLESPACE_TYPE", typeof(string)),
			new("LOGFILE_GROUP_NAME", typeof(string)),
			new("EXTENT_SIZE", typeof(long)),
			new("AUTOEXTEND_SIZE", typeof(long)),
			new("MAXIMUM_SIZE", typeof(long)),
			new("NODEGROUP_ID", typeof(long)),
			new("TABLESPACE_COMMENT", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "TABLESPACES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillTriggers(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TRIGGER_CATALOG", typeof(string)),
			new("TRIGGER_SCHEMA", typeof(string)),
			new("TRIGGER_NAME", typeof(string)),
			new("EVENT_MANIPULATION", typeof(string)),
			new("EVENT_OBJECT_CATALOG", typeof(string)),
			new("EVENT_OBJECT_SCHEMA", typeof(string)),
			new("EVENT_OBJECT_TABLE", typeof(string)),
			new("ACTION_ORDER", typeof(long)),
			new("ACTION_CONDITION", typeof(string)),
			new("ACTION_STATEMENT", typeof(string)),
			new("ACTION_ORIENTATION", typeof(string)),
			new("ACTION_TIMING", typeof(string)),
			new("ACTION_REFERENCE_OLD_TABLE", typeof(string)),
			new("ACTION_REFERENCE_NEW_TABLE", typeof(string)),
			new("ACTION_REFERENCE_OLD_ROW", typeof(string)),
			new("ACTION_REFERENCE_NEW_ROW", typeof(string)),
			new("CREATED", typeof(DateTime)),
			new("SQL_MODE", typeof(string)),
			new("DEFINER", typeof(string)),
			new("CHARACTER_SET_CLIENT", typeof(string)),
			new("COLLATION_CONNECTION", typeof(string)),
			new("DATABASE_COLLATION", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "TRIGGERS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillUserPrivileges(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("GRANTEE", typeof(string)),
			new("TABLE_CATALOG", typeof(string)),
			new("PRIVILEGE_TYPE", typeof(string)),
			new("IS_GRANTABLE", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "USER_PRIVILEGES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillViews(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TABLE_CATALOG", typeof(string)),
			new("TABLE_SCHEMA", typeof(string)),
			new("TABLE_NAME", typeof(string)),
			new("VIEW_DEFINITION", typeof(string)),
			new("CHECK_OPTION", typeof(string)),
			new("IS_UPDATABLE", typeof(string)),
			new("DEFINER", typeof(string)),
			new("SECURITY_TYPE", typeof(string)),
			new("CHARACTER_SET_CLIENT", typeof(string)),
			new("COLLATION_CONNECTION", typeof(string)),
		});

		await FillDataTableAsync(ioBehavior, dataTable, "VIEWS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillDataTableAsync(IOBehavior ioBehavior, DataTable dataTable, string tableName, CancellationToken cancellationToken)
	{
		Action? close = null;
		if (m_connection.State != ConnectionState.Open)
		{
			await m_connection.OpenAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			close = m_connection.Close;
		}

		using (var command = m_connection.CreateCommand())
		{
#pragma warning disable CA2100
			command.CommandText = "SELECT " + string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(static x => x!.ColumnName)) + " FROM INFORMATION_SCHEMA." + tableName + ";";
#pragma warning restore CA2100
			using var reader = await command.ExecuteReaderAsync(default, ioBehavior, cancellationToken).ConfigureAwait(false);
			while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
			{
				var rowValues = new object[dataTable.Columns.Count];
				reader.GetValues(rowValues);
				dataTable.Rows.Add(rowValues);
			}
		}

		close?.Invoke();
	}

	readonly MySqlConnection m_connection;
	readonly Dictionary<string, Func<IOBehavior, DataTable, CancellationToken, Task>> m_schemaCollections;
}
