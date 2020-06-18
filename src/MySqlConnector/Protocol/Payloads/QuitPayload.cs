namespace MySqlConnector.Protocol.Payloads
{
	internal static class QuitPayload
	{
		public static PayloadData Instance { get; } = new(new[] { (byte) CommandKind.Quit });
	}
}
