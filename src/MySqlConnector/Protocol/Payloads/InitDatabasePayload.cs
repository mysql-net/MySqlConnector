using System;
using System.Text;

namespace MySql.Data.Serialization
{
	internal class InitDatabasePayload
	{
		public static PayloadData Create(string databaseName)
		{
			var writer = new PayloadWriter();

			writer.WriteByte((byte) CommandKind.InitDatabase);
			writer.Write(Encoding.UTF8.GetBytes(databaseName));

			return new PayloadData(new ArraySegment<byte>(writer.ToBytes()));
		}
	}
}
