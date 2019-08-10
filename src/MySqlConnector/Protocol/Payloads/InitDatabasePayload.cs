using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	internal static class InitDatabasePayload
	{
		public static PayloadData Create(string databaseName)
		{
			var writer = new ByteBufferWriter();

			writer.Write((byte) CommandKind.InitDatabase);
			writer.Write(databaseName);

			return writer.ToPayloadData();
		}
	}
}
