using System;
using System.IO;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class StreamByteHandler : IByteHandler
	{
		public StreamByteHandler(Stream stream)
		{
			m_stream = stream;
		}

		public ValueTask<int> ReadBytesAsync(byte[] buffer, int offset, int count, IOBehavior ioBehavior)
		{
			return ioBehavior == IOBehavior.Asynchronous ?
				new ValueTask<int>(m_stream.ReadAsync(buffer, offset, count)) :
				new ValueTask<int>(m_stream.Read(buffer, offset, count));
		}

		public ValueTask<int> WriteBytesAsync(ArraySegment<byte> payload, IOBehavior ioBehavior)
		{
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				return new ValueTask<int>(DoWriteBytesAsync(payload));
			}
			else
			{
				m_stream.Write(payload.Array, payload.Offset, payload.Count);
				return new ValueTask<int>(0);
			}
		}

		private async Task<int> DoWriteBytesAsync(ArraySegment<byte> payload)
		{
			await m_stream.WriteAsync(payload.Array, payload.Offset, payload.Count).ConfigureAwait(false);
			return 0;
		}

		private readonly Stream m_stream;
	}
}
