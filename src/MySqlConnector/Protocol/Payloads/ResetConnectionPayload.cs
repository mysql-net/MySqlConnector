namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class ResetConnectionPayload
	{
		public static PayloadData Instance { get; } = new PayloadData(new[] { (byte) CommandKind.ResetConnection });
	}
}
