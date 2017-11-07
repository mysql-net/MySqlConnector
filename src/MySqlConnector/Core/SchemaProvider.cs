#if !NETSTANDARD1_3
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using MySql.Data.MySqlClient.Types;

namespace MySql.Data.MySqlClient
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
				{ "Procedures", FillProcedures }
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

		readonly MySqlConnection m_connection;
		readonly Dictionary<string, Action<DataTable>> m_schemaCollections;
	}
}
#endif
