using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using MySqlConnector.Logging;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Core;

internal sealed class CachedProcedure
{
	public static async Task<CachedProcedure?> FillAsync(IOBehavior ioBehavior, MySqlConnection connection, string schema, string component, CancellationToken cancellationToken)
	{
		// try to use mysql.proc first, as it is much faster
		if (connection.Session.ServerVersion.Version < ServerVersions.RemovesMySqlProcTable && !connection.Session.ProcAccessDenied)
		{
			try
			{
				using var cmd = connection.CreateCommand();
				cmd.Transaction = connection.CurrentTransaction;
				cmd.CommandText = @"SELECT param_list, returns FROM mysql.proc WHERE db = @schema AND name = @component";
				cmd.Parameters.AddWithValue("@schema", schema);
				cmd.Parameters.AddWithValue("@component", component);

				using var reader = await cmd.ExecuteReaderNoResetTimeoutAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
				var exists = await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
				if (!exists)
					return null;

				var parametersSqlBytes = (byte[]) reader.GetValue(0);
				var returnsSqlBytes = (byte[]) reader.GetValue(1);

				// ASSUME this is UTF-8 encoded; it's possible that the `character_set_client` column would need to be used?
				var parametersSql = Encoding.UTF8.GetString(parametersSqlBytes);
				var returnsSql = Encoding.UTF8.GetString(returnsSqlBytes);

				var parsedParameters = ParseParameters(parametersSql);
				if (returnsSql.Length != 0)
				{
					var returnDataType = ParseDataType(returnsSql, out var unsigned, out var length);
					parsedParameters.Insert(0, CreateCachedParameter(0, null, "", returnDataType, unsigned, length, returnsSql));
				}

				return new CachedProcedure(schema, component, parsedParameters);
			}
			catch (MySqlException ex)
			{
				Log.Info("Session{0} failed to retrieve metadata for Schema={1} Component={2}; falling back to INFORMATION_SCHEMA. Error: {3}", connection.Session.Id, schema, component, ex.Message);
				if (ex.ErrorCode == MySqlErrorCode.TableAccessDenied)
					connection.Session.ProcAccessDenied = true;
			}
		}

		if (connection.Session.ServerVersion.Version < ServerVersions.SupportsProcedureCache)
		{
			Log.Info("Session{0} ServerVersion={1} does not support cached procedures", connection.Session.Id, connection.Session.ServerVersion.OriginalString);
			return null;
		}

		var parameters = new List<CachedParameter>();
		int routineCount;
		using (var cmd = connection.CreateCommand())
		{
			cmd.Transaction = connection.CurrentTransaction;
			cmd.CommandText = @"SELECT COUNT(*)
				FROM information_schema.routines
				WHERE ROUTINE_SCHEMA = @schema AND ROUTINE_NAME = @component;
				SELECT ORDINAL_POSITION, PARAMETER_MODE, PARAMETER_NAME, DTD_IDENTIFIER
				FROM information_schema.parameters
				WHERE SPECIFIC_SCHEMA = @schema AND SPECIFIC_NAME = @component
				ORDER BY ORDINAL_POSITION";
			cmd.Parameters.AddWithValue("@schema", schema);
			cmd.Parameters.AddWithValue("@component", component);

			using var reader = await cmd.ExecuteReaderNoResetTimeoutAsync(CommandBehavior.Default, ioBehavior, cancellationToken).ConfigureAwait(false);
			await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
			routineCount = reader.GetInt32(0);
			await reader.NextResultAsync(ioBehavior, cancellationToken).ConfigureAwait(false);

			while (await reader.ReadAsync(ioBehavior, cancellationToken).ConfigureAwait(false))
			{
				var dataType = ParseDataType(reader.GetString(3), out var unsigned, out var length);
				parameters.Add(new(
					reader.GetInt32(0),
					!reader.IsDBNull(1) ? reader.GetString(1) : null,
					!reader.IsDBNull(2) ? reader.GetString(2) : "",
					dataType,
					unsigned,
					length
				));
			}
		}

		if (Log.IsTraceEnabled())
			Log.Trace("Procedure for Schema={0} Component={1} has RoutineCount={2}, ParameterCount={3}", schema, component, routineCount, parameters.Count);
		return routineCount == 0 ? null : new CachedProcedure(schema, component, parameters);
	}

	public IReadOnlyList<CachedParameter> Parameters { get; }

	private CachedProcedure(string schema, string component, IReadOnlyList<CachedParameter> parameters)
	{
		m_schema = schema;
		m_component = component;
		Parameters = parameters;
	}

	internal MySqlParameterCollection AlignParamsWithDb(MySqlParameterCollection? parameterCollection)
	{
		var alignedParams = new MySqlParameterCollection();
		var returnParam = parameterCollection?.FirstOrDefault(static x => x.Direction == ParameterDirection.ReturnValue);

		foreach (var cachedParam in Parameters)
		{
			MySqlParameter alignParam;
			if (cachedParam.Direction == ParameterDirection.ReturnValue)
			{
				alignParam = returnParam ?? throw new InvalidOperationException($"Attempt to call stored function {FullyQualified} without specifying a return parameter");
			}
			else
			{
				var index = parameterCollection?.NormalizedIndexOf(cachedParam.Name) ?? -1;
				alignParam = index >= 0 ? parameterCollection![index] : throw new ArgumentException($"Parameter '{cachedParam.Name}' not found in the collection.");
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

	internal static List<CachedParameter> ParseParameters(string parametersSql)
	{
		// strip comments
		parametersSql = s_cStyleComments.Replace(parametersSql, "");
		parametersSql = s_singleLineComments.Replace(parametersSql, "");

		// normalize spaces
		parametersSql = s_multipleSpaces.Replace(parametersSql, " ");

		if (string.IsNullOrWhiteSpace(parametersSql))
			return new List<CachedParameter>();

		// strip precision specifier containing comma
		parametersSql = s_numericTypes.Replace(parametersSql, @"$1");

		// strip enum values containing commas (these would have been stripped by ParseDataType anyway)
		parametersSql = s_enum.Replace(parametersSql, "ENUM");

		var parameters = parametersSql.Split(',');
		var cachedParameters = new List<CachedParameter>(parameters.Length);
		for (var i = 0; i < parameters.Length; i++)
		{
			var parameter = parameters[i].Trim();
			var originalString = parameter;
			string direction = "IN";
			if (parameter.StartsWith("INOUT ", StringComparison.OrdinalIgnoreCase))
			{
				direction = "INOUT";
				parameter = parameter.Substring(6);
			}
			else if (parameter.StartsWith("OUT ", StringComparison.OrdinalIgnoreCase))
			{
				direction = "OUT";
				parameter = parameter.Substring(4);
			}
			else if (parameter.StartsWith("IN ", StringComparison.OrdinalIgnoreCase))
			{
				direction = "IN";
				parameter = parameter.Substring(3);
			}

			var parts = s_parameterName.Match(parameter);
			var name = parts.Groups[1].Success ? parts.Groups[1].Value.Replace("``", "`") : parts.Groups[2].Value;

			var dataType = ParseDataType(parts.Groups[3].Value, out var unsigned, out var length);
			cachedParameters.Add(CreateCachedParameter(i + 1, direction, name, dataType, unsigned, length, originalString));
		}

		return cachedParameters;
	}

	internal static string ParseDataType(string sql, out bool unsigned, out int length)
	{
		sql = s_characterSet.Replace(sql, "");
		sql = s_collate.Replace(sql, "");
		sql = s_enum.Replace(sql, "ENUM");

		length = 0;
		var match = s_length.Match(sql);
		if (match.Success)
		{
			length = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
			sql = s_length.Replace(sql, "");
		}

		var list = sql.Trim().Split(new[] { ' ' });
		var type = string.Empty;

		if (list.Length < 2 || !s_typeMapping.TryGetValue(list[0] + ' ' + list[1], out type))
		{
			if (s_typeMapping.TryGetValue(list[0], out type))
			{
				if (list[0].StartsWith("BOOL", StringComparison.OrdinalIgnoreCase))
				{
					length = 1;
				}
			}
		}

		unsigned = list.Contains("UNSIGNED", StringComparer.OrdinalIgnoreCase);
		return type ?? list[0];
	}

	private static CachedParameter CreateCachedParameter(int ordinal, string? direction, string name, string dataType, bool unsigned, int length, string originalSql)
	{
		try
		{
			return new CachedParameter(ordinal, direction, name, dataType, unsigned, length);
		}
		catch (NullReferenceException ex)
		{
			throw new MySqlException($"Failed to parse stored procedure parameter '{originalSql}'; extracted data type was {dataType}", ex);
		}
	}

	private string FullyQualified => $"`{m_schema}`.`{m_component}`";

	private static readonly IMySqlConnectorLogger Log = MySqlConnectorLogManager.CreateLogger(nameof(CachedProcedure));
	private static readonly IReadOnlyDictionary<string, string> s_typeMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		{ "BOOL", "TINYINT" },
		{ "BOOLEAN", "TINYINT" },
		{ "INTEGER", "INT" },
		{ "NUMERIC", "DECIMAL" },
		{ "FIXED", "DECIMAL" },
		{ "REAL", "DOUBLE" },
		{ "DOUBLE PRECISION", "DOUBLE" },
		{ "NVARCHAR", "VARCHAR" },
		{ "CHARACTER VARYING", "VARCHAR" },
		{ "NATIONAL VARCHAR", "VARCHAR" },
		{ "NCHAR", "CHAR" },
		{ "CHARACTER", "CHAR" },
		{ "NATIONAL CHAR", "CHAR" },
		{ "CHAR BYTE", "BINARY" },
	};

	private static readonly Regex s_cStyleComments = new(@"/\*.*?\*/", RegexOptions.Singleline);
	private static readonly Regex s_singleLineComments = new(@"(^|\s)--.*?$", RegexOptions.Multiline);
	private static readonly Regex s_multipleSpaces = new(@"\s+");
	private static readonly Regex s_numericTypes = new(@"(DECIMAL|DEC|FIXED|NUMERIC|FLOAT|DOUBLE PRECISION|DOUBLE|REAL)\s*\([0-9]+(,\s*[0-9]+)\)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	private static readonly Regex s_enum = new(@"ENUM\s*\([^)]+\)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	private static readonly Regex s_parameterName = new(@"^(?:`((?:[\u0001-\u005F\u0061-\uFFFF]+|``)+)`|([A-Za-z0-9$_\u0080-\uFFFF]+)) (.*)$");
	private static readonly Regex s_characterSet = new(" (CHARSET|CHARACTER SET) [A-Za-z0-9_]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	private static readonly Regex s_collate = new(" (COLLATE) [A-Za-z0-9_]+", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
	private static readonly Regex s_length = new(@"\s*\(\s*([0-9]+)\s*(?:,\s*[0-9]+\s*)?\)");

	private readonly string m_schema;
	private readonly string m_component;
}
