namespace MySqlConnector.Tests;

public class SkipCIFactAttribute : FactAttribute
{
	public SkipCIFactAttribute()
	{
		if (SkipCITheoryAttribute.IsCiBuild)
			Skip = "Skipped for CI";
	}
}
