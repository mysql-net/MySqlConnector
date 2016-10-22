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
			m_buffer = new byte[16384];
		}

		public ValueTask<ArraySegment<byte>> ReadBytesAsync(int count, IOBehavior ioBehavior)
		{
			var buffer = count < m_buffer.Length ? m_buffer : new byte[count];
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				return new ValueTask<ArraySegment<byte>>(DoReadBytesAsync(buffer, count));
			}
			else
			{
				var bytesRead = m_stream.Read(buffer, 0, count);
				return new ValueTask<ArraySegment<byte>>(new ArraySegment<byte>(buffer, 0, bytesRead));
			}
		}

		public ValueTask<int> WriteBytesAsync(ArraySegment<byte> data, IOBehavior ioBehavior)
		{
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				return new ValueTask<int>(DoWriteBytesAsync(data));
			}
			else
			{
				m_stream.Write(data.Array, data.Offset, data.Count);
				return default(ValueTask<int>);
			}
		}

		private async Task<ArraySegment<byte>> DoReadBytesAsync(byte[] buffer, int count)
		{
			var bytesRead = await m_stream.ReadAsync(buffer, 0, count).ConfigureAwait(false);
			return new ArraySegment<byte>(buffer, 0, bytesRead);
		}

		private async Task<int> DoWriteBytesAsync(ArraySegment<byte> payload)
		{
			await m_stream.WriteAsync(payload.Array, payload.Offset, payload.Count).ConfigureAwait(false);
			return 0;
		}

		readonly Stream m_stream;
		readonly byte[] m_buffer;
	}
}
