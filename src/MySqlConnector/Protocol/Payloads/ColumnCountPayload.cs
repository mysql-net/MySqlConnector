using MySqlConnector.Protocol.Serialization;

namespace MySqlConnector.Protocol.Payloads;

/// <summary>
///     Helper class to parse Column count packet.
///     https://mariadb.com/kb/en/result-set-packets/#column-count-packet
///     Packet contains a columnCount, and - if capability MARIADB_CLIENT_CACHE_METADATA is set - a flag to indicate if metadata follows
/// </summary>
internal readonly struct ColumnCountPayload
{
	private ColumnCountPayload(int columnCount, bool metadataFollows)
	{
		ColumnCount = columnCount;
		MetadataFollows = metadataFollows;
	}

	public int ColumnCount { get; }
	public bool MetadataFollows { get; }

	public static ColumnCountPayload Create(ReadOnlySpan<byte> span, bool supportsOptionalMetadata)
	{
		var reader = new ByteArrayReader(span);
		var columnCount = (int) reader.ReadLengthEncodedInteger();
		var metadataFollows = !supportsOptionalMetadata || reader.BytesRemaining == 0 || reader.ReadByte() == 1;
		return new ColumnCountPayload(columnCount, metadataFollows);
	}
}
