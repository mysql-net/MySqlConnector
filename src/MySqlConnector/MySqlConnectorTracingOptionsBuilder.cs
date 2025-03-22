namespace MySqlConnector;

/// <summary>
/// <see cref="MySqlConnectorTracingOptionsBuilder"/> provides an API for configuring OpenTelemetry tracing options.
/// </summary>
public sealed class MySqlConnectorTracingOptionsBuilder
{
	/// <summary>
	/// Gets or sets a value indicating whether to enable the "time-to-first-read" event.
	/// Default is true to preserve existing behavior.
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
