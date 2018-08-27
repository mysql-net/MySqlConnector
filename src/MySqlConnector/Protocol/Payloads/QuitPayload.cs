namespace MySqlConnector.Protocol.Payloads
{
	internal readonly struct QuitPayload
	{
		public static PayloadData Instance { get; } = new PayloadData(new[] { (byte) CommandKind.Quit });
	}
}
