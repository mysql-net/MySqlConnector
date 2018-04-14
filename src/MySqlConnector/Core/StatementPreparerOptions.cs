using System;

namespace MySqlConnector.Core
{
	[Flags]
	internal enum StatementPreparerOptions
	{
		None = 0,
		AllowUserVariables = 1,
		OldGuids = 2,
		AllowOutputParameters = 4,
		DateTimeUtc = 8,
		DateTimeLocal = 16,
	}
}
