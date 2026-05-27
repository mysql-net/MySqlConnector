using Microsoft.Extensions.Logging;
using MySqlConnector.Logging;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Helpers;

internal static class ColumnMapperHelper
{
	internal static async Task FillColumnMappingsAsync(
		string tableName,
		int fieldCount,
		ILogger logger,
		IOBehavior ioBehavior,
		MySqlBulkLoader bulkLoader,
		List<MySqlBulkCopyColumnMapping> externalColumnMappings,
		MySqlConnection connection,
		MySqlTransaction? transaction,
		CancellationToken cancellationToken)
	{
		// merge column mappings with the destination schema
		var columnMappings = new List<MySqlBulkCopyColumnMapping>(externalColumnMappings);
		var addDefaultMappings = columnMappings.Count == 0;
		using (var cmd = new MySqlCommand("select * from " + tableName + ";", connection, transaction))
		using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SchemaOnly, ioBehavior, cancellationToken).ConfigureAwait(false))
		{
			var schema = reader.GetColumnSchema();
			for (var i = 0; i < schema.Count; i++)
			{
				var destinationColumn = reader.GetName(i);
				var dataTypeName = schema[i].DataTypeName;
				if (dataTypeName == "BIT")
				{
					AddColumnMapping(logger, columnMappings, addDefaultMappings, i, destinationColumn, $"@`\uE002\bcol{i}`", $"%COL% = CAST(%VAR% AS UNSIGNED)");
				}
				else
				{
					var type = schema[i].DataType;
					if (type == typeof(byte[]) ||
						dataTypeName == "VECTOR" ||
						(type == typeof(Guid) && (connection.GuidFormat is MySqlGuidFormat.Binary16 or MySqlGuidFormat.LittleEndianBinary16 or MySqlGuidFormat.TimeSwapBinary16)))
					{
						AddColumnMapping(logger, columnMappings, addDefaultMappings, i, destinationColumn, $"@`\uE002\bcol{i}`", $"%COL% = UNHEX(%VAR%)");
					}
					else if (addDefaultMappings)
					{
						if (schema[i].DataTypeName == "YEAR")
						{
							// the current code can't distinguish between 0 = 0000 and 0 = 2000
							throw new NotSupportedException("'YEAR' columns are not supported by MySqlBulkCopy.");
						}

						Log.AddingDefaultColumnMapping(logger, i, destinationColumn);
						columnMappings.Add(new(i, destinationColumn));
					}
				}
			}
		}

		// set columns and expressions from the column mappings
		for (var i = 0; i < fieldCount; i++)
		{
			var columnMapping = columnMappings.FirstOrDefault(x => x.SourceOrdinal == i);
			if (columnMapping is null)
			{
				Log.IgnoringColumn(logger, i);
				bulkLoader.Columns.Add("@`\uE002\bignore`");
			}
			else
			{
				if (columnMapping.DestinationColumn.Length == 0)
					throw new InvalidOperationException($"MySqlBulkCopyColumnMapping.DestinationName is not set for SourceOrdinal {columnMapping.SourceOrdinal}");
				if (columnMapping.DestinationColumn[0] == '@' && columnMapping.Expression is not null)
					bulkLoader.Columns.Add(columnMapping.DestinationColumn);
				else
					bulkLoader.Columns.Add(QuoteIdentifier(columnMapping.DestinationColumn));
				if (columnMapping.Expression is not null)
					bulkLoader.Expressions.Add(columnMapping.Expression);
			}
		}

		foreach (var columnMapping in columnMappings)
		{
			if (columnMapping.SourceOrdinal < 0 || columnMapping.SourceOrdinal >= fieldCount)
				throw new InvalidOperationException($"SourceOrdinal {columnMapping.SourceOrdinal} is an invalid value");
		}
	}

	private static void AddColumnMapping(
		ILogger logger,
		List<MySqlBulkCopyColumnMapping> columnMappings,
		bool addDefaultMappings,
		int destinationOrdinal,
		string destinationColumn,
		string variableName,
		string expression)
	{
		expression = expression.Replace("%COL%", "`" + destinationColumn + "`").Replace("%VAR%", variableName);
		var columnMapping = columnMappings.FirstOrDefault(x => destinationColumn.Equals(x.DestinationColumn, StringComparison.OrdinalIgnoreCase));
		if (columnMapping is not null)
		{
			if (columnMapping.Expression is not null)
			{
				Log.ColumnMappingAlreadyHasExpression(logger, columnMapping.SourceOrdinal, destinationColumn, columnMapping.Expression);
			}
			else
			{
				Log.SettingExpressionToMapColumn(logger, columnMapping.SourceOrdinal, destinationColumn, expression);
				columnMappings.Remove(columnMapping);
				columnMappings.Add(new(columnMapping.SourceOrdinal, variableName, expression));
			}
		}
		else if (addDefaultMappings)
		{
			Log.AddingDefaultColumnMapping(logger, destinationOrdinal, destinationColumn);
			columnMappings.Add(new(destinationOrdinal, variableName, expression));
		}
	}

	private static string QuoteIdentifier(string identifier) => "`" + identifier.Replace("`", "``") + "`";
}
