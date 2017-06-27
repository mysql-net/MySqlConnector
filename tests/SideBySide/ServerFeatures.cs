using System;

namespace SideBySide
{
	[Flags]
	public enum ServerFeatures
	{
		None = 0,
		Json = 1,
		StoredProcedures = 2,
		Sha256Password = 4,
		OpenSsl = 8,
	}
}
