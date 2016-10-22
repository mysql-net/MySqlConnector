using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MySql.Data.Serialization;

namespace MySql.Data.Protocol.Serialization
{
	internal class CompressedPayloadHandler : IPayloadHandler
	{
		public CompressedPayloadHandler(IByteHandler byteHandler)
		{
			m_uncompressedStream = new MemoryStream();
			m_uncompressedStreamByteHandler = new StreamByteHandler(m_uncompressedStream);
			m_byteHandler = byteHandler;
			m_bufferedByteReader = new BufferedByteReader();
		}

		public void StartNewConversation()
		{
			m_compressedSequenceNumber = 0;
			m_uncompressedSequenceNumber = 0;
		}

		public IByteHandler ByteHandler
		{
			get { return m_byteHandler; }
			set { throw new NotSupportedException(); }
		}

		public ValueTask<ArraySegment<byte>> ReadPayloadAsync(ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior) =>
			ProtocolUtility.ReadPayloadAsync(m_bufferedByteReader, new CompressedByteHandler(this, protocolErrorBehavior), GetNextUncompressedSequenceNumber, default(ArraySegment<byte>), protocolErrorBehavior, ioBehavior);

		public ValueTask<int> WritePayloadAsync(ArraySegment<byte> payload, IOBehavior ioBehavior)
		{
			return ProtocolUtility.WritePayloadAsync(m_uncompressedStreamByteHandler, GetNextUncompressedSequenceNumber, payload, ioBehavior).ContinueWith(_ =>
			{
				if (m_uncompressedStream.Length == 0)
					return default(ValueTask<int>);

				ArraySegment<byte> uncompressedData;
				if (!m_uncompressedStream.TryGetBuffer(out uncompressedData))
					throw new InvalidOperationException("Couldn't get uncompressed stream buffer.");

				return CompressAndWrite(uncompressedData, ioBehavior)
					.ContinueWith(__ =>
					{
						m_uncompressedStream.SetLength(0);
						return default(ValueTask<int>);
					});
			});
		}

		private ValueTask<ArraySegment<byte>> ReadBytesAsync(int count, ProtocolErrorBehavior protocolErrorBehavior, IOBehavior ioBehavior)
		{
			if (m_remainingData.Count > 0)
			{
				int bytesToRead = Math.Min(m_remainingData.Count, count);
				var result = new ArraySegment<byte>(m_remainingData.Array, m_remainingData.Offset, bytesToRead);
				m_remainingData = new ArraySegment<byte>(m_remainingData.Array, m_remainingData.Offset + bytesToRead, m_remainingData.Count - bytesToRead);
				return new ValueTask<ArraySegment<byte>>(result);
			}

			return m_bufferedByteReader.ReadBytesAsync(m_byteHandler, 7, ioBehavior)
				.ContinueWith(headerReadBytes =>
				{
					if (headerReadBytes.Count < 7)
					{
						return protocolErrorBehavior == ProtocolErrorBehavior.Ignore ?
							default(ValueTask<ArraySegment<byte>>) :
							ValueTaskExtensions.FromException<ArraySegment<byte>>(new EndOfStreamException("Wanted to read 7 bytes but only read {0} when reading compressed packet header".FormatInvariant(headerReadBytes.Count)));
					}

					var payloadLength = (int) SerializationUtility.ReadUInt32(headerReadBytes.Array, headerReadBytes.Offset, 3);
					int packetSequenceNumber = headerReadBytes.Array[headerReadBytes.Offset + 3];
					var uncompressedLength = (int) SerializationUtility.ReadUInt32(headerReadBytes.Array, headerReadBytes.Offset + 4, 3);

					var expectedSequenceNumber = GetNextCompressedSequenceNumber() % 256;
					if (packetSequenceNumber != expectedSequenceNumber)
					{
						if (protocolErrorBehavior == ProtocolErrorBehavior.Ignore)
							return default(ValueTask<ArraySegment<byte>>);

						var exception = new InvalidOperationException("Packet received out-of-order. Expected {0}; got {1}.".FormatInvariant(expectedSequenceNumber, packetSequenceNumber));
						return ValueTaskExtensions.FromException<ArraySegment<byte>>(exception);
					}

					// MySQL protocol hack: reset the uncompressed sequence number back to the sequence number of this compressed packet
					if (!m_isContinuationPacket)
						m_uncompressedSequenceNumber = packetSequenceNumber;

					// except when uncompressed packets need to be broken up across multiple compressed packets
					m_isContinuationPacket = payloadLength == ProtocolUtility.MaxPacketSize || uncompressedLength == ProtocolUtility.MaxPacketSize;

					return m_bufferedByteReader.ReadBytesAsync(m_byteHandler, payloadLength, ioBehavior)
						.ContinueWith(payloadReadBytes =>
						{
							if (payloadReadBytes.Count < payloadLength)
							{
								return protocolErrorBehavior == ProtocolErrorBehavior.Ignore ?
									default(ValueTask<ArraySegment<byte>>) :
									ValueTaskExtensions.FromException<ArraySegment<byte>>(new EndOfStreamException("Wanted to read {0} bytes but only read {1} when reading compressed payload".FormatInvariant(payloadLength, payloadReadBytes.Count)));
							}

							if (uncompressedLength == 0)
							{
								// uncompressed
								m_remainingData = payloadReadBytes;
							}
							else
							{
								// check CMF (Compression Method and Flags) and FLG (Flags) bytes for expected values
								var cmf = payloadReadBytes.Array[payloadReadBytes.Offset];
								var flg = payloadReadBytes.Array[payloadReadBytes.Offset + 1];
								if (cmf != 0x78 || ((flg & 0x40) == 0x40) || ((cmf * 256 + flg) % 31 != 0))
								{
									// CMF = 0x78: 32K Window Size + deflate compression
									// FLG & 0x40: has preset dictionary (not supported)
									// CMF*256+FLG is a multiple of 31: header checksum
									return protocolErrorBehavior == ProtocolErrorBehavior.Ignore ?
										default(ValueTask<ArraySegment<byte>>) :
										ValueTaskExtensions.FromException<ArraySegment<byte>>(new NotSupportedException("Unsupported zlib header: {0:X2}{1:X2}".FormatInvariant(cmf, flg)));
								}

								var uncompressedData = new byte[uncompressedLength];
								using (var compressedStream = new MemoryStream(payloadReadBytes.Array, payloadReadBytes.Offset + 2, payloadReadBytes.Count - 6)) // TODO: handle zlib format correctly
								using (var decompressingStream = new DeflateStream(compressedStream, CompressionMode.Decompress))
								{
									var bytesRead = decompressingStream.Read(uncompressedData, 0, uncompressedLength);
									m_remainingData = new ArraySegment<byte>(uncompressedData, 0, bytesRead);

									// compute Adler-32 checksum
									int s1 = 1, s2 = 0;
									for (int i = 0; i < bytesRead; i++)
									{
										s1 = (s1 + uncompressedData[i]) % 65521;
										s2 = (s2 + s1) % 65521;
									}

									int adlerStartOffset = payloadReadBytes.Offset + payloadReadBytes.Count - 4;
									if (payloadReadBytes.Array[adlerStartOffset + 0] != ((s2 >> 8) & 0xFF) ||
										payloadReadBytes.Array[adlerStartOffset + 1] != (s2 & 0xFF) ||
										payloadReadBytes.Array[adlerStartOffset + 2] != ((s1 >> 8) & 0xFF) ||
										payloadReadBytes.Array[adlerStartOffset + 3] != (s1 & 0xFF))
									{
										return protocolErrorBehavior == ProtocolErrorBehavior.Ignore ?
											default(ValueTask<ArraySegment<byte>>) :
											ValueTaskExtensions.FromException<ArraySegment<byte>>(new NotSupportedException("Invalid Adler-32 checksum of uncompressed data."));
									}
								}
							}

							var result = new ArraySegment<byte>(m_remainingData.Array, m_remainingData.Offset, count);
							m_remainingData = new ArraySegment<byte>(m_remainingData.Array, m_remainingData.Offset + count, m_remainingData.Count - count);
							return new ValueTask<ArraySegment<byte>>(result);
						});
				});
		}

		private int GetNextCompressedSequenceNumber() => m_compressedSequenceNumber++;

		private int GetNextUncompressedSequenceNumber() => m_uncompressedSequenceNumber++;

		private ValueTask<int> CompressAndWrite(ArraySegment<byte> remainingUncompressedData, IOBehavior ioBehavior)
		{
			var remainingUncompressedBytes = Math.Min(remainingUncompressedData.Count, ProtocolUtility.MaxPacketSize);

			// don't compress small packets; 80 bytes is typically a good cutoff
			var compressedData = default(ArraySegment<byte>);
			if (remainingUncompressedBytes > 80)
			{
				using (var compressedStream = new MemoryStream())
				{
					// write CMF: 32K window + deflate algorithm
					compressedStream.WriteByte(0x78);

					// write FLG: maximum compression + checksum
					compressedStream.WriteByte(0xDA);

					using (var deflateStream = new DeflateStream(compressedStream, CompressionLevel.Optimal, leaveOpen: true))
						deflateStream.Write(remainingUncompressedData.Array, remainingUncompressedData.Offset, remainingUncompressedBytes);

					// compute Adler-32 checksum
					int s1 = 1, s2 = 0;
					for (int i = 0; i < remainingUncompressedBytes; i++)
					{
						s1 = (s1 + remainingUncompressedData.Array[remainingUncompressedData.Offset + i]) % 65521;
						s2 = (s2 + s1) % 65521;
					}

					// write Adler-32 checksum to stream
					compressedStream.WriteByte((byte) ((s2 >> 8) & 0xFF));
					compressedStream.WriteByte((byte) (s2 & 0xFF));
					compressedStream.WriteByte((byte) ((s1 >> 8) & 0xFF));
					compressedStream.WriteByte((byte) (s1 & 0xFF));

					if (!compressedStream.TryGetBuffer(out compressedData))
						throw new InvalidOperationException("Couldn't get compressed stream buffer.");
				}
			}

			uint uncompressedLength = (uint) remainingUncompressedBytes;
			if (compressedData.Array == null || compressedData.Count >= remainingUncompressedBytes)
			{
				// setting the length to 0 indicates sending uncompressed data
				uncompressedLength = 0;
				compressedData = new ArraySegment<byte>(remainingUncompressedData.Array, remainingUncompressedData.Offset, remainingUncompressedBytes);
			}

			var buffer = new byte[compressedData.Count + 7];
			SerializationUtility.WriteUInt32((uint) compressedData.Count, buffer, 0, 3);
			buffer[3] = (byte) GetNextCompressedSequenceNumber();
			SerializationUtility.WriteUInt32(uncompressedLength, buffer, 4, 3);
			Buffer.BlockCopy(compressedData.Array, compressedData.Offset, buffer, 7, compressedData.Count);

			remainingUncompressedData = new ArraySegment<byte>(remainingUncompressedData.Array, remainingUncompressedData.Offset + remainingUncompressedBytes, remainingUncompressedData.Count - remainingUncompressedBytes);
			return m_byteHandler.WriteBytesAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), ioBehavior)
				.ContinueWith(_ => remainingUncompressedData.Count == 0 ? default(ValueTask<int>) :
					CompressAndWrite(remainingUncompressedData, ioBehavior));
		}

		private class CompressedByteHandler : IByteHandler
		{
			public CompressedByteHandler(CompressedPayloadHandler compressedPayloadHandler, ProtocolErrorBehavior protocolErrorBehavior)
			{
				m_compressedPayloadHandler = compressedPayloadHandler;
				m_protocolErrorBehavior = protocolErrorBehavior;
			}

			public ValueTask<ArraySegment<byte>> ReadBytesAsync(int count, IOBehavior ioBehavior) =>
				m_compressedPayloadHandler.ReadBytesAsync(count, m_protocolErrorBehavior, ioBehavior);

			public ValueTask<int> WriteBytesAsync(ArraySegment<byte> data, IOBehavior ioBehavior)
			{
				throw new NotSupportedException();
			}

			readonly CompressedPayloadHandler m_compressedPayloadHandler;
			readonly ProtocolErrorBehavior m_protocolErrorBehavior;
		}

		readonly MemoryStream m_uncompressedStream;
		readonly IByteHandler m_uncompressedStreamByteHandler;
		readonly IByteHandler m_byteHandler;
		readonly BufferedByteReader m_bufferedByteReader;
		int m_compressedSequenceNumber;
		int m_uncompressedSequenceNumber;
		ArraySegment<byte> m_remainingData;
		bool m_isContinuationPacket;
	}
}
