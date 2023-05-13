namespace MySqlConnector.Tests;

public class SkipCITheoryAttribute : TheoryAttribute
{
	public SkipCITheoryAttribute()
	{
		if (IsCiBuild)
			Skip = "Skipped for CI";
	}

	public static bool IsCiBuild =>
		Environment.GetEnvironmentVariable("APPVEYOR") == "True" ||
		Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ||
		Environment.GetEnvironmentVariable("TRAVIS") == "true" ||
		Environment.GetEnvironmentVariable("TF_BUILD") == "True";
}
