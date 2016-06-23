using System;

namespace MySql.Data.Serialization
{
	internal class PayloadData
	{
		public PayloadData(ArraySegment<byte> data)
		{
			ArraySegment = data;
		}

		public void ThrowIfError()
		{
			if (HeaderByte == ErrorPayload.Signature)
			{
				var error = ErrorPayload.Create(this);
				throw error.ToException();
			}
		}

		public ArraySegment<byte> ArraySegment { get; }
		public byte HeaderByte => ArraySegment.Array[ArraySegment.Offset];
	}
}
