namespace MySqlConnector.Tests;

public class SkipCITheoryAttribute : TheoryAttribute
{
	public SkipCITheoryAttribute()
	{
		if (IsCiBuild)
			Skip = "Skipped for CI";
	}

	public static bool IsCiBuild => Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";
}
