using System;

namespace MySql.Data.Serialization
{
	internal class ResetConnectionPayload
	{
		public static PayloadData Create() => new PayloadData(new ArraySegment<byte>(new[] { (byte) CommandKind.ResetConnection }));
	}
}
