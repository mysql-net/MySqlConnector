using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace MySqlConnector.Protocol
{
	internal readonly struct PayloadData : IDisposable
	{
		public PayloadData(byte[] data, bool isPooled = false)
		{
			Memory = data;
			m_isPooled = isPooled;
		}

		public PayloadData(ReadOnlyMemory<byte> data, bool isPooled = false)
		{
			Memory = data;
			m_isPooled = isPooled;
		}

		public ReadOnlyMemory<byte> Memory { get; }
		public ReadOnlySpan<byte> Span => Memory.Span;
		public byte HeaderByte => Span[0];

		public void Dispose()
		{
			if (m_isPooled && MemoryMarshal.TryGetArray(Memory, out var arraySegment))
				ArrayPool<byte>.Shared.Return(arraySegment.Array!);
		}

		readonly bool m_isPooled;
	}
}
