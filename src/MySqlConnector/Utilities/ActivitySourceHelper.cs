using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace MySqlConnector.Utilities;

internal static class ActivitySourceHelper
{
	public const string DatabaseConnectionIdTagName = "db.connection_id";
	public const string DatabaseConnectionStringTagName = "db.connection_string";
	public const string DatabaseNameTagName = "db.name";
	public const string DatabaseStatementTagName = "db.statement";
	public const string DatabaseSystemTagName = "db.system";
	public const string DatabaseUserTagName = "db.user";
	public const string NetPeerIpTagName = "net.peer.ip";
	public const string NetPeerNameTagName = "net.peer.name";
	public const string NetPeerPortTagName = "net.peer.port";
	public const string NetTransportTagName = "net.transport";
	public const string StatusCodeTagName = "otel.status_code";
	public const string ThreadIdTagName = "thread.id";

	public const string DatabaseSystemValue = "mysql";
	public const string NetTransportNamedPipeValue = "pipe";
	public const string NetTransportTcpIpValue = "ip_tcp";
	public const string NetTransportUnixValue = "unix";

	public const string ExecuteActivityName = "Execute";
	public const string OpenActivityName = "Open";

	public static Activity? StartActivity(string name, IEnumerable<KeyValuePair<string, object?>>? activityTags = null)
	{
		var activity = ActivitySource.StartActivity(name, ActivityKind.Client, default(ActivityContext), activityTags);
		if (activity is { IsAllDataRequested: true })
			activity.SetTag(ActivitySourceHelper.ThreadIdTagName, Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture));
		return activity;
	}

	public static void SetSuccess(this Activity activity)
	{
#if NET6_0_OR_GREATER
		activity.SetStatus(ActivityStatusCode.Ok);
#endif
		activity.SetTag(StatusCodeTagName, "OK");
	}

	public static void SetException(this Activity activity, Exception exception)
	{
		var description = exception is MySqlException mySqlException ? mySqlException.ErrorCode.ToString() : exception.Message;
#if NET6_0_OR_GREATER
		activity.SetStatus(ActivityStatusCode.Error, description);
#endif
		activity.SetTag(StatusCodeTagName, "ERROR");
		activity.SetTag("otel.status_description", description);
		activity.AddEvent(new ActivityEvent("exception", tags: new ActivityTagsCollection
		{
			{ "exception.type", exception.GetType().FullName },
			{ "exception.message", exception.Message },
			{ "exception.stacktrace", exception.ToString() },
		}));
	}

	private static ActivitySource ActivitySource { get; } = CreateActivitySource();

	private static ActivitySource CreateActivitySource()
	{
		var assembly = typeof(ActivitySourceHelper).Assembly;
		var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
		return new("MySqlConnector", version);
	}
}
