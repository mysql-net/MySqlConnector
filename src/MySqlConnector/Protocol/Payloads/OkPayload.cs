using System.Text;
using MySqlConnector.Protocol.Serialization;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Payloads;

internal sealed class OkPayload
{
	public ulong AffectedRowCount { get; }
	public ulong LastInsertId { get; }
	public ServerStatus ServerStatus { get; }
	public int WarningCount { get; }
	public string? StatusInfo { get; }
	public string? NewSchema { get; }

	public const byte Signature = 0x00;

	/* See
	 * https://dev.mysql.com/doc/internals/en/packet-OK_Packet.html
	 * https://mariadb.com/kb/en/the-mariadb-library/resultset/
	 * https://github.com/MariaDB/mariadb-connector-j/blob/5fa814ac6e1b4c9cb6d141bd221cbd5fc45c8a78/src/main/java/org/mariadb/jdbc/internal/com/read/resultset/SelectResultSet.java#L443-L444
	 */
	public static bool IsOk(ReadOnlySpan<byte> span, bool deprecateEof) =>
		span.Length > 0 &&
			(span.Length > 6 && span[0] == Signature ||
			 deprecateEof && span.Length < 0xFF_FFFF && span[0] == EofPayload.Signature);

	/// <summary>
	/// Creates an <see cref="OkPayload"/> from the given <paramref name="span"/>, or throws <see cref="FormatException"/>
	/// if the bytes do not represent a valid <see cref="OkPayload"/>.
	/// </summary>
	/// <param name="span">The bytes from which to read an OK packet.</param>
	/// <param name="deprecateEof">Whether the <see cref="ProtocolCapabilities.DeprecateEof"/> flag was set on the connection.</param>
	/// <param name="clientSessionTrack">Whether <see cref="ProtocolCapabilities.SessionTrack"/> flag was set on the connection.</param>
	/// <returns>A <see cref="OkPayload"/> with the contents of the OK packet.</returns>
	/// <exception cref="FormatException">Thrown when the bytes are not a valid OK packet.</exception>
	public static OkPayload Create(ReadOnlySpan<byte> span, bool deprecateEof, bool clientSessionTrack) =>
		Read(span, deprecateEof, clientSessionTrack, true)!;

	/// <summary>
	/// Verifies that the bytes in the given <paramref name="span"/> form a valid <see cref="OkPayload"/>, or throws
	/// <see cref="FormatException"/> if they do not.
	/// </summary>
	/// <param name="span">The bytes from which to read an OK packet.</param>
	/// <param name="deprecateEof">Whether the <see cref="ProtocolCapabilities.DeprecateEof"/> flag was set on the connection.</param>
	/// <param name="clientSessionTrack">Whether <see cref="ProtocolCapabilities.SessionTrack"/> flag was set on the connection.</param>
	/// <exception cref="FormatException">Thrown when the bytes are not a valid OK packet.</exception>
	public static void Verify(ReadOnlySpan<byte> span, bool deprecateEof, bool clientSessionTrack) =>
		Read(span, deprecateEof, clientSessionTrack, createPayload: false);

	private static OkPayload? Read(ReadOnlySpan<byte> span, bool deprecateEof, bool clientSessionTrack, bool createPayload)
	{
		var reader = new ByteArrayReader(span);
		var signature = reader.ReadByte();
		if (signature != Signature && (!deprecateEof || signature != EofPayload.Signature))
			throw new FormatException($"Expected to read 0x00 or 0xFE but got 0x{signature:X2}");
		var affectedRowCount = reader.ReadLengthEncodedInteger();
		var lastInsertId = reader.ReadLengthEncodedInteger();
		var serverStatus = (ServerStatus) reader.ReadUInt16();
		var warningCount = (int) reader.ReadUInt16();
		string? newSchema = null;
		ReadOnlySpan<byte> statusBytes;

		if (clientSessionTrack)
		{
			if (reader.BytesRemaining > 0)
			{
				statusBytes = reader.ReadLengthEncodedByteString(); // human-readable info

				if ((serverStatus & ServerStatus.SessionStateChanged) == ServerStatus.SessionStateChanged && reader.BytesRemaining > 0)
				{
					// implies ProtocolCapabilities.SessionTrack
					var sessionStateChangeDataLength = checked((int) reader.ReadLengthEncodedInteger());
					var endOffset = reader.Offset + sessionStateChangeDataLength;
					while (reader.Offset < endOffset)
					{
						var kind = (SessionTrackKind) reader.ReadByte();
						var dataLength = (int) reader.ReadLengthEncodedInteger();
						switch (kind)
						{
							case SessionTrackKind.Schema:
								newSchema = Encoding.UTF8.GetString(reader.ReadLengthEncodedByteString());
								break;

							default:
								reader.Offset += dataLength;
								break;
						}
					}
				}
			}
			else
			{
				statusBytes = default;
			}
		}
		else
		{
			// read EOF-terminated string
			statusBytes = reader.ReadByteString(reader.BytesRemaining);

			// try to detect if it was actually a length-prefixed string (up to 250 bytes); some servers send
			// a length-prefixed status string even when CLIENT_SESSION_TRACK is not specified
			if (statusBytes.Length != 0 && statusBytes[0] == statusBytes.Length - 1)
				statusBytes = statusBytes[1..];
		}

		if (createPayload)
		{
			var statusInfo = statusBytes.Length == 0 ? null : Encoding.UTF8.GetString(statusBytes);

			if (affectedRowCount == 0 && lastInsertId == 0 && warningCount == 0 && statusInfo is null && newSchema is null)
			{
				if (serverStatus == ServerStatus.AutoCommit)
					return s_autoCommitOk;
				if (serverStatus == (ServerStatus.AutoCommit | ServerStatus.SessionStateChanged))
					return s_autoCommitSessionStateChangedOk;
			}

			return new OkPayload(affectedRowCount, lastInsertId, serverStatus, warningCount, statusInfo, newSchema);
		}
		else
		{
			return null;
		}
	}

	private OkPayload(ulong affectedRowCount, ulong lastInsertId, ServerStatus serverStatus, int warningCount, string? statusInfo, string? newSchema)
	{
		AffectedRowCount = affectedRowCount;
		LastInsertId = lastInsertId;
		ServerStatus = serverStatus;
		WarningCount = warningCount;
		StatusInfo = statusInfo;
		NewSchema = newSchema;
	}

	private static readonly OkPayload s_autoCommitOk = new(0, 0, ServerStatus.AutoCommit, 0, null, null);
	private static readonly OkPayload s_autoCommitSessionStateChangedOk = new(0, 0, ServerStatus.AutoCommit | ServerStatus.SessionStateChanged, 0, null, null);
}
