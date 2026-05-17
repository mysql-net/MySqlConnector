using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Reflection;

namespace MySqlConnector.Utilities;

internal static class ActivitySourceHelper
{
	public const string DatabaseConnectionIdTagName = "db.connection_id";
	public const string DatabaseConnectionStringTagName = "db.connection_string";
	public const string DatabaseNamespaceTagNameExperimental = "db.name"; // Y
	public const string DatabaseNamespaceTagNameStable = "db.namespace"; // Y
	public const string DatabaseOperationBatchSizeTagName = "db.operation.batch.size";
	public const string DatabaseOperationNameTagName = "db.operation.name";
	public const string DatabaseQueryTextTagName = "db.query.text";
	public const string DatabaseStatementTagName = "db.statement";
	public const string DatabaseStoredProcedureNameTagName = "db.stored_procedure.name";
	public const string DatabaseSystemTagNameExperimental = "db.system"; // Y
	public const string DatabaseSystemTagNameStable = "db.system.name"; // Y
	public const string DatabaseUserTagName = "db.user";
	public const string ErrorTypeTagName = "error.type";
	public const string NetworkPeerAddressTagName = "network.peer.address";
	public const string NetworkPeerPortTagName = "network.peer.port";
	public const string NetPeerIpTagName = "net.peer.ip";
	public const string NetPeerNameTagName = "net.peer.name";
	public const string NetPeerPortTagName = "net.peer.port";
	public const string NetTransportTagName = "net.transport";
	public const string ResponseStatusCodeTagName = "db.response.status_code";
	public const string ServerAddressTagName = "server.address";
	public const string ServerPortTagName = "server.port";
	public const string ThreadIdTagName = "thread.id";

	public const string DatabaseSystemValue = "mysql";
	public const string NetTransportNamedPipeValue = "pipe";
	public const string NetTransportTcpIpValue = "ip_tcp";
	public const string NetTransportUnixValue = "unix";

	public const string ExecuteActivityName = "Execute"; // TODO
	public const string OpenActivityName = "Open"; // TODO

	public static Activity? StartActivity(string name, IEnumerable<KeyValuePair<string, object?>>? activityTags = null)
	{
		var activity = ActivitySource.StartActivity(name, ActivityKind.Client, default(ActivityContext), activityTags);
		if (activity is { IsAllDataRequested: true })
			activity.SetTag(ActivitySourceHelper.ThreadIdTagName, Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture));
		return activity;
	}

	public static void SetException(this Activity activity, Exception exception, MySqlConnectorSemanticConventionsKinds conventionsKinds)
	{
		string description;
		var errorType = exception.GetType().FullName ?? exception.GetType().Name;
		if (exception is MySqlException mySqlException)
		{
			description = mySqlException.ErrorCode.ToString();
			var errorCode = (int) mySqlException.ErrorCode;
			if (conventionsKinds.HasFlag(MySqlConnectorSemanticConventionsKinds.Experimental))
				activity.SetTag(ResponseStatusCodeTagName, errorCode);
			if (conventionsKinds.HasFlag(MySqlConnectorSemanticConventionsKinds.Stable))
			{
				errorType = errorCode.ToString(CultureInfo.InvariantCulture);
				activity.SetTag(ResponseStatusCodeTagName, errorType);
			}
		}
		else
		{
			description = exception.Message;
		}
		activity.SetTag(ErrorTypeTagName, errorType);
		activity.SetStatus(ActivityStatusCode.Error, description);
		activity.AddEvent(new ActivityEvent("db.client.operation.exception", tags: new ActivityTagsCollection
		{
			{ "exception.type", exception.GetType().FullName },
			{ "exception.message", exception.Message },
			{ "exception.stacktrace", exception.ToString() },
		}));
	}

	public static void CopyTags(IEnumerable<KeyValuePair<string, object?>> tags, Activity? activity)
	{
		if (activity is { IsAllDataRequested: true })
		{
			foreach (var tag in tags)
				activity.SetTag(tag.Key, tag.Value);
		}
	}

	public static Meter Meter { get; } = new("MySqlConnector", GetVersion());

	private static ActivitySource ActivitySource { get; } = new("MySqlConnector", GetVersion());

	private static string GetVersion() =>
		typeof(ActivitySourceHelper).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()!.Version;
}
