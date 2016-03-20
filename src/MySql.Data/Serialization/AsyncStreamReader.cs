using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.Serialization
{
	internal sealed class AsyncStreamReader
	{
		public static async Task<AsyncStreamReader> CreateAsync(Stream stream, CancellationToken cancellationToken)
		{
			var buffer = new byte[256];
			int bytesRead = await stream.ReadAsync(buffer, 0, 4, cancellationToken);
			if (bytesRead != 4)
				throw new EndOfStreamException();

			int payloadLength = (int) ReadUInt32(buffer, 0, 3);
			int sequenceId = buffer[4];

			return new AsyncStreamReader(stream, cancellationToken, sequenceId, payloadLength, buffer);
		}

		public int PayloadLength { get; }

		private AsyncStreamReader(Stream stream, CancellationToken cancellationToken, int sequenceId, int payloadLength, byte[] buffer)
		{
			m_stream = stream;
			m_cancellationToken = cancellationToken;
			SequenceId = sequenceId;
			m_buffer = buffer;
			PayloadLength = payloadLength;
		}

		public int SequenceId { get; }

		public async Task<ByteArrayReader> ReadPayloadAsync()
		{
			if (PayloadLength > m_buffer.Length)
				throw new InvalidOperationException();
			await m_stream.ReadAsync(m_buffer, 0, PayloadLength, m_cancellationToken);
			return new ByteArrayReader(m_buffer, 0, PayloadLength);
		}

		public async Task<byte> ReadByteAsync()
		{
			await ReadExactlyAsync(1);
			return m_buffer[0];
		}

		public async Task ReadByteAsync(byte expected)
		{
			await ReadExactlyAsync(1);
			if (m_buffer[0] != expected)
				throw new FormatException($"Expected to read 0x{expected:X2} but got 0x{m_buffer[0]:X2}");
		}

		private async Task ReadExactlyAsync(int count)
		{
			if (count >= m_buffer.Length)
				throw new ArgumentOutOfRangeException(nameof(count));
			int bytesRead = await m_stream.ReadAsync(m_buffer, 0, 1, m_cancellationToken);
			if (bytesRead == 0)
				throw new EndOfStreamException();
		}

		private static uint ReadUInt32(byte[] buffer, int offset, int count)
		{
			uint value = 0;
			uint multiplier = 1;
			for (int i = 0; i < count; i++)
			{
				value += buffer[offset + i] * multiplier;
				multiplier *= 256;
			}
			return value;
		}

		readonly Stream m_stream;
		readonly CancellationToken m_cancellationToken;
		readonly byte[] m_buffer;
	}
}
