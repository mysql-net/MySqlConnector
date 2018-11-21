using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	internal static class ChangeUserPayload
	{
		public static PayloadData Create(string user, byte[] authResponse, string schemaName, byte[] connectionAttributes)
		{
			var writer = new ByteBufferWriter();

			writer.Write((byte) CommandKind.ChangeUser);
			writer.WriteNullTerminatedString(user);
			writer.Write(checked((byte) authResponse.Length));
			writer.Write(authResponse);
			writer.WriteNullTerminatedString(schemaName ?? "");
			writer.Write((byte) CharacterSet.Utf8Mb4GeneralCaseInsensitive);
			writer.Write((byte) 0);
			writer.WriteNullTerminatedString("mysql_native_password");
			if (connectionAttributes != null)
				writer.Write(connectionAttributes);

			return writer.ToPayloadData();
		}
	}
}
