namespace IntegrationTests;

[Flags]
public enum ServerFeatures
{
	None = 0,

	/// <summary>
	/// Server supports the <c>JSON</c> data type (MySQL 5.7 and later).
	/// </summary>
	Json = 0x1,

	/// <summary>
	/// Server supports creating and executing stored procedures.
	/// </summary>
	StoredProcedures = 0x2,

	/// <summary>
	/// A user named <c>sha256user</c> exists on your server and uses the <c>sha256_password</c> auth plugin.
	/// </summary>
	Sha256Password = 0x4,

	/// <summary>
	/// Server supports RSA public key encryption (for <c>sha256_password</c> and <c>caching_sha2_password</c>).
	/// </summary>
	RsaEncryption = 0x8,

	/// <summary>
	/// Server supports large packets (over 4MB).
	/// </summary>
	LargePackets = 0x10,

	/// <summary>
	/// A user named <c>caching-sha2-user</c> exists on your server and uses the <c>caching_sha2_password</c> auth plugin.
	/// </summary>
	CachingSha2Password = 0x20,

	/// <summary>
	/// Server supports <c>CLIENT_SESSION_TRACK</c> capability (MySQL 5.7 and later)
	/// </summary>
	SessionTrack = 0x40,

	/// <summary>
	/// Server can cancel queries promptly (so timed tests don't time out).
	/// </summary>
	Timeout = 0x80,

	/// <summary>
	/// Server returns error codes in error packet (some MySQL proxies do not).
	/// </summary>
	[Obsolete]
	ErrorCodes = 0x100,

	/// <summary>
	/// The certificates used by the database server are trusted by the client.
	/// </summary>
	KnownCertificateAuthority = 0x200,

	/// <summary>
	/// Server supports TLS 1.1.
	/// </summary>
	Tls11 = 0x400,

	/// <summary>
	/// Server supports TLS 1.2.
	/// </summary>
	Tls12 = 0x800,

	/// <summary>
	/// Server rounds <c>datetime</c> values to the specified precision (not implemented in MariaDB).
	/// </summary>
	RoundDateTime = 0x1000,

	/// <summary>
	/// Server supports <c>UUID_TO_BIN</c> (MySQL 8.0 and later).
	/// </summary>
	UuidToBin = 0x2000,

	/// <summary>
	/// A user named <c>ed25519user</c> exists on your server and uses the <c>client_ed25519</c> auth plugin.
	/// </summary>
	Ed25519 = 0x4000,

	/// <summary>
	/// Server is accessible via a Unix domain socket.
	/// </summary>
	UnixDomainSocket = 0x8000,

	/// <summary>
	/// Server supports TLS 1.3.
	/// </summary>
	Tls13 = 0x1_0000,

	/// <summary>
	/// Server supports the <c>COM_RESET_CONNECTION</c> command.
	/// </summary>
	ResetConnection = 0x2_0000,

	/// <summary>
	/// Server allows <c>0000-00-00</c> to be stored as <c>DATE</c> or <c>DATETIME</c>; i.e., it does _not_ have the <c>NO_ZERO_DATE</c> <a href="https://dev.mysql.com/doc/refman/8.4/en/sql-mode.html#sqlmode_no_zero_date">SQL mode</a> or strict mode enabled.
	/// </summary>
	ZeroDateTime = 0x4_0000,

	/// <summary>
	/// Server supports query attributes (MySQL 8.4 and later).
	/// </summary>
	QueryAttributes = 0x8_0000,

	/// <summary>
	/// Server supports <c>set global general_log</c>.
	/// </summary>
	GlobalLog = 0x10_0000,

	/// <summary>
	/// The MySQL server can start streaming rows back as soon as they are available, as opposed to buffering the entire result set in memory.
	/// </summary>
	StreamingResults = 0x20_0000,

	/// <summary>
	/// A "SLEEP" command produces a result set when it is cancelled, not an error payload.
	/// </summary>
	CancelSleepSuccessfully = 0x40_0000,

	/// <summary>
	/// Server permits redirection (sent as a server variable in first OK packet).
	/// </summary>
	Redirection = 0x80_0000,

	/// <summary>
	/// Server provides hash of TLS certificate in first OK packet.
	/// </summary>
	TlsFingerprintValidation = 0x100_0000,

	/// <summary>
	/// Server supports the <c>parsec</c> authentication plugin.
	/// </summary>
	ParsecAuthentication = 0x200_0000,

	/// <summary>
	/// Server supports the <c>VECTOR</c> SQL type.
	/// </summary>
	Vector = 0x400_0000,

	/// <summary>
	/// Server has a dedicated type on the wire for <c>VECTOR</c>.
	/// </summary>
	VectorType = 0x800_0000,

	/// <summary>
	/// Server supports <c>INSERT ... RETURNING</c> (MariaDB 10.5 and later).
	/// </summary>
	InsertReturning = 0x1000_0000,
}
