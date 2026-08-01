namespace MySqlConnector.Tests;

public class SingleCommandPayloadCreatorTests
{
	[Fact]
	public void NoAttributesNoActivity()
	{
		var (count, kinds) = SingleCommandPayloadCreator.GetAttributeCountAndKinds(null, null);
		Assert.Equal(0, count);
		Assert.Equal(TelemetryAttributeKind.None, kinds);
	}

	[Fact]
	public void NoAttributesActivity()
	{
		using var activity = new Activity("test");
#if !NET5_0_OR_GREATER
		activity.SetIdFormat(ActivityIdFormat.W3C);
#endif
		activity.Start();
		var (count, kinds) = SingleCommandPayloadCreator.GetAttributeCountAndKinds(null, activity);
		Assert.Equal(1, count);
		Assert.Equal(TelemetryAttributeKind.TraceParent, kinds);
	}

	[Fact]
	public void AttributesNoActivity()
	{
		var attributes = new MySqlAttributeCollection()
		{
			new("test", "value"),
		};
		var (count, kinds) = SingleCommandPayloadCreator.GetAttributeCountAndKinds(attributes, null);
		Assert.Equal(1, count);
		Assert.Equal(TelemetryAttributeKind.None, kinds);
	}

	[Fact]
	public void AttributesActivity()
	{
		var attributes = new MySqlAttributeCollection()
		{
			new("test", "value"),
		};
		using var activity = new Activity("test");
#if !NET5_0_OR_GREATER
		activity.SetIdFormat(ActivityIdFormat.W3C);
#endif
		activity.Start();
		var (count, kinds) = SingleCommandPayloadCreator.GetAttributeCountAndKinds(attributes, activity);
		Assert.Equal(2, count);
		Assert.Equal(TelemetryAttributeKind.TraceParent, kinds);
	}

	[Fact]
	public void AttributesActivityTraceState()
	{
		var attributes = new MySqlAttributeCollection()
		{
			new("test", "value"),
		};
		using var activity = new Activity("test");
#if !NET5_0_OR_GREATER
		activity.SetIdFormat(ActivityIdFormat.W3C);
#endif
		activity.Start();
		activity.TraceStateString = "key=value";
		var (count, kinds) = SingleCommandPayloadCreator.GetAttributeCountAndKinds(attributes, activity);
		Assert.Equal(3, count);
		Assert.Equal(TelemetryAttributeKind.TraceParent | TelemetryAttributeKind.TraceState, kinds);
	}

	[Fact]
	public void AttributesDuplicateActivity()
	{
		var attributes = new MySqlAttributeCollection()
		{
			new("traceparent", "duplicate"),
			new("tracestate", "duplicate"),
		};
		using var activity = new Activity("test");
#if !NET5_0_OR_GREATER
		activity.SetIdFormat(ActivityIdFormat.W3C);
#endif
		activity.Start();
		activity.TraceStateString = "key=value";
		var (count, kinds) = SingleCommandPayloadCreator.GetAttributeCountAndKinds(attributes, activity);
		Assert.Equal(2, count);
		Assert.Equal(TelemetryAttributeKind.None, kinds);
	}
}
