namespace MySqlConnector.Protocol.Payloads
{
	internal readonly struct ResetConnectionPayload
	{
		public static PayloadData Instance { get; } = new PayloadData(new[] { (byte) CommandKind.ResetConnection });
	}
}
