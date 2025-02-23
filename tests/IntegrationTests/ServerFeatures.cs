namespace IntegrationTests;

[Flags]
public enum ServerFeatures
{
	None = 0,
	Json = 0x1,
	StoredProcedures = 0x2,
	Sha256Password = 0x4,
	RsaEncryption = 0x8,
	LargePackets = 0x10,
	CachingSha2Password = 0x20,
	SessionTrack = 0x40,
	Timeout = 0x80,
	ErrorCodes = 0x100,
	KnownCertificateAuthority = 0x200,
	Tls11 = 0x400,
	Tls12 = 0x800,
	RoundDateTime = 0x1000,
	UuidToBin = 0x2000,
	Ed25519 = 0x4000,
	UnixDomainSocket = 0x8000,
	Tls13 = 0x1_0000,
	ResetConnection = 0x2_0000,
	ZeroDateTime = 0x4_0000,
	QueryAttributes = 0x8_0000,
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
	/// Server supports the <c>VECTOR</c> data type.
	/// </summary>
	Vector = 0x200_0000,
}
