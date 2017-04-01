using System;

namespace MySql.Data.Serialization
{
	internal class PingPayload
	{
		public static PayloadData Create() => new PayloadData(new ArraySegment<byte>(new[] { (byte) CommandKind.Ping }));
	}
}
