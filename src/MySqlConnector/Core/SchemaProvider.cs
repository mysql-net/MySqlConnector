using System.Globalization;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core;

internal sealed partial class SchemaProvider
{
	public SchemaProvider(MySqlConnection connection)
	{
		m_connection = connection;
	}

	private void DoFillDataSourceInformation(DataTable dataTable)
	{
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

		static string GetVersion(Version v) =>
#if NET6_0_OR_GREATER
			string.Create(CultureInfo.InvariantCulture, stackalloc char[10], $"{v.Major:00}.{v.Minor:00}.{v.Build:0000}");
#else
			FormattableString.Invariant($"{v.Major:00}.{v.Minor:00}.{v.Build:0000}");
#endif
	}

	private static void DoFillDataTypes(DataTable dataTable)
	{
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
	}

	private static void DoFillReservedWords(DataTable dataTable)
	{
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

	private async Task FillDataTableAsync(IOBehavior ioBehavior, DataTable dataTable, string tableName, List<KeyValuePair<string, string>>? columns, CancellationToken cancellationToken)
	{
		Action? close = null;
		if (m_connection.State != ConnectionState.Open)
		{
			await m_connection.OpenAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			close = m_connection.Close;
		}

		// remove columns that the server doesn't support
		if (dataTable.TableName == "Columns")
		{
			using (var command = new MySqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema = 'information_schema' AND table_name = 'COLUMNS' AND column_name = 'GENERATION_EXPRESSION';", m_connection))
			{
				if (await command.ExecuteScalarAsync(ioBehavior, cancellationToken).ConfigureAwait(false) is null)
					dataTable.Columns.Remove("GENERATION_EXPRESSION");
			}

			using (var command = new MySqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema = 'information_schema' AND table_name = 'COLUMNS' AND column_name = 'SRS_ID';", m_connection))
			{
				if (await command.ExecuteScalarAsync(ioBehavior, cancellationToken).ConfigureAwait(false) is null)
					dataTable.Columns.Remove("SRS_ID");
			}
		}

		using (var command = m_connection.CreateCommand())
		{
#pragma warning disable CA2100
			command.CommandText = "SELECT " + string.Join(", ", dataTable.Columns.Cast<DataColumn>().Select(static x => x!.ColumnName)) + " FROM INFORMATION_SCHEMA." + tableName;
#pragma warning restore CA2100
			if (columns is { Count: > 0 })
			{
				command.CommandText += " WHERE " + string.Join(" AND ", columns.Select(x => $@"{x.Key} = @{x.Key}"));
				foreach (var column in columns)
					command.Parameters.AddWithValue("@" + column.Key, column.Value);
			}

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

	private readonly MySqlConnection m_connection;
}
