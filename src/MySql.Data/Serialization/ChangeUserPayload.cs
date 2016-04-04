using System;

namespace MySql.Data.Serialization
{
	internal class ChangeUserPayload
	{
		public static PayloadData Create(string user, byte[] authResponse, string schemaName)
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

			return new PayloadData(new ArraySegment<byte>(writer.ToBytes()));
		}
	}
}
