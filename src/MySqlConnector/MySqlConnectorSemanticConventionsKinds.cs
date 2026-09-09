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
	[Obsolete("Stable conventions must be used.")]
	None = 0,

	/// <summary>
	/// Emit legacy experimental database semantic convention attributes and events.
	/// This value is obsolete and is no longer supported.
	/// </summary>
	[Obsolete("Experimental semantic conventions are no longer supported; use Stable.")]
	Experimental = 1,

	/// <summary>
	/// Emit stable database semantic convention attributes and events.
	/// </summary>
	Stable = 2,
}
