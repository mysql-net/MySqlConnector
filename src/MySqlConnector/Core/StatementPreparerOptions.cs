using System;

namespace MySqlConnector.Core
{
	[Flags]
	internal enum StatementPreparerOptions
	{
		None = 0,
		AllowUserVariables = 0x1,
		AllowOutputParameters = 0x4,
		DateTimeUtc = 0x8,
		DateTimeLocal = 0x10,
		GuidFormatChar36 = 0x20,
		GuidFormatChar32 = 0x40,
		GuidFormatBinary16 = 0x60,
		GuidFormatTimeSwapBinary16 = 0x80,
		GuidFormatLittleEndianBinary16 = 0xA0,
		GuidFormatMask = 0xE0,
	}
}
