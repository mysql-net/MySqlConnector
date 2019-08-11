using System;

namespace MySqlConnector.Protocol.Serialization
{
	internal readonly struct Packet
	{
		public Packet(ArraySegment<byte> contents)
		{
			Contents = contents;
		}

		public ArraySegment<byte> Contents { get; }
	}
}
