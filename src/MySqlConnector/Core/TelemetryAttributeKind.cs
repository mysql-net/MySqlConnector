namespace MySqlConnector.Core;

[Flags]
internal enum TelemetryAttributeKind
{
	None = 0,
	TraceParent = 1,
	TraceState = 2,
}
