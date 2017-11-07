namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class PingPayload
	{
		public static PayloadData Create() => new PayloadData(new[] { (byte) CommandKind.Ping });
	}
}
