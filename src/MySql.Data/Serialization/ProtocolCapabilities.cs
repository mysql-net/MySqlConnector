using System;

namespace MySql.Data.Serialization
{
	[Flags]
	internal enum ProtocolCapabilities
    {
		/// <summary>
		/// Use the improved version of Old Password Authentication.
		/// </summary>
		LongPassword = 1,

		/// <summary>
		/// Send found rows instead of affected rows in EOF_Packet.
		/// </summary>
		FoundRows = 2,

		/// <summary>
		/// Longer flags in Protocol::ColumnDefinition320.
		/// </summary>
		LongFlag = 4,

		/// <summary>
		/// Database (schema) name can be specified on connect in Handshake Response Packet.
		/// </summary>
		ConnectWithDatabase = 8,

		/// <summary>
		/// Do not permit database.table.column.
		/// </summary>
		NoSchema = 0x10,

		/// <summary>
		/// Supports compression.
		/// </summary>
		Compress = 0x20,

		/// <summary>
		/// Special handling of ODBC behavior.
		/// </summary>
		Odbc = 0x40,

		/// <summary>
		/// Enables the LOCAL INFILE request of LOAD DATA|XML.
		/// </summary>
		LocalFiles = 0x80,

		/// <summary>
		/// Parser can ignore spaces before '('.
		/// </summary>
		IgnoreSpace = 0x100,

		/// <summary>
		/// Supports the 4.1 protocol.
		/// </summary>
		Protocol41 = 0x200,

		/// <summary>
		/// Supports interactive and noninteractive clients.
		/// </summary>
		Interactive = 0x400,

		/// <summary>
		/// Supports SSL.
		/// </summary>
		Ssl = 0x800,

		IgnoreSigpipe = 0x1000,

		/// <summary>
		/// Can send status flags in EOF_Packet.
		/// </summary>
		Transactions = 0x2000,

		/// <summary>
		/// Supports Authentication::Native41.
		/// </summary>
		SecureConnection = 0x8000,

		/// <summary>
		/// Can handle multiple statements per COM_QUERY and COM_STMT_PREPARE.
		/// </summary>
		MultiStatements = 0x10000,

		/// <summary>
		/// Can send multiple resultsets for COM_QUERY.
		/// </summary>
		MultiResults = 0x20000,

		/// <summary>
		/// Can send multiple resultsets for COM_STMT_EXECUTE.
		/// </summary>
		PsMultiResults = 0x40000,

		/// <summary>
		/// Sends extra data in Initial Handshake Packet and supports the pluggable authentication protocol.
		/// </summary>
		PluginAuth = 0x80000,

		/// <summary>
		/// Permits connection attributes in Protocol::HandshakeResponse41.
		/// </summary>
		ConnectAttributes = 0x100000,

		/// <summary>
		/// Understands length-encoded integer for auth response data in Protocol::HandshakeResponse41.
		/// </summary>
		PluginAuthLengthEncodedClientData = 0x00200000,

		/// <summary>
		/// Announces support for expired password extension.
		/// </summary>
		CanHandleExpiredPasswords = 0x400000,

		/// <summary>
		/// Can set SERVER_SESSION_STATE_CHANGED in the Status Flags and send session-state change data after a OK packet.
		/// </summary>
		SessionTrack = 0x800000,

		/// <summary>
		/// Can send OK after a Text Resultset.
		/// </summary>
		DeprecateEof = 0x1000000,
	}
}
