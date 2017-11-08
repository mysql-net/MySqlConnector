namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class QuitPayload
	{
		public static PayloadData Create() => new PayloadData(new[] { (byte) CommandKind.Quit });
	}
}
