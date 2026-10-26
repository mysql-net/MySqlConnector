using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads;

internal static class StatementClosePayload
{
	public static PayloadData Create(int statementId)
	{
		var writer = new ByteBufferWriter();

		writer.Write((byte) CommandKind.StatementClose);
		writer.Write(statementId);

		return writer.ToPayloadData();
	}
}
