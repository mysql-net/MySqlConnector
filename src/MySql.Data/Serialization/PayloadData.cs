using System;

namespace MySql.Data.Serialization
{
	internal class PayloadData
	{
		public PayloadData(ArraySegment<byte> data)
		{
			ArraySegment = data;
		}

		public ArraySegment<byte> ArraySegment { get; }
		public byte HeaderByte => ArraySegment.Array[ArraySegment.Offset];
	}
}
