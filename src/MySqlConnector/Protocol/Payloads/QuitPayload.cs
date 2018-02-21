namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class QuitPayload
	{
		public static PayloadData Instance { get; } = new PayloadData(new[] { (byte) CommandKind.Quit });
	}
}
