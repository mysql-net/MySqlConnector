using System.Text;

namespace MySqlConnector.Protocol.Payloads;

internal static class QueryPayload
{
	public static PayloadData Create(bool supportsQueryAttributes, string query)
	{
		var length = Encoding.UTF8.GetByteCount(query);
		var payload = new byte[length + 1 + (supportsQueryAttributes ? 2 : 0)];
		payload[0] = (byte) CommandKind.Query;
		if (supportsQueryAttributes)
			payload[2] = 1;
		Encoding.UTF8.GetBytes(query, 0, query.Length, payload, supportsQueryAttributes ? 3 : 1);
		return new PayloadData(payload);
	}
}
