namespace MySqlConnector.Protocol.Payloads;

internal static class QuitPayload
{
	public static PayloadData Instance { get; } = new([ (byte) CommandKind.Quit ]);
}
