namespace SideBySide;

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
	BulkCopyDataTable = 0x20_0000,
}
