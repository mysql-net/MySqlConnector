namespace MySqlConnector.Protocol.Payloads
{
	internal static class PingPayload
	{
		public static PayloadData Instance { get; } = new(new[] { (byte) CommandKind.Ping });
	}
}
