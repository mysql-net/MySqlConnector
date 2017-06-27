using System;

namespace SideBySide
{
	[Flags]
	public enum ServerFeatures
	{
		None = 0,
		Json = 1,
		StoredProcedures = 2,
	}
}
