using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Payloads
{
	internal static class EmptyPayload
	{
		public static PayloadData Instance { get; } = new(Utility.EmptyByteArray);
	}
}
