#if !MYSQL_DATA
#if NET5_0_OR_GREATER
using System.Diagnostics;
using System.Globalization;

namespace SideBySide;

public class ActivityTests : IClassFixture<DatabaseFixture>
{
	public ActivityTests(DatabaseFixture database)
	{
	}

	[Fact]
	public void OpenTags()
	{
		using var parentActivity = new Activity(nameof(OpenTags));
		parentActivity.Start();

		Activity activity = null;
		using var listener = new ActivityListener
		{
			ShouldListenTo = x => x.Name == "MySqlConnector",
			Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
				options.TraceId == parentActivity.TraceId ? ActivitySamplingResult.AllData : ActivitySamplingResult.None,
			ActivityStopped = x => activity = x,
		};
		ActivitySource.AddActivityListener(listener);

		string connectionString;
		using (var connection = new MySqlConnection(AppConfig.ConnectionString))
		{
			connection.Open();
			connectionString = connection.ConnectionString;
		}
		var csb = new MySqlConnectionStringBuilder(connectionString);

		Assert.NotNull(activity);
		Assert.Equal(ActivityKind.Client, activity.Kind);
		Assert.Equal("Open", activity.OperationName);
#if NET6_0_OR_GREATER
		Assert.Equal(ActivityStatusCode.Ok, activity.Status);
#endif

		AssertTags(activity.Tags, csb);
		AssertTag(activity.Tags, "otel.status_code", "OK");
	}

	[Fact]
	public void SelectTags()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		var csb = new MySqlConnectionStringBuilder(connection.ConnectionString);

		using var parentActivity = new Activity(nameof(OpenTags));
		parentActivity.Start();

		Activity activity = null;
		using var listener = new ActivityListener
		{
			ShouldListenTo = x => x.Name == "MySqlConnector",
			Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
				options.TraceId == parentActivity.TraceId ? ActivitySamplingResult.AllData : ActivitySamplingResult.None,
			ActivityStopped = x => activity = x,
		};
		ActivitySource.AddActivityListener(listener);

		using (var command = new MySqlCommand("SELECT 1;", connection))
		{
			command.ExecuteScalar();
		}

		Assert.NotNull(activity);
		Assert.Equal(ActivityKind.Client, activity.Kind);
		Assert.Equal("Execute", activity.OperationName);
#if NET6_0_OR_GREATER
		Assert.Equal(ActivityStatusCode.Ok, activity.Status);
#endif

		AssertTags(activity.Tags, csb);
		AssertTag(activity.Tags, "db.connection_id", connection.ServerThread.ToString(CultureInfo.InvariantCulture));
		AssertTag(activity.Tags, "db.statement", "SELECT 1;");
		AssertTag(activity.Tags, "otel.status_code", "OK");
	}

	private void AssertTags(IEnumerable<KeyValuePair<string, string>> tags, MySqlConnectionStringBuilder csb)
	{
		AssertTag(tags, "db.system", "mysql");
		AssertTag(tags, "db.connection_string", csb.ConnectionString);
		AssertTag(tags, "db.user", csb.UserID);
		if (csb.Server[0] is >= 'a' and <= 'z' or >= 'A' and <= 'Z')
			AssertTag(tags, "net.peer.name", csb.Server);
		AssertTag(tags, "net.transport", "ip_tcp");
		AssertTag(tags, "db.name", csb.Database);
	}

	private void AssertTag(IEnumerable<KeyValuePair<string, string>> tags, string expectedTag, string expectedValue)
	{
		var tag = tags.SingleOrDefault(x => x.Key == expectedTag);
		if (tag.Key is null)
			Assert.True(false, $"tags did not contain '{expectedTag}'");
		Assert.Equal(expectedValue, tag.Value);
	}
}
#endif
#endif
