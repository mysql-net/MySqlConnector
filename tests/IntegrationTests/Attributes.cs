using System.Runtime.CompilerServices;

namespace IntegrationTests;

public class SkippableFactAttribute : FactAttribute
{
	public SkippableFactAttribute([CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
		: this(ServerFeatures.None, ConfigSettings.None, sourceFilePath, sourceLineNumber)
	{
	}

	public SkippableFactAttribute(ServerFeatures serverFeatures, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
		: this(serverFeatures, ConfigSettings.None, sourceFilePath, sourceLineNumber)
	{
	}

	public SkippableFactAttribute(ConfigSettings configSettings, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
		: this(ServerFeatures.None, configSettings, sourceFilePath, sourceLineNumber)
	{
	}

	public SkippableFactAttribute(ServerFeatures serverFeatures, ConfigSettings configSettings, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
		: base(sourceFilePath, sourceLineNumber)
	{
		Skip = TestUtilities.GetSkipReason(serverFeatures, configSettings);
	}

	public string MySqlData
	{
		get
		{
			return null;
		}
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
	public SkippableTheoryAttribute([CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
		: this(ServerFeatures.None, ConfigSettings.None, sourceFilePath, sourceLineNumber)
	{
	}

	public SkippableTheoryAttribute(ServerFeatures serverFeatures, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
		: this(serverFeatures, ConfigSettings.None, sourceFilePath, sourceLineNumber)
	{
	}

	public SkippableTheoryAttribute(ConfigSettings configSettings, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
		: this(ServerFeatures.None, configSettings, sourceFilePath, sourceLineNumber)
	{
	}

	public SkippableTheoryAttribute(ServerFeatures serverFeatures, ConfigSettings configSettings, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
		: base(sourceFilePath, sourceLineNumber)
	{
		Skip = TestUtilities.GetSkipReason(serverFeatures, configSettings);
	}

	public string MySqlData
	{
		get
		{
			return null;
		}
		set
		{
#if MYSQL_DATA
			Skip = value;
#endif
		}
	}
}
