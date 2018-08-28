namespace MySqlConnector.Protocol.Payloads
{
	internal static class EmptyPayload
	{
		public static PayloadData Instance { get; } = new PayloadData(new byte[0]);
	}
}
