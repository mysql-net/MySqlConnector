namespace MySqlConnector;

/// <summary>
/// <see cref="MySqlConnectorTracingOptionsBuilder"/> provides an API for configuring OpenTelemetry tracing options.
/// </summary>
public sealed class MySqlConnectorTracingOptionsBuilder
{
	/// <summary>
	/// Gets or sets a value indicating whether to enable the "read-result-set-header" event.
	/// Default is false; set to true to opt in to this event.
	/// </summary>
	public MySqlConnectorTracingOptionsBuilder EnableResultSetHeaderEvent(bool enable = true)
	{
		m_enableResultSetHeaderEvent = enable;
		return this;
	}

	internal MySqlConnectorTracingOptions Build() =>
		new()
		{
			EnableResultSetHeaderEvent = m_enableResultSetHeaderEvent,
		};

	private bool m_enableResultSetHeaderEvent = MySqlConnectorTracingOptions.Default.EnableResultSetHeaderEvent;
}
