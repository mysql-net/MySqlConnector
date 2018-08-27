using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	internal readonly struct InitDatabasePayload
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
