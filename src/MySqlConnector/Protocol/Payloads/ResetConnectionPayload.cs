namespace MySqlConnector.Protocol.Payloads
{
	internal static class ResetConnectionPayload
	{
		public static PayloadData Instance { get; } = new PayloadData(new[] { (byte) CommandKind.ResetConnection });
	}
}
