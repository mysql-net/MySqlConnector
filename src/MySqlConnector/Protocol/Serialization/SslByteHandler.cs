using System;
using System.Net.Security;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
	internal sealed class SslByteHandler : IByteHandler
	{
		public SslByteHandler(SslStream sslStream)
		{
			m_sslStream = sslStream;
		}

		public ValueTask<int> ReadBytesAsync(byte[] buffer, int offset, int count, IOBehavior ioBehavior)
		{
			return ioBehavior == IOBehavior.Asynchronous ?
				new ValueTask<int>(DoReadBytesAsync(buffer, offset, count)) :
				new ValueTask<int>(m_sslStream.Read(buffer, offset, count));
		}

		public async Task<int> DoReadBytesAsync(byte[] buffer, int offset, int count)
		{
			return await m_sslStream.ReadAsync(buffer, offset, count).ConfigureAwait(false);
		}

		public ValueTask<int> WriteBytesAsync(ArraySegment<byte> payload, IOBehavior ioBehavior)
		{
			if (ioBehavior == IOBehavior.Asynchronous)
			{
				return new ValueTask<int>(DoWriteBytesAsync(payload));
			}
			else
			{
				m_sslStream.Write(payload.Array, payload.Offset, payload.Count);
				return default(ValueTask<int>);
			}
		}

		private async Task<int> DoWriteBytesAsync(ArraySegment<byte> payload)
		{
			await m_sslStream.WriteAsync(payload.Array, payload.Offset, payload.Count).ConfigureAwait(false);
			return default(int);
		}

		private readonly SslStream m_sslStream;
	}
}
