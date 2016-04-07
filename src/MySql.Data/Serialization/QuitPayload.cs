using System;

namespace MySql.Data.Serialization
{
	internal class QuitPayload
	{
		public static PayloadData Create()
		{
			return new PayloadData(new ArraySegment<byte>(new[] { (byte) CommandKind.Quit }));
		}
	}
}
