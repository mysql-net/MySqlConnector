using System.Runtime.CompilerServices;

namespace MySqlConnector.Tests;

public class SkipCITheoryAttribute : TheoryAttribute
{
	public SkipCITheoryAttribute([CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
		: base(sourceFilePath, sourceLineNumber)
	{
		if (IsCiBuild)
			Skip = "Skipped for CI";
	}

	public static bool IsCiBuild => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
}
