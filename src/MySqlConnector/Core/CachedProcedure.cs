using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core
{
	internal sealed class CachedProcedure
	{
		public static async Task<CachedProcedure> FillAsync(IOBehavior ioBehavior, MySqlConnection connection, string schema, string component, CancellationToken cancellationToken)
		{
			var parameters = new List<CachedParameter>();
			int routineCount;
			using (var cmd = connection.CreateCommand())
			{
				cmd.Transaction = connection.CurrentTransaction;
				cmd.CommandText = @"SELECT COUNT(*)
					FROM information_schema.routines
					WHERE ROUTINE_SCHEMA = @schema AND ROUTINE_NAME = @component;
					SELECT ORDINAL_POSITION, PARAMETER_MODE, PARAMETER_NAME, DATA_TYPE, DTD_IDENTIFIER
					FROM information_schema.parameters
					WHERE SPECIFIC_SCHEMA = @schema AND SPECIFIC_NAME = @component
					ORDER BY ORDINAL_POSITION";
				cmd.Parameters.AddWithValue("@schema", schema);
				cmd.Parameters.AddWithValue("@component", component);

				using (var reader = (MySqlDataReader) await cmd.ExecuteReaderAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false))
				{
					await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
					routineCount = reader.GetInt32(0);
					await reader.NextResultAsync(cancellationToken).ConfigureAwait(false);

					while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
					{
						parameters.Add(new CachedParameter(
							reader.GetInt32(0),
							!reader.IsDBNull(1) ? reader.GetString(1) : null,
							!reader.IsDBNull(2) ? reader.GetString(2) : null,
							reader.GetString(3),
							reader.GetString(4).IndexOf("unsigned", StringComparison.OrdinalIgnoreCase) != -1
						));
					}
				}
			}

			return routineCount == 0 ? null : new CachedProcedure(schema, component, parameters.AsReadOnly());
		}

		public ReadOnlyCollection<CachedParameter> Parameters { get; }

		private CachedProcedure(string schema, string component, ReadOnlyCollection<CachedParameter> parameters)
		{
			m_schema = schema;
			m_component = component;
			Parameters = parameters;
		}

		internal MySqlParameterCollection AlignParamsWithDb(MySqlParameterCollection parameterCollection)
		{
			var alignedParams = new MySqlParameterCollection();
			var returnParam = parameterCollection.FirstOrDefault(x => x.Direction == ParameterDirection.ReturnValue);

			foreach (var cachedParam in Parameters)
			{
				MySqlParameter alignParam;
				if (cachedParam.Direction == ParameterDirection.ReturnValue)
				{
					alignParam = returnParam ?? throw new InvalidOperationException($"Attempt to call stored function {FullyQualified} without specifying a return parameter");
				}
				else
				{
					var index = parameterCollection.NormalizedIndexOf(cachedParam.Name);
					alignParam = index >= 0 ? parameterCollection[index] : throw new ArgumentException($"Parameter '{cachedParam.Name}' not found in the collection.");
				}

				if (!alignParam.HasSetDirection)
					alignParam.Direction = cachedParam.Direction;
				if (!alignParam.HasSetDbType)
					alignParam.MySqlDbType = cachedParam.MySqlDbType;

				// cached parameters are oredered by ordinal position
				alignedParams.Add(alignParam);
			}

			return alignedParams;
		}

		private string FullyQualified => $"`{m_schema}`.`{m_component}`";

		readonly string m_schema;
		readonly string m_component;
	}
}
