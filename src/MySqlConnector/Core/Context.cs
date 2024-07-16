using MySqlConnector.Protocol;

namespace MySqlConnector.Core;

internal sealed class Context
{
	public Context(ProtocolCapabilities protocolCapabilities)
	{
		SupportsDeprecateEof = (protocolCapabilities & ProtocolCapabilities.DeprecateEof) != 0;
		SupportsCachedPreparedMetadata = (protocolCapabilities & ProtocolCapabilities.MariaDbCacheMetadata) != 0;
		SupportsQueryAttributes = (protocolCapabilities & ProtocolCapabilities.QueryAttributes) != 0;
		SupportsSessionTrack = (protocolCapabilities & ProtocolCapabilities.SessionTrack) != 0;
	}

	public bool SupportsDeprecateEof { get; }
	public bool SupportsQueryAttributes { get; }
	public bool SupportsSessionTrack { get; }
	public bool SupportsCachedPreparedMetadata { get; }
}
