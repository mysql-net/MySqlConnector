using System;

namespace MySql.Data.Protocol.Serialization
{
	internal class Packet
	{
		public Packet(int sequenceNumber, ArraySegment<byte> contents)
		{
			SequenceNumber = sequenceNumber;
			Contents = contents;
		}

		public int SequenceNumber { get; }
		public ArraySegment<byte> Contents { get; }
	}
}
