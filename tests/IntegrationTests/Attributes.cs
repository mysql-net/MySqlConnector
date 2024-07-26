namespace IntegrationTests;

public class SkippableFactAttribute : FactAttribute
{
	public SkippableFactAttribute()
		: this(ServerFeatures.None, ConfigSettings.None)
	{
	}

	public SkippableFactAttribute(ServerFeatures serverFeatures)
		: this(serverFeatures, ConfigSettings.None)
	{
	}

	public SkippableFactAttribute(ServerFeatures[] serverFeatureList)
		: this(serverFeatureList, ConfigSettings.None)
	{
	}

	public SkippableFactAttribute(ConfigSettings configSettings)
		: this(ServerFeatures.None, configSettings)
	{
	}

	public SkippableFactAttribute(ServerFeatures serverFeatures, ConfigSettings configSettings)
	{
		Skip = TestUtilities.GetSkipReason(serverFeatures, configSettings);
	}

	public SkippableFactAttribute(ServerFeatures[] serverFeatureList, ConfigSettings configSettings)
	{
		Skip = null;
		foreach (ServerFeatures serverFeatures in serverFeatureList)
		{
			Skip = TestUtilities.GetSkipReason(serverFeatures, configSettings);
			if (Skip is not null) break;
		}
	}

	public string MySqlData
	{
		get => null;
		set
		{
#if MYSQL_DATA
			Skip = value;
#endif
		}
	}
}

public class SkippableTheoryAttribute : TheoryAttribute
{
	public SkippableTheoryAttribute()
		: this(ServerFeatures.None, ConfigSettings.None)
	{
	}

	public SkippableTheoryAttribute(ServerFeatures serverFeatures)
		: this(serverFeatures, ConfigSettings.None)
	{
	}

	public SkippableTheoryAttribute(ConfigSettings configSettings)
		: this(ServerFeatures.None, configSettings)
	{
	}

	public SkippableTheoryAttribute(ServerFeatures serverFeatures, ConfigSettings configSettings)
	{
		Skip = TestUtilities.GetSkipReason(serverFeatures, configSettings);
	}

	public string MySqlData
	{
		get => null;
		set
		{
#if MYSQL_DATA
			Skip = value;
#endif
		}
	}
}
