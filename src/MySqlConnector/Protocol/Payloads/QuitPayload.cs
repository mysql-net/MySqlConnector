using System;

namespace MySqlConnector.Protocol.Payloads
{
	internal sealed class QuitPayload
	{
		public static ArraySegment<byte> Create() => new ArraySegment<byte>(new[] { (byte) CommandKind.Quit });
	}
}
