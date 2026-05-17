#if !MYSQL_DATA
#if NET5_0_OR_GREATER
using System.Diagnostics;
using System.Globalization;

namespace IntegrationTests;

[CollectionDefinition(nameof(NonParallelCollection), DisableParallelization = true)]
public class NonParallelCollection
{
}

[Collection(nameof(NonParallelCollection))]
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
		Assert.Equal(ActivityStatusCode.Unset, activity.Status);

		AssertTags(activity.Tags, csb);
	}

	[Fact]
	public void OpenTagsStableConvention()
	{
		var dataSourceBuilder = new MySqlDataSourceBuilder(AppConfig.ConnectionString)
			.ConfigureTracing(o => o.WithSemanticConventionsKinds(MySqlConnectorSemanticConventionsKinds.Stable));
		using var dataSource = dataSourceBuilder.Build();

		using var parentActivity = new Activity(nameof(OpenTagsStableConvention));
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
		using (var connection = dataSource.OpenConnection())
		{
			connectionString = connection.ConnectionString;
		}
		var csb = new MySqlConnectionStringBuilder(connectionString);

		Assert.NotNull(activity);
		Assert.Equal(ActivityKind.Client, activity.Kind);
		Assert.Equal("Open", activity.OperationName);
		Assert.Equal(ActivityStatusCode.Unset, activity.Status);

		AssertStableTags(activity, csb);
	}

	[Fact]
	public void OpenFromPoolTags()
	{
		var activities = new Activity[2];

		// create a unique pool
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.MaximumPoolSize = 7;
		var connectionString = csb.ConnectionString;

		for (var i = 0; i < activities.Length; i++)
		{
			using var parentActivity = new Activity(nameof(OpenFromPoolTags));
			parentActivity.Start();

			using var listener = new ActivityListener
			{
				ShouldListenTo = x => x.Name == "MySqlConnector",
				Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
					options.TraceId == parentActivity.TraceId ? ActivitySamplingResult.AllData : ActivitySamplingResult.None,
				ActivityStopped = x => activities[i] = x,
			};
			ActivitySource.AddActivityListener(listener);

			using (var connection = new MySqlConnection(connectionString))
				connection.Open();

			Assert.NotNull(activities[i]);
		}

		// activities should have the same connection ID
		Assert.Equal(activities[0].Tags.Single(x => x.Key == "db.connection_id").Value, activities[1].Tags.Single(x => x.Key == "db.connection_id").Value);
	}

	[Fact]
	public void OpenFailedTags()
	{
		using var parentActivity = new Activity(nameof(OpenFailedTags));
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

		var csb = new MySqlConnectionStringBuilder("Server=www.mysqlconnector.net;User Id=invaliduser;Database=invaliddb;Connection Timeout=1");
		using (var connection = new MySqlConnection(csb.ConnectionString))
			Assert.Throws<MySqlException>(() => connection.Open());

		Assert.NotNull(activity);
		Assert.Equal(ActivityKind.Client, activity.Kind);
		Assert.Equal("Open", activity.OperationName);
		Assert.Equal(ActivityStatusCode.Error, activity.Status);

		AssertTags(activity.Tags, csb);
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
		Assert.Equal(ActivityStatusCode.Unset, activity.Status);

		AssertTags(activity.Tags, csb);
		AssertTag(activity.Tags, "db.connection_id", connection.ServerThread.ToString(CultureInfo.InvariantCulture));
		AssertTag(activity.Tags, "db.statement", "SELECT 1;");
	}

	[Fact]
	public void SelectTagsStableConvention()
	{
		var dataSourceBuilder = new MySqlDataSourceBuilder(AppConfig.ConnectionString)
			.ConfigureTracing(o => o.WithSemanticConventionsKinds(MySqlConnectorSemanticConventionsKinds.Stable));
		using var dataSource = dataSourceBuilder.Build();
		using var connection = dataSource.OpenConnection();
		var csb = new MySqlConnectionStringBuilder(connection.ConnectionString);

		using var parentActivity = new Activity(nameof(SelectTagsStableConvention));
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
		Assert.Equal(ActivityStatusCode.Unset, activity.Status);

		AssertStableTags(activity, csb);
		AssertTag(activity.Tags, "db.connection_id", connection.ServerThread.ToString(CultureInfo.InvariantCulture));
		AssertTag(activity.Tags, "db.query.text", "SELECT 1;");
		AssertNoTag(activity.Tags, "db.statement");
	}

	[Fact]
	public void SelectTagsDupConvention()
	{
		var dataSourceBuilder = new MySqlDataSourceBuilder(AppConfig.ConnectionString)
			.ConfigureTracing(o => o.WithSemanticConventionsKinds(MySqlConnectorSemanticConventionsKinds.Experimental | MySqlConnectorSemanticConventionsKinds.Stable));
		using var dataSource = dataSourceBuilder.Build();
		using var connection = dataSource.OpenConnection();
		var csb = new MySqlConnectionStringBuilder(connection.ConnectionString);

		using var parentActivity = new Activity(nameof(SelectTagsDupConvention));
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
		Assert.Equal(ActivityStatusCode.Unset, activity.Status);

		AssertTags(activity.Tags, csb);
		AssertStableTags(activity, csb, assertNoLegacyTags: false);
		AssertTag(activity.Tags, "db.connection_id", connection.ServerThread.ToString(CultureInfo.InvariantCulture));
		AssertTag(activity.Tags, "db.statement", "SELECT 1;");
		AssertTag(activity.Tags, "db.query.text", "SELECT 1;");
	}

	[Fact]
	public void ErrorTagsStableConvention()
	{
		var dataSourceBuilder = new MySqlDataSourceBuilder(AppConfig.ConnectionString)
			.ConfigureTracing(o => o.WithSemanticConventionsKinds(MySqlConnectorSemanticConventionsKinds.Stable));
		using var dataSource = dataSourceBuilder.Build();
		using var connection = dataSource.OpenConnection();

		using var parentActivity = new Activity(nameof(ErrorTagsStableConvention));
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

		using var command = new MySqlCommand("SELECT * FROM table_that_does_not_exist_for_activity_tests;", connection);
		Assert.Throws<MySqlException>(() => command.ExecuteScalar());

		Assert.NotNull(activity);
		Assert.Equal(ActivityKind.Client, activity.Kind);
		Assert.Equal("Execute", activity.OperationName);
		Assert.Equal(ActivityStatusCode.Error, activity.Status);

		var statusCode = AssertHasTag(activity.Tags, "db.response.status_code");
		Assert.True(int.TryParse(statusCode, NumberStyles.None, CultureInfo.InvariantCulture, out _));
		AssertTag(activity.Tags, "error.type", statusCode);
		AssertTagObject(activity.TagObjects, "db.response.status_code", statusCode);
		AssertTagObject(activity.TagObjects, "error.type", statusCode);
	}

	[Fact]
	public void BatchTagsStableConvention()
	{
		var dataSourceBuilder = new MySqlDataSourceBuilder(AppConfig.ConnectionString)
			.ConfigureTracing(o => o.WithSemanticConventionsKinds(MySqlConnectorSemanticConventionsKinds.Stable));
		using var dataSource = dataSourceBuilder.Build();
		using var connection = dataSource.OpenConnection();

		using var parentActivity = new Activity(nameof(BatchTagsStableConvention));
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

		using var batch = new MySqlBatch(connection)
		{
			BatchCommands =
			{
				new MySqlBatchCommand("SELECT 1;"),
				new MySqlBatchCommand("SELECT 2;"),
			},
		};
		batch.ExecuteNonQuery();

		Assert.NotNull(activity);
		Assert.Equal(ActivityKind.Client, activity.Kind);
		Assert.Equal("Execute", activity.OperationName);
		Assert.Equal(ActivityStatusCode.Unset, activity.Status);

		AssertTag(activity.Tags, "db.operation.name", "BATCH");
		AssertTagObject(activity.TagObjects, "db.operation.batch.size", 2);
		AssertNoTag(activity.Tags, "db.query.text");
		AssertNoTag(activity.Tags, "db.statement");
	}

	[Fact]
	public void StoredProcedureTagsStableConvention()
	{
		const string procedureName = "activity_tags_test";

		var dataSourceBuilder = new MySqlDataSourceBuilder(AppConfig.ConnectionString)
			.ConfigureTracing(o => o.WithSemanticConventionsKinds(MySqlConnectorSemanticConventionsKinds.Stable));
		using var dataSource = dataSourceBuilder.Build();
		using var connection = dataSource.OpenConnection();
		using (var command = new MySqlCommand($"DROP PROCEDURE IF EXISTS {procedureName};", connection))
			command.ExecuteNonQuery();
		using (var command = new MySqlCommand($"CREATE PROCEDURE {procedureName}() SELECT 1;", connection))
			command.ExecuteNonQuery();

		try
		{
			using var parentActivity = new Activity(nameof(StoredProcedureTagsStableConvention));
			parentActivity.Start();

			Activity activity = null;
			using (var listener = new ActivityListener
			{
				ShouldListenTo = x => x.Name == "MySqlConnector",
				Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
					options.TraceId == parentActivity.TraceId ? ActivitySamplingResult.AllData : ActivitySamplingResult.None,
				ActivityStopped = x => activity = x,
			})
			{
				ActivitySource.AddActivityListener(listener);

				using var command = new MySqlCommand(procedureName, connection)
				{
					CommandType = CommandType.StoredProcedure,
				};
				Assert.Equal(1, Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture));
			}

			Assert.NotNull(activity);
			Assert.Equal(ActivityKind.Client, activity.Kind);
			Assert.Equal("Execute", activity.OperationName);
			Assert.Equal(ActivityStatusCode.Unset, activity.Status);

			AssertTag(activity.Tags, "db.operation.name", "CALL");
			AssertTag(activity.Tags, "db.stored_procedure.name", procedureName);
			AssertNoTag(activity.Tags, "db.query.text");
			AssertNoTag(activity.Tags, "db.statement");
		}
		finally
		{
			using var command = new MySqlCommand($"DROP PROCEDURE IF EXISTS {procedureName};", connection);
			command.ExecuteNonQuery();
		}
	}

	[SkippableTheory(ServerFeatures.QueryAttributes)]
	[InlineData(false)]
	[InlineData(true)]
	public void ExecuteTraceparentQueryAttribute(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		using var parentActivity = new Activity(nameof(ExecuteTraceparentQueryAttribute));
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

		using var command = new MySqlCommand("SELECT mysql_query_attribute_string('traceparent');", connection);
		if (prepare)
			command.Prepare();

		var traceparent = command.ExecuteScalar();

		Assert.NotNull(activity);
		Assert.Equal(activity.Id, traceparent);
	}

	[SkippableTheory(ServerFeatures.QueryAttributes)]
	[InlineData(false)]
	[InlineData(true)]
	public void ExecuteTraceparentQueryAttributeBatch(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		using var parentActivity = new Activity(nameof(ExecuteTraceparentQueryAttributeBatch));
		parentActivity.Start();

		var activities = new List<Activity>();
		using var listener = new ActivityListener
		{
			ShouldListenTo = x => x.Name == "MySqlConnector",
			Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
				options.TraceId == parentActivity.TraceId ? ActivitySamplingResult.AllData : ActivitySamplingResult.None,
			ActivityStopped = activities.Add,
		};
		ActivitySource.AddActivityListener(listener);

		using var batch = new MySqlBatch(connection)
		{
			BatchCommands =
			{
				new MySqlBatchCommand("SELECT mysql_query_attribute_string('traceparent');"),
				new MySqlBatchCommand("SELECT mysql_query_attribute_string('traceparent');"),
			},
		};
		if (prepare)
			batch.Prepare();

		var traceparents = new List<string>();
		using (var reader = batch.ExecuteReader())
		{
			do
			{
				Assert.True(reader.Read());
				traceparents.Add(reader.GetString(0));
				Assert.False(reader.Read());
			} while (reader.NextResult());
		}

		var activity = Assert.Single(activities);
		Assert.Equal(ActivityKind.Client, activity.Kind);
		Assert.Equal("Execute", activity.OperationName);
		Assert.All(traceparents, x => Assert.Equal(activity.Id, x));
	}

	[SkippableTheory(ServerFeatures.QueryAttributes)]
	[InlineData(false)]
	[InlineData(true)]
	public void ExecuteTraceContextQueryAttributes(bool prepare)
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		using var parentActivity = new Activity(nameof(ExecuteTraceContextQueryAttributes));
		parentActivity.TraceStateString = "test=value";
		parentActivity.Start();

		using var listener = new ActivityListener
		{
			ShouldListenTo = x => x.Name == "MySqlConnector",
			Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData,
		};
		ActivitySource.AddActivityListener(listener);

		using var command = new MySqlCommand(
			"SELECT mysql_query_attribute_string('traceparent') AS traceparent, mysql_query_attribute_string('tracestate') AS tracestate;",
			connection);
		if (prepare)
			command.Prepare();

		using var reader = command.ExecuteReader();
		Assert.True(reader.Read());
		var traceparent = reader.GetString(reader.GetOrdinal("traceparent"));
		var tracestate = reader.GetString(reader.GetOrdinal("tracestate"));
		Assert.False(reader.Read());

		Assert.Matches($"^00-{parentActivity.TraceId.ToString()}-[0-9a-f]{{16}}-[0-9a-f]{{2}}$", traceparent);
		Assert.Equal(parentActivity.TraceStateString, tracestate);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ReadResultSetHeaderEvent(bool enableEvent)
	{
		var dataSourceBuilder = new MySqlDataSourceBuilder(AppConfig.ConnectionString)
			.ConfigureTracing(o => o.EnableResultSetHeaderEvent(enableEvent));
		using var dataSource = dataSourceBuilder.Build();
		using var connection = dataSource.OpenConnection();

		using var parentActivity = new Activity(nameof(ReadResultSetHeaderEvent));
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
		if (enableEvent)
		{
			var activityEvent = Assert.Single(activity.Events);
			Assert.Equal("read-result-set-header", activityEvent.Name);
		}
		else
		{
			Assert.Empty(activity.Events);
		}
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

	private void AssertStableTags(Activity activity, MySqlConnectionStringBuilder csb, bool assertNoLegacyTags = true)
	{
		AssertTag(activity.Tags, "db.system.name", "mysql");
		AssertTag(activity.Tags, "db.namespace", csb.Database);
		AssertTag(activity.Tags, "server.address", csb.Server);
		AssertHasTag(activity.Tags, "network.peer.address");
		AssertTagObject(activity.TagObjects, "network.peer.port", csb.Port);
		if (csb.Port == 3306)
		{
			AssertNoTag(activity.Tags, "server.port");
		}
		else
		{
			AssertTagObject(activity.TagObjects, "server.port", csb.Port);
		}

		if (assertNoLegacyTags)
		{
			AssertNoTag(activity.Tags, "db.connection_string");
			AssertNoTag(activity.Tags, "db.user");
			AssertNoTag(activity.Tags, "db.system");
			AssertNoTag(activity.Tags, "db.name");
			AssertNoTag(activity.Tags, "thread.id");
			AssertNoTag(activity.Tags, "net.transport");
			AssertNoTag(activity.Tags, "net.peer.name");
			AssertNoTag(activity.Tags, "net.peer.ip");
			AssertNoTag(activity.Tags, "net.peer.port");
		}
	}

	private string AssertHasTag(IEnumerable<KeyValuePair<string, string>> tags, string expectedTag)
	{
		var tag = tags.SingleOrDefault(x => x.Key == expectedTag);
		if (tag.Key is null)
			Assert.Fail($"tags did not contain '{expectedTag}'");
		return tag.Value;
	}

	private void AssertTag(IEnumerable<KeyValuePair<string, string>> tags, string expectedTag, string expectedValue)
	{
		Assert.Equal(expectedValue, AssertHasTag(tags, expectedTag));
	}

	private void AssertNoTag(IEnumerable<KeyValuePair<string, string>> tags, string tagName)
	{
		var tag = tags.SingleOrDefault(x => x.Key == tagName);
		if (tag.Key is not null)
			Assert.Fail($"tags unexpectedly contained '{tagName}' = '{tag.Value}'");
	}

	private void AssertTagObject(IEnumerable<KeyValuePair<string, object>> tags, string expectedTag, object expectedValue)
	{
		var tag = tags.SingleOrDefault(x => x.Key == expectedTag);
		if (tag.Key is null)
			Assert.Fail($"tag objects did not contain '{expectedTag}'");
		if (IsNumeric(tag.Value) && IsNumeric(expectedValue))
		{
			Assert.Equal(Convert.ToDecimal(expectedValue, CultureInfo.InvariantCulture), Convert.ToDecimal(tag.Value, CultureInfo.InvariantCulture));
			return;
		}
		Assert.Equal(expectedValue, tag.Value);

		static bool IsNumeric(object value) =>
			value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
	}
}
#endif
#endif
