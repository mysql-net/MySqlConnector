using System;

namespace MySqlConnector.Protocol.Payloads
{
	internal class ResetConnectionPayload
	{
		public static PayloadData Create() => new PayloadData(new ArraySegment<byte>(new[] { (byte) CommandKind.ResetConnection }));
	}
}
