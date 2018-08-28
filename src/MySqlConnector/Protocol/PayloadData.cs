using System;
using System.Buffers;

namespace MySqlConnector.Protocol
{
	internal readonly struct PayloadData : IDisposable
	{
		public PayloadData(byte[] data, bool isPooled = false)
		{
			ArraySegment = new ArraySegment<byte>(data);
			m_isPooled = isPooled;
		}

		public PayloadData(ArraySegment<byte> data, bool isPooled = false)
		{
			ArraySegment = data;
			m_isPooled = isPooled;
		}

		public ArraySegment<byte> ArraySegment { get; }
		public ReadOnlySpan<byte> AsSpan() => ArraySegment.AsSpan();
		public byte HeaderByte => ArraySegment.Array[ArraySegment.Offset];

		public void Dispose()
		{
			if (m_isPooled)
				ArrayPool<byte>.Shared.Return(ArraySegment.Array);
		}

		readonly bool m_isPooled;
	}
}
