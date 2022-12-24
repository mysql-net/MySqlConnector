using System.Security;
using Microsoft.Extensions.Logging;

namespace MySqlConnector.Logging;

internal static partial class LogMessages
{
	[LoggerMessage(EventIds.DataSourceCreatedWithPool, LogLevel.Information, "DataSource {DataSourceId} created with pool {PoolId}")]
	public static partial void DataSourceCreatedWithPool(ILogger logger, int dataSourceId, int poolId);

	[LoggerMessage(EventIds.DataSourceCreatedWithoutPool, LogLevel.Information, "DataSource {DataSourceId} created with no pool")]
	public static partial void DataSourceCreatedWithoutPool(ILogger logger, int dataSourceId);

	[LoggerMessage(EventIds.CreatedNonPooledSession, LogLevel.Debug, "Created new non-pooled session {SessionId}")]
	public static partial void CreatedNonPooledSession(ILogger logger, string sessionId);

	[LoggerMessage(EventIds.ResettingConnection, LogLevel.Debug, "Session {SessionId} resetting connection")]
	public static partial void ResettingConnection(ILogger logger, string sessionId);

	[LoggerMessage(EventIds.IgnoringCancellationForCommand, LogLevel.Trace, "Ignoring cancellation for closed connection or invalid command {CommandId}")]
	public static partial void IgnoringCancellationForCommand(ILogger logger, int commandId);

	[LoggerMessage(EventIds.CancelingCommand, LogLevel.Debug, "Command {CommandId} for session {SessionId} has been canceled via {CancellationSource}")]
	public static partial void CancelingCommand(ILogger logger, int commandId, string sessionId, string cancellationSource);

	[LoggerMessage(EventIds.IgnoringCancellationForClosedConnection, LogLevel.Information, "Session {SessionId} ignoring cancellation for closed connection")]
	public static partial void IgnoringCancellationForClosedConnection(ILogger logger, Exception exception, string sessionId);

	[LoggerMessage(EventIds.CancelingCommandFailed, LogLevel.Information, "Session {SessionId} cancelling command {CommandId} failed")]
	public static partial void CancelingCommandFailed(ILogger logger, Exception exception, string sessionId, int commandId);

	[LoggerMessage(EventIds.GettingCachedProcedure, LogLevel.Trace, "Session {SessionId} getting cached procedure named {ProcedureName}")]
	public static partial void GettingCachedProcedure(ILogger logger, string sessionId, string procedureName);

	[LoggerMessage(EventIds.PoolDoesNotHaveSharedProcedureCache, LogLevel.Information, "Session {SessionId} pool {PoolId} doesn't have a shared procedure cache; procedure will only be cached on this connection")]
	public static partial void PoolDoesNotHaveSharedProcedureCache(ILogger logger, string sessionId, int? poolId);

	[LoggerMessage(EventIds.CouldNotNormalizeDatabaseAndName, LogLevel.Information, "Session {SessionId} couldn't normalize the name '{ProcedureName}' in database {Database}; not caching procedure")]
	public static partial void CouldNotNormalizeDatabaseAndName(ILogger logger, string sessionId, string procedureName, string database);

	[LoggerMessage(EventIds.FailedToCacheProcedure, LogLevel.Information, "Session {SessionId} failed to cache procedure {Schema}.{Component}")]
	public static partial void FailedToCacheProcedure(ILogger logger, string sessionId, string schema, string component);

	[LoggerMessage(EventIds.CachingProcedure, LogLevel.Trace, "Session {SessionId} caching procedure {Schema}.{Component}")]
	public static partial void CachingProcedure(ILogger logger, string sessionId, string schema, string component);

	[LoggerMessage(EventIds.ProcedureCacheCount, LogLevel.Trace, "Session {SessionId} procedure cache count is {ProcedureCacheCount}")]
	public static partial void ProcedureCacheCount(ILogger logger, string sessionId, int procedureCacheCount);

	[LoggerMessage(EventIds.DidNotFindCachedProcedure, LogLevel.Information, "Session {SessionId} did not find cached procedure {Schema}.{Component}")]
	public static partial void DidNotFindCachedProcedure(ILogger logger, string sessionId, string schema, string component);

	[LoggerMessage(EventIds.ReturningCachedProcedure, LogLevel.Trace, "Session {SessionId} returning cached procedure {Schema}.{Component}")]
	public static partial void ReturningCachedProcedure(ILogger logger, string sessionId, string schema, string component);
}
