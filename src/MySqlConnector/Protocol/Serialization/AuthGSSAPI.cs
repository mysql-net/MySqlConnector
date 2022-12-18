#pragma warning disable SA1515 // Single-line comment should be preceded by blank line

using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using MySqlConnector.Core;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Serialization;

internal static class NegotiateStreamConstants
{
	public const int HeaderLength = 5;
	public const byte MajorVersion = 1;
	public const byte MinorVersion = 0;
	public const byte HandshakeDone = 0x14;
	public const byte HandshakeError = 0x15;
	public const byte HandshakeInProgress = 0x16;
	public const ushort MaxPayloadLength = ushort.MaxValue;
}

#pragma warning disable CA1844 // Provide memory-based overrides of async methods when subclassing 'Stream'

/// <summary>
/// Helper class to translate NegotiateStream framing for SPNEGO token
/// into MySQL protocol packets.
///
/// Serves as underlying stream for System.Net.NegotiateStream
/// to perform MariaDB's auth_gssapi_client authentication.
///
/// NegotiateStream protocol is described in e.g here
/// https://winprotocoldoc.blob.core.windows.net/productionwindowsarchives/MS-NNS/[MS-NNS].pdf
/// We only use Handshake Messages for authentication.
/// </summary>
internal class NegotiateToMySqlConverterStream : Stream
{
	private readonly MemoryStream m_writeBuffer;
	private readonly ServerSession m_serverSession;
	private readonly IOBehavior m_ioBehavior;
	private readonly CancellationToken m_cancellationToken;
	private MemoryStream m_readBuffer;
	private int m_writePayloadLength;
	private bool m_clientHandshakeDone;

	public PayloadData? MySQLProtocolPayload { get; private set; }
	public NegotiateToMySqlConverterStream(ServerSession serverSession, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		m_serverSession = serverSession;
		m_readBuffer = new();
		m_writeBuffer = new();
		m_ioBehavior = ioBehavior;
		m_cancellationToken = cancellationToken;
	}

	private static void CreateNegotiateStreamMessageHeader(byte[] buffer, int offset, byte messageId, long payloadLength)
	{
		buffer[offset] = messageId;
		buffer[offset+1] = NegotiateStreamConstants.MajorVersion;
		buffer[offset+2] = NegotiateStreamConstants.MinorVersion;
		buffer[offset+3] = (byte) (payloadLength >> 8);
		buffer[offset+4] = (byte) (payloadLength & 0xff);
	}
	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		var bytesRead = 0;

		if (m_readBuffer.Length == m_readBuffer.Position)
		{
			if (count < NegotiateStreamConstants.HeaderLength)
				throw new InvalidDataException("Unexpected call to read less then NegotiateStream header");

			if (m_clientHandshakeDone)
			{
				// NegotiateStream protocol expects server to send "handshake done"
				// empty message at the end of handshake.
				CreateNegotiateStreamMessageHeader(buffer, offset, NegotiateStreamConstants.HandshakeDone, 0);
				return NegotiateStreamConstants.HeaderLength;
			}
			// Read and cache packet from server.
			var payload = await m_serverSession.ReceiveReplyAsync(m_ioBehavior, cancellationToken).ConfigureAwait(false);
			var payloadMemory = payload.Memory;

			if (payloadMemory.Length > NegotiateStreamConstants.MaxPayloadLength)
				throw new InvalidDataException($"Payload too big for NegotiateStream - {payloadMemory.Length:d} bytes");

			// Check the first byte of the incoming packet.
			// It can be an OK packet indicating end of server processing,
			// or it can be 0x01 prefix we must strip off - 0x01 server masks special bytes, e.g 0xff, 0xfe in the payload
			// during pluggable authentication packet exchanges.
			switch (payloadMemory.Span[0])
			{
			case 0x0:
				MySQLProtocolPayload = payload;
				CreateNegotiateStreamMessageHeader(buffer, offset, NegotiateStreamConstants.HandshakeDone, 0);
				return NegotiateStreamConstants.HeaderLength;
			case 0x1:
				payloadMemory = payloadMemory[1..];
				break;
			}

			m_readBuffer = new(payloadMemory.ToArray());
			CreateNegotiateStreamMessageHeader(buffer, offset, NegotiateStreamConstants.HandshakeInProgress, m_readBuffer.Length);
			bytesRead = NegotiateStreamConstants.HeaderLength;
			offset += bytesRead;
			count -= bytesRead;
		}
		if (count > 0)
		{
			// Return cached data.
			bytesRead += m_readBuffer.Read(buffer, offset, count);
		}
		return bytesRead;
	}

	public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer, offset, count, m_cancellationToken).GetAwaiter().GetResult();

	public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (m_writePayloadLength == 0)
		{
			// The message header was not read yet.
			if (count < NegotiateStreamConstants.HeaderLength)
				// For simplicity, we expect header to be written in one go
				throw new InvalidDataException("Cannot parse NegotiateStream handshake message header");

			// Parse NegotiateStream handshake header
			var messageId = buffer[offset+0];
			var majorProtocolVersion = buffer[offset+1];
			var minorProtocolVersion = buffer[offset+2];
			var payloadSizeLow = buffer[offset+4];
			var payloadSizeHigh = buffer[offset+3];

			if (majorProtocolVersion != NegotiateStreamConstants.MajorVersion ||
				minorProtocolVersion != NegotiateStreamConstants.MinorVersion)
			{
				throw new FormatException($"Unknown version of NegotiateStream protocol {majorProtocolVersion:d}.{minorProtocolVersion:d}, expected {NegotiateStreamConstants.MajorVersion:d}.{NegotiateStreamConstants.MinorVersion:d}");
			}
			if (messageId != NegotiateStreamConstants.HandshakeDone &&
				messageId != NegotiateStreamConstants.HandshakeError &&
				messageId != NegotiateStreamConstants.HandshakeInProgress)
			{
				throw new FormatException($"Invalid NegotiateStream MessageId 0x{messageId:X2}");
			}

			m_writePayloadLength = (int) payloadSizeLow + ((int) payloadSizeHigh << 8);
			if (messageId == NegotiateStreamConstants.HandshakeDone)
				m_clientHandshakeDone = true;

			count -= NegotiateStreamConstants.HeaderLength;
		}

		if (count == 0)
			return;

		if (count + m_writeBuffer.Length > m_writePayloadLength)
			throw new InvalidDataException("Attempt to write more than a single message");

		PayloadData payload;
		if (count < m_writePayloadLength)
		{
			m_writeBuffer.Write(buffer, offset, count);
			if (m_writeBuffer.Length < m_writePayloadLength)
				// The message is only partially written
				return;

			var payloadBytes = m_writeBuffer.ToArray();
			payload = new(new ArraySegment<byte>(payloadBytes, 0, (int) m_writeBuffer.Length));
			m_writeBuffer.SetLength(0);
		}
		else
		{
			// full payload provided
			payload = new(new ArraySegment<byte>(buffer, offset, m_writePayloadLength));
		}
		await m_serverSession.SendReplyAsync(payload, m_ioBehavior, cancellationToken).ConfigureAwait(false);
		// Need to parse NegotiateStream header next time
		m_writePayloadLength = 0;
	}

	public override void Write(byte[] buffer, int offset, int count) => WriteAsync(buffer, offset, count, m_cancellationToken).GetAwaiter().GetResult();

	public override bool CanRead => true;

	public override bool CanSeek => false;

	public override bool CanWrite => true;

	public override long Length => throw new NotImplementedException();

	public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public override void Flush()
	{
	}

	public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

	public override void SetLength(long value) => throw new NotImplementedException();
}

internal static class AuthGSSAPI
{
	private static string GetServicePrincipalName(byte[] switchRequest)
	{
		var reader = new ByteArrayReader(switchRequest.AsSpan());
		return Encoding.UTF8.GetString(reader.ReadNullOrEofTerminatedByteString());
	}
	public static async Task<PayloadData> AuthenticateAsync(ConnectionSettings cs, byte[] switchRequestPayloadData,
		ServerSession session, IOBehavior ioBehavior, CancellationToken cancellationToken)
	{
		using var innerStream = new NegotiateToMySqlConverterStream(session, ioBehavior, cancellationToken);
		using var negotiateStream = new NegotiateStream(innerStream);
		var targetName = cs.ServerSPN.Length == 0 ? GetServicePrincipalName(switchRequestPayloadData) : cs.ServerSPN;
		if (ioBehavior == IOBehavior.Synchronous)
		{
			negotiateStream.AuthenticateAsClient(CredentialCache.DefaultNetworkCredentials, targetName);
		}
		else
		{
			await negotiateStream.AuthenticateAsClientAsync(CredentialCache.DefaultNetworkCredentials, targetName).ConfigureAwait(false);
		}
		if (cs.ServerSPN.Length != 0 && !negotiateStream.IsMutuallyAuthenticated)
		{
			// Negotiate used NTLM fallback, server name cannot be verified.
			throw new AuthenticationException($"GSSAPI : Unable to verify server principal name using authentication type {negotiateStream.RemoteIdentity?.AuthenticationType}");
		}
		if (innerStream.MySQLProtocolPayload is PayloadData payload)
			// return already pre-read OK packet.
			return payload;

		// Read final OK packet from server
		return await session.ReceiveReplyAsync(ioBehavior, cancellationToken).ConfigureAwait(false);
	}
}
