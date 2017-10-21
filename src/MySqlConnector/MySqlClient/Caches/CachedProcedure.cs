using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySql.Data.Protocol.Serialization;

namespace MySql.Data.MySqlClient.Caches
{
	internal class CachedProcedure
	{
		internal static async Task<CachedProcedure> FillAsync(IOBehavior ioBehavior, MySqlConnection connection, string schema, string component, CancellationToken cancellationToken)
		{
			var cmd = connection.CreateCommand();

			cmd.CommandText = @"SELECT ORDINAL_POSITION, PARAMETER_MODE, PARAMETER_NAME, DATA_TYPE, DTD_IDENTIFIER
				FROM information_schema.parameters
				WHERE SPECIFIC_SCHEMA = @schema AND SPECIFIC_NAME = @component
				ORDER BY ORDINAL_POSITION";
			cmd.Parameters.Add(new MySqlParameter
			{
				ParameterName = "@schema",
				Value = schema
			});
			cmd.Parameters.Add(new MySqlParameter
			{
				ParameterName = "@component",
				Value = component
			});

			var parameters = new List<CachedParameter>();
			using (var reader = (MySqlDataReader) await cmd.ExecuteReaderAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false))
			{
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

			return new CachedProcedure(schema, component, parameters.AsReadOnly());
		}

		protected CachedProcedure(string schema, string component, ReadOnlyCollection<CachedParameter> parameters)
		{
			m_schema = schema;
			m_component = component;
			m_parameters = parameters;
		}

		internal MySqlParameterCollection AlignParamsWithDb(MySqlParameterCollection parameterCollection)
		{
			var alignedParams = new MySqlParameterCollection();
			var returnParam = parameterCollection.FirstOrDefault(x => x.Direction == ParameterDirection.ReturnValue);

			foreach (var cachedParam in m_parameters)
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
					alignParam.DbType = cachedParam.DbType;

				// cached parameters are oredered by ordinal position
				alignedParams.Add(alignParam);
			}

			return alignedParams;
		}

		private string FullyQualified => $"`{m_schema}`.`{m_component}`";

		readonly string m_schema;
		readonly string m_component;
		readonly ReadOnlyCollection<CachedParameter> m_parameters;
	}
}
