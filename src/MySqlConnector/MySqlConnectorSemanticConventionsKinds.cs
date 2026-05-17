namespace MySqlConnector;

/// <summary>
/// Specifies which OpenTelemetry semantic convention versions MySqlConnector emits for tracing spans.
/// </summary>
[Flags]
public enum MySqlConnectorSemanticConventionsKinds
{
	/// <summary>
	/// Emit no semantic convention attributes or events.
	/// </summary>
	None = 0,

	/// <summary>
	/// Emit legacy experimental database semantic convention attributes and events.
	/// </summary>
	Experimental = 1,

	/// <summary>
	/// Emit stable database semantic convention attributes and events.
	/// </summary>
	Stable = 2,
}
