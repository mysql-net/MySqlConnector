namespace MySqlConnector;

/// <summary>
/// <see cref="MySqlConnectorTracingOptionsBuilder"/> provides an API for configuring OpenTelemetry tracing options.
/// </summary>
public sealed class MySqlConnectorTracingOptionsBuilder
{
	/// <summary>
	/// Sets whether to enable the "read-result-set-header" event.
	/// Default is false; set to true to opt in to this event.
	/// </summary>
	public MySqlConnectorTracingOptionsBuilder EnableResultSetHeaderEvent(bool enable = true)
	{
		m_enableResultSetHeaderEvent = enable;
		return this;
	}

	/// <summary>
	/// Sets the kinds of database conventions emitted for tracing spans.
	/// </summary>
	/// <param name="kinds">The kinds of semantic conventions to emit.</param>
	/// <returns>This builder, so options can be chained.</returns>
	/// <remarks>The default is controlled by the <c>OTEL_SEMCONV_STABILITY_OPT_IN</c> environment variable.</remarks>
	public MySqlConnectorTracingOptionsBuilder WithSemanticConventionsKinds(MySqlConnectorSemanticConventionsKinds kinds)
	{
		if (kinds is not (MySqlConnectorSemanticConventionsKinds.Experimental or MySqlConnectorSemanticConventionsKinds.Stable or
				MySqlConnectorSemanticConventionsKinds.Experimental | MySqlConnectorSemanticConventionsKinds.Stable))
		{
			throw new ArgumentOutOfRangeException(nameof(kinds), "kinds must be Experimental or Stable conventions (or both).");
		}
		m_semanticConventionsKinds = kinds;
		return this;
	}

	internal MySqlConnectorTracingOptions Build() =>
		new()
		{
			EnableResultSetHeaderEvent = m_enableResultSetHeaderEvent,
			SemanticConventionsKinds = m_semanticConventionsKinds,
		};

	private bool m_enableResultSetHeaderEvent = MySqlConnectorTracingOptions.Default.EnableResultSetHeaderEvent;
	private MySqlConnectorSemanticConventionsKinds m_semanticConventionsKinds = MySqlConnectorTracingOptions.Default.SemanticConventionsKinds;
}
