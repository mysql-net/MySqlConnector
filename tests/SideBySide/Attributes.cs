using Xunit;

namespace SideBySide
{
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

		public SkippableFactAttribute(ConfigSettings configSettings)
			: this(ServerFeatures.None, configSettings)
		{
		}

		public SkippableFactAttribute(ServerFeatures serverFeatures, ConfigSettings configSettings)
		{
			Skip = TestUtilities.GetSkipReason(serverFeatures, configSettings);
		}

		public string Baseline
		{
			get => null;
			set
			{
#if BASELINE
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

		public string Baseline
		{
			get => null;
			set
			{
#if BASELINE
				Skip = value;
#endif
			}
		}
	}
}
