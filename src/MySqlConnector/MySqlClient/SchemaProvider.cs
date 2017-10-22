#if !NETSTANDARD1_3
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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
			dataTable.Columns.AddRange(new[] { // The names come from DbMetaDataColumnNames
				new DataColumn("DataType", typeof(string)),
				new DataColumn("TypeName", typeof(string)),
				new DataColumn("ProviderDbType", typeof(int)),
				new DataColumn("IsUnsigned", typeof(bool))
			});

			// Column type mappings:
			var colTypes = Types.TypeMapper.Instance.GetColumnMappings();
			foreach (var map in colTypes)
			{
				var dbTypeMap = map.DbTypeMapping;
				var dbType = dbTypeMap.DbTypes.FirstOrDefault();
				dataTable.Rows.Add(dbTypeMap.ClrType.FullName, map.DataTypeName, (int)dbType, map.Unsigned);
			}

			// Data type mappings:
			foreach (MySqlDbType mapItem in Enum.GetValues(typeof(MySqlDbType)))
			{
				var typeName = Enum.GetName(typeof(MySqlDbType), mapItem);
				var dbType = Types.TypeMapper.Instance.GetDbTypeForMySqlDbType(mapItem);
				var map = Types.TypeMapper.Instance.GetDbTypeMapping(dbType);
				if (map != null) // MySqlDbType.Set is not supported by the mapper.
				{
					dataTable.Rows.Add(map.ClrType.FullName, Enum.GetName(typeof(MySqlDbType), mapItem).ToLower(), (int)dbType, typeName.Contains("UInt") || typeName.Contains("UByte"));
				}
			}
		}

		private void FillProcedures(DataTable dataTable)
		{
			dataTable.Columns.AddRange(new[] {
				new DataColumn("ROUTINE_TYPE"),
				new DataColumn("ROUTINE_SCHEMA"),
				new DataColumn("SPECIFIC_NAME")
			});

			Action close = null;
			if (m_connection.State != ConnectionState.Open)
			{
				m_connection.Open();
				close = m_connection.Close;
			}

			var procsQuery = "SELECT ROUTINE_TYPE, ROUTINE_SCHEMA, SPECIFIC_NAME FROM INFORMATION_SCHEMA.ROUTINES;";
			using (var command = new MySqlCommand(procsQuery, m_connection))
			using (var reader = command.ExecuteReader())
			{
				while (reader.Read())
					dataTable.Rows.Add(reader.GetString(0), reader.GetString(1), reader.GetString(2));
			}

			close?.Invoke();
		}

		readonly MySqlConnection m_connection;
		readonly Dictionary<string, Action<DataTable>> m_schemaCollections;
	}
}
#endif
