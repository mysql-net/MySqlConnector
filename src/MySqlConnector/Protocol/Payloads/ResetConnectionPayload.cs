namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class ResetConnectionPayload
	{
		public static PayloadData Create() => new PayloadData(new[] { (byte) CommandKind.ResetConnection });
	}
}
