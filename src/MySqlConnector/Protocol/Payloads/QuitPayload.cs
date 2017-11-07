using System;

namespace MySql.Data.Serialization
{
	internal class QuitPayload
	{
		public static ArraySegment<byte> Create() => new ArraySegment<byte>(new[] { (byte) CommandKind.Quit });
	}
}
