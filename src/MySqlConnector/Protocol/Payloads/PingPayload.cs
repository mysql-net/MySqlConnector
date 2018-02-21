namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class PingPayload
	{
		public static PayloadData Instance { get; } = new PayloadData(new[] { (byte) CommandKind.Ping });
	}
}
