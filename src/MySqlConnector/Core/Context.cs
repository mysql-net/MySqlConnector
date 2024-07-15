using MySqlConnector.Protocol;

namespace MySqlConnector.Core;

internal sealed class Context
{
	public Context(ProtocolCapabilities protocolCapabilities, string? database, int connectionId)
	{
		SupportsDeprecateEof = (protocolCapabilities & ProtocolCapabilities.DeprecateEof) != 0;
		SupportsCachedPreparedMetadata = (protocolCapabilities & ProtocolCapabilities.MariaDbCacheMetadata) != 0;
		SupportsQueryAttributes = (protocolCapabilities & ProtocolCapabilities.QueryAttributes) != 0;
		SupportsSessionTrack = (protocolCapabilities & ProtocolCapabilities.SessionTrack) != 0;
		ConnectionId = connectionId;
		Database = database;
		m_initialDatabase = database;
	}

	public bool SupportsDeprecateEof { get; }
	public bool SupportsQueryAttributes { get; }
	public bool SupportsSessionTrack { get; }
	public bool SupportsCachedPreparedMetadata { get; }
	public string? ClientCharset { get; set; }

	public string? Database { get; set; }
	private readonly string? m_initialDatabase;
	public bool IsInitialDatabase() => string.Equals(m_initialDatabase, Database, StringComparison.Ordinal);

	public int ConnectionId { get; set; }
}
