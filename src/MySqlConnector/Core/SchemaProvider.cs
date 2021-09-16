using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
			new("SupportedJoinOperators", typeof(SupportedJoinOperators))
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
			new("CollectionName", typeof(string)), // lgtm[cs/local-not-disposed]
			new("NumberOfRestrictions", typeof(int)), // lgtm[cs/local-not-disposed]
			new("NumberOfIdentifierParts", typeof(int)) // lgtm[cs/local-not-disposed]
		});

		foreach (var collectionName in m_schemaCollections.Keys)
			dataTable.Rows.Add(collectionName, 0, 0);

		return Utility.CompletedTask;
	}

	private async Task FillCharacterSets(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DEFAULT_COLLATE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DESCRIPTION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("MAXLEN", typeof(int)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "CHARACTER_SETS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillCollations(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("COLLATION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ID", typeof(int)), // lgtm[cs/local-not-disposed]
			new("IS_DEFAULT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("IS_COMPILED", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SORTLEN", typeof(int)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "COLLATIONS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillCollationCharacterSetApplicability(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("COLLATION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "COLLATION_CHARACTER_SET_APPLICABILITY", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillColumns(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COLUMN_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ORDINAL_POSITION", typeof(uint)), // lgtm[cs/local-not-disposed]
			new("COLUMN_DEFAULT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("IS_NULLABLE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DATA_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CHARACTER_MAXIMUM_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
			new("NUMERIC_PRECISION", typeof(ulong)), // lgtm[cs/local-not-disposed]
			new("NUMERIC_SCALE", typeof(ulong)), // lgtm[cs/local-not-disposed]
			new("DATETIME_PRECISION", typeof(uint)), // lgtm[cs/local-not-disposed]
			new("CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COLLATION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COLUMN_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COLUMN_KEY", typeof(string)), // lgtm[cs/local-not-disposed]
			new("EXTRA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PRIVILEGES", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COLUMN_COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		using (var command = new MySqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema = 'information_schema' AND table_name = 'COLUMNS' AND column_name = 'GENERATION_EXPRESSION';", m_connection))
		{
			if (await command.ExecuteScalarAsync(ioBehavior, cancellationToken).ConfigureAwait(false) is not null)
				dataTable.Columns.Add(new DataColumn("GENERATION_EXPRESSION", typeof(string))); // lgtm[cs/local-not-disposed]
		}

		using (var command = new MySqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema = 'information_schema' AND table_name = 'COLUMNS' AND column_name = 'SRS_ID';", m_connection))
		{
			if (await command.ExecuteScalarAsync(ioBehavior, cancellationToken).ConfigureAwait(false) is not null)
				dataTable.Columns.Add(new DataColumn("SRS_ID", typeof(uint))); // lgtm[cs/local-not-disposed]
		}

		await FillDataTableAsync(ioBehavior, dataTable, "COLUMNS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillDatabases(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("CATALOG_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SCHEMA_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DEFAULT_CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DEFAULT_COLLATION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SQL_PATH", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "SCHEMATA", cancellationToken).ConfigureAwait(false);
	}

	private Task FillDataTypes(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TypeName", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ProviderDbType", typeof(int)), // lgtm[cs/local-not-disposed]
			new("ColumnSize", typeof(long)), // lgtm[cs/local-not-disposed]
			new("CreateFormat", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CreateParameters", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DataType", typeof(string)), // lgtm[cs/local-not-disposed]
			new("IsAutoIncrementable", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("IsBestMatch", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("IsCaseSensitive", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("IsFixedLength", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("IsFixedPrecisionScale", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("IsLong", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("IsNullable", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("IsSearchable", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("IsSearchableWithLike", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("IsUnsigned", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("MaximumScale", typeof(short)), // lgtm[cs/local-not-disposed]
			new("MinimumScale", typeof(short)), // lgtm[cs/local-not-disposed]
			new("IsConcurrencyType", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("IsLiteralSupported", typeof(bool)), // lgtm[cs/local-not-disposed]
			new("LiteralPrefix", typeof(string)), // lgtm[cs/local-not-disposed]
			new("LiteralSuffix", typeof(string)), // lgtm[cs/local-not-disposed]
			new("NativeDataType", typeof(string)), // lgtm[cs/local-not-disposed]
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
				null
			);
		}

		return Utility.CompletedTask;
	}

	private async Task FillEngines(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("ENGINE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SUPPORT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TRANSACTIONS", typeof(string)), // lgtm[cs/local-not-disposed]
			new("XA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SAVEPOINTS", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "ENGINES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillKeyColumnUsage(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("CONSTRAINT_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CONSTRAINT_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CONSTRAINT_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COLUMN_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ORDINAL_POSITION", typeof(int)), // lgtm[cs/local-not-disposed]
			new("POSITION_IN_UNIQUE_CONSTRAINT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("REFERENCED_TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("REFERENCED_TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("REFERENCED_COLUMN_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "KEY_COLUMN_USAGE", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillKeyWords(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("WORD", typeof(string)), // lgtm[cs/local-not-disposed]
			new("RESERVED", typeof(int)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "KEYWORDS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillParameters(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("SPECIFIC_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SPECIFIC_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SPECIFIC_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ORDINAL_POSITION", typeof(int)), // lgtm[cs/local-not-disposed]
			new("PARAMETER_MODE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PARAMETER_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DATA_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CHARACTER_MAXIMUM_LENGTH", typeof(int)), // lgtm[cs/local-not-disposed]
			new("CHARACTER_OCTET_LENGTH", typeof(int)), // lgtm[cs/local-not-disposed]
			new("NUMERIC_PRECISION", typeof(int)), // lgtm[cs/local-not-disposed]
			new("NUMERIC_SCALE", typeof(int)), // lgtm[cs/local-not-disposed]
			new("DATETIME_PRECISION", typeof(int)), // lgtm[cs/local-not-disposed]
			new("CHARACTER_SET_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COLLATION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DTD_IDENTIFIER", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ROUTINE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "PARAMETERS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillPartitions(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PARTITION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SUBPARTITION_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PARTITION_ORDINAL_POSITION", typeof(int)), // lgtm[cs/local-not-disposed]
			new("SUBPARTITION_ORDINAL_POSITION", typeof(int)), // lgtm[cs/local-not-disposed]
			new("PARTITION_METHOD", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SUBPARTITION_METHOD", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PARTITION_EXPRESSION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SUBPARTITION_EXPRESSION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PARTITION_DESCRIPTION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_ROWS", typeof(long)), // lgtm[cs/local-not-disposed]
			new("AVG_ROW_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
			new("DATA_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
			new("MAX_DATA_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
			new("INDEX_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
			new("DATA_FREE", typeof(long)), // lgtm[cs/local-not-disposed]
			new("CREATE_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
			new("UPDATE_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
			new("CHECK_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
			new("CHECKSUM", typeof(long)), // lgtm[cs/local-not-disposed]
			new("PARTITION_COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("NODEGROUP", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLESPACE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "PARTITIONS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillPlugins(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("PLUGIN_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PLUGIN_VERSION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PLUGIN_STATUS", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PLUGIN_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PLUGIN_TYPE_VERSION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PLUGIN_LIBRARY", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PLUGIN_LIBRARY_VERSION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PLUGIN_AUTHOR", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PLUGIN_DESCRIPTION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PLUGIN_LICENSE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("LOAD_OPTION", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "PLUGINS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillProcedures(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("SPECIFIC_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ROUTINE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ROUTINE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ROUTINE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ROUTINE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DTD_IDENTIFIER", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ROUTINE_BODY", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ROUTINE_DEFINITION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("EXTERNAL_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("EXTERNAL_LANGUAGE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PARAMETER_STYLE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("IS_DETERMINISTIC", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SQL_DATA_ACCESS", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SQL_PATH", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SECURITY_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CREATED", typeof(DateTime)), // lgtm[cs/local-not-disposed]
			new("LAST_ALTERED", typeof(DateTime)), // lgtm[cs/local-not-disposed]
			new("SQL_MODE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ROUTINE_COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DEFINER", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "ROUTINES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillProcessList(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("ID", typeof(long)), // lgtm[cs/local-not-disposed]
			new("USER", typeof(string)), // lgtm[cs/local-not-disposed]
			new("HOST", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DB", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COMMAND", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TIME", typeof(int)), // lgtm[cs/local-not-disposed]
			new("STATE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("INFO", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "PROCESSLIST", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillProfiling(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("QUERY_ID", typeof(int)), // lgtm[cs/local-not-disposed]
			new("SEQ", typeof(int)), // lgtm[cs/local-not-disposed]
			new("STATE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DURATION", typeof(decimal)), // lgtm[cs/local-not-disposed]
			new("CPU_USER", typeof(decimal)), // lgtm[cs/local-not-disposed]
			new("CPU_SYSTEM", typeof(decimal)), // lgtm[cs/local-not-disposed]
			new("CONTEXT_VOLUNTARY", typeof(int)), // lgtm[cs/local-not-disposed]
			new("CONTEXT_INVOLUNTARY", typeof(int)), // lgtm[cs/local-not-disposed]
			new("BLOCK_OPS_IN", typeof(int)), // lgtm[cs/local-not-disposed]
			new("BLOCK_OPS_OUT", typeof(int)), // lgtm[cs/local-not-disposed]
			new("MESSAGES_SENT", typeof(int)), // lgtm[cs/local-not-disposed]
			new("MESSAGES_RECEIVED", typeof(int)), // lgtm[cs/local-not-disposed]
			new("PAGE_FAULTS_MAJOR", typeof(int)), // lgtm[cs/local-not-disposed]
			new("PAGE_FAULTS_MINOR", typeof(int)), // lgtm[cs/local-not-disposed]
			new("SWAPS", typeof(int)), // lgtm[cs/local-not-disposed]
			new("SOURCE_FUNCTION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SOURCE_FILE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SOURCE_LINE", typeof(int)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "PROFILING", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillReferentialConstraints(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("CONSTRAINT_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CONSTRAINT_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CONSTRAINT_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("UNIQUE_CONSTRAINT_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("UNIQUE_CONSTRAINT_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("UNIQUE_CONSTRAINT_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("MATCH_OPTION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("UPDATE_RULE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DELETE_RULE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("REFERENCED_TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "REFERENTIAL_CONSTRAINTS", cancellationToken).ConfigureAwait(false);
	}


	private Task FillReservedWords(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
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

		return Utility.CompletedTask;
	}

	private async Task FillResourceGroups(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("RESOURCE_GROUP_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("RESOURCE_GROUP_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("RESOURCE_GROUP_ENABLED", typeof(int)), // lgtm[cs/local-not-disposed]
			new("VCPU_IDS", typeof(string)), // lgtm[cs/local-not-disposed]
			new("THREAD_PRIORITY", typeof(int)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "RESOURCE_GROUPS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillSchemaPrivileges(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("GRANTEE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PRIVILEGE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("IS_GRANTABLE", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "SCHEMA_PRIVILEGES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillTables(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ENGINE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("VERSION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ROW_FORMAT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_ROWS", typeof(long)), // lgtm[cs/local-not-disposed]
			new("AVG_ROW_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
			new("DATA_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
			new("MAX_DATA_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
			new("INDEX_LENGTH", typeof(long)), // lgtm[cs/local-not-disposed]
			new("DATA_FREE", typeof(long)), // lgtm[cs/local-not-disposed]
			new("AUTO_INCREMENT", typeof(long)), // lgtm[cs/local-not-disposed]
			new("CREATE_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
			new("UPDATE_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
			new("CHECK_TIME", typeof(DateTime)), // lgtm[cs/local-not-disposed]
			new("TABLE_COLLATION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CHECKSUM", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CREATE_OPTIONS", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "TABLES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillTableConstraints(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("CONSTRAINT_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CONSTRAINT_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CONSTRAINT_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CONSTRAINT_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "TABLE_CONSTRAINTS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillTablePrivileges(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("GRANTEE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PRIVILEGE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("IS_GRANTABLE", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "TABLE_PRIVILEGES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillTableSpaces(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TABLESPACE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ENGINE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLESPACE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("LOGFILE_GROUP_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("EXTENT_SIZE", typeof(long)), // lgtm[cs/local-not-disposed]
			new("AUTOEXTEND_SIZE", typeof(long)), // lgtm[cs/local-not-disposed]
			new("MAXIMUM_SIZE", typeof(long)), // lgtm[cs/local-not-disposed]
			new("NODEGROUP_ID", typeof(long)), // lgtm[cs/local-not-disposed]
			new("TABLESPACE_COMMENT", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "TABLESPACES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillTriggers(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TRIGGER_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TRIGGER_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TRIGGER_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("EVENT_MANIPULATION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("EVENT_OBJECT_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("EVENT_OBJECT_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("EVENT_OBJECT_TABLE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ACTION_ORDER", typeof(long)), // lgtm[cs/local-not-disposed]
			new("ACTION_CONDITION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ACTION_STATEMENT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ACTION_ORIENTATION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ACTION_TIMING", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ACTION_REFERENCE_OLD_TABLE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ACTION_REFERENCE_NEW_TABLE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ACTION_REFERENCE_OLD_ROW", typeof(string)), // lgtm[cs/local-not-disposed]
			new("ACTION_REFERENCE_NEW_ROW", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CREATED", typeof(DateTime)), // lgtm[cs/local-not-disposed]
			new("SQL_MODE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DEFINER", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CHARACTER_SET_CLIENT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COLLATION_CONNECTION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DATABASE_COLLATION", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "TRIGGERS", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillUserPrivileges(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("GRANTEE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("PRIVILEGE_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("IS_GRANTABLE", typeof(string)), // lgtm[cs/local-not-disposed]
		});

		await FillDataTableAsync(ioBehavior, dataTable, "USER_PRIVILEGES", cancellationToken).ConfigureAwait(false);
	}

	private async Task FillViews(IOBehavior ioBehavior, DataTable dataTable, CancellationToken cancellationToken)
	{
		dataTable.Columns.AddRange(new DataColumn[]
		{
			new("TABLE_CATALOG", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_SCHEMA", typeof(string)), // lgtm[cs/local-not-disposed]
			new("TABLE_NAME", typeof(string)), // lgtm[cs/local-not-disposed]
			new("VIEW_DEFINITION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CHECK_OPTION", typeof(string)), // lgtm[cs/local-not-disposed]
			new("IS_UPDATABLE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("DEFINER", typeof(string)), // lgtm[cs/local-not-disposed]
			new("SECURITY_TYPE", typeof(string)), // lgtm[cs/local-not-disposed]
			new("CHARACTER_SET_CLIENT", typeof(string)), // lgtm[cs/local-not-disposed]
			new("COLLATION_CONNECTION", typeof(string)), // lgtm[cs/local-not-disposed]
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
