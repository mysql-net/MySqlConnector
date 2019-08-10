namespace MySqlConnector.Protocol.Payloads
{
	internal static class PingPayload
	{
		public static PayloadData Instance { get; } = new PayloadData(new[] { (byte) CommandKind.Ping });
	}
}
