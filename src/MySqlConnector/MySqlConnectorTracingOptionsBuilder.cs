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
	/// Formerly used to set the kinds of database conventions emitted for tracing spans.
	/// </summary>
	/// <param name="kinds">The kinds of semantic conventions to emit.</param>
	/// <returns>This builder, so options can be chained.</returns>
	/// <remarks>This method only exists for backwards compatibility with MySqlConnector 2.6.x and has no effect.</remarks>
	[Obsolete("Only Stable conventions are supported; calling this method has no effect.")]
	public MySqlConnectorTracingOptionsBuilder WithSemanticConventionsKinds(MySqlConnectorSemanticConventionsKinds kinds) => this;

	internal MySqlConnectorTracingOptions Build() =>
		new()
		{
			EnableResultSetHeaderEvent = m_enableResultSetHeaderEvent,
		};

	private bool m_enableResultSetHeaderEvent = MySqlConnectorTracingOptions.Default.EnableResultSetHeaderEvent;
}
