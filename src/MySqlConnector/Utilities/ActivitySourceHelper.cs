using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Reflection;

namespace MySqlConnector.Utilities;

internal static class ActivitySourceHelper
{
	public const string DatabaseConnectionIdTagName = "db.connection_id";
	public const string DatabaseNamespaceTagName = "db.namespace";
	public const string DatabaseOperationBatchSizeTagName = "db.operation.batch.size";
	public const string DatabaseOperationNameTagName = "db.operation.name";
	public const string DatabaseQueryTextTagName = "db.query.text";
	public const string DatabaseStoredProcedureNameTagName = "db.stored_procedure.name";
	public const string DatabaseSystemTagName = "db.system.name";
	public const string ErrorTypeTagName = "error.type";
	public const string NetworkPeerAddressTagName = "network.peer.address";
	public const string NetworkPeerPortTagName = "network.peer.port";
	public const string ResponseStatusCodeTagName = "db.response.status_code";
	public const string ServerAddressTagName = "server.address";
	public const string ServerPortTagName = "server.port";

	public const string DatabaseSystemValue = "mysql";
	public const string ErrorTypeTimeoutValue = "timeout";

	public const string ExecuteActivityName = "Execute";
	public const string OpenActivityName = "Open";

	public static IEnumerable<KeyValuePair<string, object?>> DefaultActivityTags { get; } = [new(DatabaseSystemTagName, DatabaseSystemValue)];

	public static Activity? StartActivity(string name, IEnumerable<KeyValuePair<string, object?>> activityTags) =>
		ActivitySource.StartActivity(name, ActivityKind.Client, default(ActivityContext), activityTags);

	public static void SetException(this Activity activity, Exception exception)
	{
		string errorType;
		string description;
		if (exception is MySqlException { Number: > 0 } mySqlException)
		{
			// a MySQL error number is used for both 'db.response.status_code' and 'error.type'
			errorType = mySqlException.Number.ToString(CultureInfo.InvariantCulture);
			activity.SetTag(ResponseStatusCodeTagName, errorType);
			description = mySqlException.ErrorCode.ToString();
		}
		else if (exception is MySqlException { ErrorCode: MySqlErrorCode.CommandTimeoutExpired })
		{
			// a client-side timeout; no 'db.response.status_code'
			errorType = ErrorTypeTimeoutValue;
			description = exception.Message;
		}
		else
		{
			errorType = exception.GetType().ToString();
			description = exception.Message;
		}
		activity.SetTag(ErrorTypeTagName, errorType);
		activity.SetStatus(ActivityStatusCode.Error, description);
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
