using System.Text;

namespace MySqlConnector.Protocol.Payloads
{
	internal static class QueryPayload
	{
		public static PayloadData Create(string query)
		{
			var length = Encoding.UTF8.GetByteCount(query);
			var payload = new byte[length + 1];
			payload[0] = (byte) CommandKind.Query;
			Encoding.UTF8.GetBytes(query, 0, query.Length, payload, 1);
			return new PayloadData(payload);
		}
	}
}
