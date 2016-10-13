using System;
using System.IO;
using System.Threading.Tasks;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class StreamProtocolLayer : BaseProtocolLayer
	{
		public StreamProtocolLayer(Stream stream)
		{
			m_stream = stream;
			m_buffer = new byte[16384];
		}

		public override ValueTask<ArraySegment<byte>> ReadAsync(int? count, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			if (!count.HasValue)
				throw new ArgumentException("count must be specified for StreamProtocolLayer.ReadAsync", nameof(count));

			var buffer = count.Value < m_buffer.Length ? m_buffer : new byte[count.Value];
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				return new ValueTask<ArraySegment<byte>>(DoReadBytesAsync(buffer, count.Value));
			}
			else
			{
				var bytesRead = m_stream.Read(buffer, 0, count.Value);
				return new ValueTask<ArraySegment<byte>>(new ArraySegment<byte>(buffer, 0, bytesRead));
			}
		}

		public override ValueTask<int> WriteAsync(ArraySegment<byte> data, IOBehavior ioBehavior)
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

		public override ValueTask<int> FlushAsync(IOBehavior ioBehavior)
		{
			return default(ValueTask<int>);
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

		private readonly Stream m_stream;
		private readonly byte[] m_buffer;
	}
}
