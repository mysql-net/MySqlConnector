namespace MySqlConnector.Protocol.Payloads;

internal static class QueryPayload
{
	public static PayloadData Create(bool supportsQueryAttributes, ReadOnlySpan<byte> query)
	{
		var payload = new byte[query.Length + 1 + (supportsQueryAttributes ? 2 : 0)];
		payload[0] = (byte) CommandKind.Query;
		if (supportsQueryAttributes)
			payload[2] = 1;
		query.CopyTo(payload.AsSpan(supportsQueryAttributes ? 3 : 1));
		return new PayloadData(payload);
	}
}
