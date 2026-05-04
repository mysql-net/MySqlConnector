namespace MySqlConnector;

internal sealed class MySqlConnectorTracingOptions
{
	public bool EnableResultSetHeaderEvent { get; set; }

	public MySqlConnectorSemanticConventionsKinds SemanticConventionsKinds { get; set; }

	public static MySqlConnectorTracingOptions Default { get; } = new()
	{
		SemanticConventionsKinds = GetDefaultSemanticConventions(),
	};

	private static MySqlConnectorSemanticConventionsKinds GetDefaultSemanticConventions()
	{
		var foundValue = false;
		var kinds = MySqlConnectorSemanticConventionsKinds.None;

		// look for the environment variable documented at https://opentelemetry.io/docs/specs/semconv/db/database-spans/
		if (Environment.GetEnvironmentVariable("OTEL_SEMCONV_STABILITY_OPT_IN") is { Length: > 0 } optIn)
		{
			var values = optIn.Split([','], StringSplitOptions.RemoveEmptyEntries);
			foreach (var value in values)
			{
				if (value.Trim() == "database")
				{
					kinds |= MySqlConnectorSemanticConventionsKinds.Stable;
					foundValue = true;
				}
				else if (value.Trim() == "database/dup")
				{
					// database/dup means to emit both kinds and takes precedence over "database"
					kinds |= MySqlConnectorSemanticConventionsKinds.Stable;
					kinds |= MySqlConnectorSemanticConventionsKinds.Experimental;
					foundValue = true;
				}
			}
		}

		// if no value was found, our default is "database/dup": emit both kinds
		if (!foundValue)
			kinds = MySqlConnectorSemanticConventionsKinds.Stable | MySqlConnectorSemanticConventionsKinds.Experimental;

		return kinds;
	}
}
