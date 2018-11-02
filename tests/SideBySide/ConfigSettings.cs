using System;

namespace SideBySide
{
	[Flags]
	public enum ConfigSettings
	{
		None = 0,
		RequiresSsl = 0x1,
		TrustedHost = 0x2,
		UntrustedHost = 0x4,
		PasswordlessUser = 0x8,
		CsvFile = 0x10,
		LocalCsvFile = 0x20,
		TsvFile = 0x40,
		LocalTsvFile = 0x80,
		TcpConnection = 0x200,
		SecondaryDatabase = 0x400,
		KnownClientCertificate = 0x800,
		GSSAPIUser = 0x1000,
		HasKerberos = 0x2000
	}
}
