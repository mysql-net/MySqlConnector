using System;

namespace MySqlConnector.Protocol
{
	internal struct PayloadData
	{
		public PayloadData(byte[] data) => ArraySegment = new ArraySegment<byte>(data);
		public PayloadData(ArraySegment<byte> data) => ArraySegment = data;

		public ArraySegment<byte> ArraySegment { get; }
		public byte HeaderByte => ArraySegment.Array[ArraySegment.Offset];
	}
}
