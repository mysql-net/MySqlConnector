using System.Buffers.Text;
using System.Text;
using MySqlConnector.Core;
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
	public CharacterSet? NewCharacterSet { get; }
	public int? NewConnectionId { get; }

	public const byte Signature = 0x00;

	/* See
	 * https://dev.mysql.com/doc/internals/en/packet-OK_Packet.html
	 * https://mariadb.com/kb/en/the-mariadb-library/resultset/
	 * https://github.com/MariaDB/mariadb-connector-j/blob/5fa814ac6e1b4c9cb6d141bd221cbd5fc45c8a78/src/main/java/org/mariadb/jdbc/internal/com/read/resultset/SelectResultSet.java#L443-L444
	 */
	public static bool IsOk(ReadOnlySpan<byte> span, IServerCapabilities serverCapabilities) =>
		span.Length > 0 &&
			(span.Length > 6 && span[0] == Signature ||
			 serverCapabilities.SupportsDeprecateEof && span.Length < 0xFF_FFFF && span[0] == EofPayload.Signature);

	/// <summary>
	/// Creates an <see cref="OkPayload"/> from the given <paramref name="span"/>, or throws <see cref="FormatException"/>
	/// if the bytes do not represent a valid <see cref="OkPayload"/>.
	/// </summary>
	/// <param name="span">The bytes from which to read an OK packet.</param>
	/// <param name="serverCapabilities">The server capabilities.</param>
	/// <returns>A <see cref="OkPayload"/> with the contents of the OK packet.</returns>
	/// <exception cref="FormatException">Thrown when the bytes are not a valid OK packet.</exception>
	public static OkPayload Create(ReadOnlySpan<byte> span, IServerCapabilities serverCapabilities) =>
		Read(span, serverCapabilities, true)!;

	/// <summary>
	/// Verifies that the bytes in the given <paramref name="span"/> form a valid <see cref="OkPayload"/>, or throws
	/// <see cref="FormatException"/> if they do not.
	/// </summary>
	/// <param name="span">The bytes from which to read an OK packet.</param>
	/// <param name="serverCapabilities">The server capabilities.</param>
	/// <exception cref="FormatException">Thrown when the bytes are not a valid OK packet.</exception>
	public static void Verify(ReadOnlySpan<byte> span, IServerCapabilities serverCapabilities) =>
		Read(span, serverCapabilities, createPayload: false);

	private static OkPayload? Read(ReadOnlySpan<byte> span, IServerCapabilities serverCapabilities, bool createPayload)
	{
		var reader = new ByteArrayReader(span);
		var signature = reader.ReadByte();
		if (signature != Signature && (!serverCapabilities.SupportsDeprecateEof || signature != EofPayload.Signature))
			throw new FormatException($"Expected to read 0x00 or 0xFE but got 0x{signature:X2}");
		var affectedRowCount = reader.ReadLengthEncodedInteger();
		var lastInsertId = reader.ReadLengthEncodedInteger();
		var serverStatus = (ServerStatus) reader.ReadUInt16();
		var warningCount = (int) reader.ReadUInt16();
		string? newSchema = null;
		CharacterSet clientCharacterSet = default;
		CharacterSet connectionCharacterSet = default;
		CharacterSet resultsCharacterSet = default;
		int? connectionId = null;
		ReadOnlySpan<byte> statusBytes;

		if (serverCapabilities.SupportsSessionTrack)
		{
			if (reader.BytesRemaining > 0)
			{
				statusBytes = reader.ReadLengthEncodedByteString(); // human-readable info
				while (reader.BytesRemaining > 0)
				{
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

							case SessionTrackKind.SystemVariables:
								var systemVariablesEndOffset = reader.Offset + dataLength;
								do
								{
									var systemVariableName = reader.ReadLengthEncodedByteString();
									var systemVariableValueLength = reader.ReadLengthEncodedIntegerOrNull();
									var systemVariableValue = systemVariableValueLength == -1 ? default : reader.ReadByteString(systemVariableValueLength);
									if (systemVariableName.SequenceEqual("character_set_client"u8) && systemVariableValueLength != 0)
									{
										clientCharacterSet = systemVariableValue.SequenceEqual("utf8mb4"u8) ? CharacterSet.Utf8Mb4Binary :
											systemVariableValue.SequenceEqual("utf8"u8) ? CharacterSet.Utf8Mb3Binary :
											CharacterSet.None;
									}
									else if (systemVariableName.SequenceEqual("character_set_connection"u8) && systemVariableValueLength != 0)
									{
										connectionCharacterSet = systemVariableValue.SequenceEqual("utf8mb4"u8) ? CharacterSet.Utf8Mb4Binary :
											systemVariableValue.SequenceEqual("utf8"u8) ? CharacterSet.Utf8Mb3Binary :
											CharacterSet.None;
									}
									else if (systemVariableName.SequenceEqual("character_set_results"u8) && systemVariableValueLength != 0)
									{
										resultsCharacterSet = systemVariableValue.SequenceEqual("utf8mb4"u8) ? CharacterSet.Utf8Mb4Binary :
											systemVariableValue.SequenceEqual("utf8"u8) ? CharacterSet.Utf8Mb3Binary :
											CharacterSet.None;
									}
									else if (systemVariableName.SequenceEqual("connection_id"u8))
									{
										connectionId = Utf8Parser.TryParse(systemVariableValue, out int parsedConnectionId, out var bytesConsumed) && bytesConsumed == systemVariableValue.Length ? parsedConnectionId : default(int?);
									}
								} while (reader.Offset < systemVariablesEndOffset);
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

			// detect the connection character set as utf8mb4 (or utf8) if all three system variables are set to the same value
			var characterSet = clientCharacterSet == CharacterSet.Utf8Mb4Binary && connectionCharacterSet == CharacterSet.Utf8Mb4Binary && resultsCharacterSet == CharacterSet.Utf8Mb4Binary ? CharacterSet.Utf8Mb4Binary :
				clientCharacterSet == CharacterSet.Utf8Mb3Binary && connectionCharacterSet == CharacterSet.Utf8Mb3Binary && resultsCharacterSet == CharacterSet.Utf8Mb3Binary ? CharacterSet.Utf8Mb3Binary :
				CharacterSet.None;

			if (affectedRowCount == 0 && lastInsertId == 0 && warningCount == 0 && statusInfo is null && newSchema is null && clientCharacterSet is CharacterSet.None && connectionId is null)
			{
				if (serverStatus == ServerStatus.AutoCommit)
					return s_autoCommitOk;
				if (serverStatus == (ServerStatus.AutoCommit | ServerStatus.SessionStateChanged))
					return s_autoCommitSessionStateChangedOk;
			}

			return new OkPayload(affectedRowCount, lastInsertId, serverStatus, warningCount, statusInfo, newSchema, characterSet, connectionId);
		}
		else
		{
			return null;
		}
	}

	private OkPayload(ulong affectedRowCount, ulong lastInsertId, ServerStatus serverStatus, int warningCount, string? statusInfo, string? newSchema, CharacterSet newCharacterSet, int? connectionId)
	{
		AffectedRowCount = affectedRowCount;
		LastInsertId = lastInsertId;
		ServerStatus = serverStatus;
		WarningCount = warningCount;
		StatusInfo = statusInfo;
		NewSchema = newSchema;
		NewCharacterSet = newCharacterSet;
		NewConnectionId = connectionId;
	}

	private static readonly OkPayload s_autoCommitOk = new(0, 0, ServerStatus.AutoCommit, 0, default, default, default, default);
	private static readonly OkPayload s_autoCommitSessionStateChangedOk = new(0, 0, ServerStatus.AutoCommit | ServerStatus.SessionStateChanged, 0, default, default, default, default);
}
