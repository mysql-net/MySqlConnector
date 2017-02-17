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

		public ValueTask<int> ReadBytesAsync(ArraySegment<byte> buffer, IOBehavior ioBehavior)
		{
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				return new ValueTask<int>(DoReadBytesAsync(buffer));
			}
			else
			{
				var bytesRead = m_stream.Read(buffer.Array, buffer.Offset, buffer.Count);
				return new ValueTask<int>(bytesRead);
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

		private async Task<int> DoReadBytesAsync(ArraySegment<byte> buffer)
		{
			var bytesRead = await m_stream.ReadAsync(buffer.Array, buffer.Offset, buffer.Count).ConfigureAwait(false);
			return bytesRead;
		}

		private async Task<int> DoWriteBytesAsync(ArraySegment<byte> payload)
		{
			await m_stream.WriteAsync(payload.Array, payload.Offset, payload.Count).ConfigureAwait(false);
			return 0;
		}

		readonly Stream m_stream;
	}
}
