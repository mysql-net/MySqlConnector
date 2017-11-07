using System;

namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class QuitPayload
	{
		public static PayloadData Create() => new PayloadData(new ArraySegment<byte>(new[] { (byte) CommandKind.Quit }));
	}
}
