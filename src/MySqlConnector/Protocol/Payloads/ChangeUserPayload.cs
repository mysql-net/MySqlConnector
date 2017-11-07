using System;
using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads
{
	internal class ChangeUserPayload
	{
		public static PayloadData Create(string user, byte[] authResponse, string schemaName, byte[] connectionAttributes)
		{
			var writer = new PayloadWriter();

			writer.WriteByte((byte) CommandKind.ChangeUser);
			writer.WriteNullTerminatedString(user);
			writer.WriteByte(checked((byte) authResponse.Length));
			writer.Write(authResponse);
			writer.WriteNullTerminatedString(schemaName ?? "");
			writer.WriteByte((byte) CharacterSet.Utf8Mb4Binary);
			writer.WriteByte(0);
			writer.WriteNullTerminatedString("mysql_native_password");
			if (connectionAttributes != null)
				writer.Write(connectionAttributes);

			return new PayloadData(new ArraySegment<byte>(writer.ToBytes()));
		}
	}
}
