namespace MySqlConnector.Protocol.Payloads
{
	internal static class QuitPayload
	{
		public static PayloadData Instance { get; } = new PayloadData(new[] { (byte) CommandKind.Quit });
	}
}
