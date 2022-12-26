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

	[LoggerMessage(EventIds.CreatedNewSession, LogLevel.Trace, "Session {SessionId} created new session")]
	public static partial void CreatedNewSession(ILogger logger, string sessionId);

	[LoggerMessage(EventIds.WaitingForAvailableSession, LogLevel.Trace, "Pool {PoolId} waiting for an available session")]
	public static partial void WaitingForAvailableSession(ILogger logger, int poolId);

	[LoggerMessage(EventIds.CreatingNewConnectionPool, LogLevel.Information, "Pool {PoolId} creating new connection pool for {ConnectionString}")]
	public static partial void CreatingNewConnectionPool(ILogger logger, int poolId, string connectionString);

	[LoggerMessage(EventIds.ScanningForLeakedSessions, LogLevel.Debug, "Pool {PoolId} is empty; scanning for any leaked sessions")]
	public static partial void ScanningForLeakedSessions(ILogger logger, int poolId);

	[LoggerMessage(EventIds.FoundExistingSession, LogLevel.Trace, "Pool {PoolId} found an existing session; checking it for validity")]
	public static partial void FoundExistingSession(ILogger logger, int poolId);

	[LoggerMessage(EventIds.DiscardingSessionDueToWrongGeneration, LogLevel.Trace, "Pool {PoolId} discarding session due to wrong generation")]
	public static partial void DiscardingSessionDueToWrongGeneration(ILogger logger, int poolId);

	[LoggerMessage(EventIds.SessionIsUnusable, LogLevel.Information, "Pool {PoolId} session {SessionId} is unusable; destroying it")]
	public static partial void SessionIsUnusable(ILogger logger, int poolId, string sessionId);

	[LoggerMessage(EventIds.ReturningPooledSession, LogLevel.Trace, "Pool {PoolId} returning pooled session {SessionId} to caller; {LeasedSessionsCount} leased sessions")]
	public static partial void ReturningPooledSession(ILogger logger, int poolId, string sessionId, int leasedSessionsCount);

	[LoggerMessage(EventIds.ReturningNewSession, LogLevel.Trace, "Pool {PoolId} returning new session {SessionId} to caller; {LeasedSessionsCount} leased sessions")]
	public static partial void ReturningNewSession(ILogger logger, int poolId, string sessionId, int leasedSessionsCount);

	[LoggerMessage(EventIds.DisposingCreatedSessionDueToException, LogLevel.Debug, "Pool {PoolId} disposing created session {SessionId} due to exception: {ExceptionMessage}")]
	public static partial void DisposingCreatedSessionDueToException(ILogger logger, Exception exception, int poolId, string sessionId, string exceptionMessage);

	[LoggerMessage(EventIds.UnexpectedErrorInGetSessionAsync, LogLevel.Warning, "Pool {PoolId} unexpected error in GetSessionAsync: {ExceptionMessage}")]
	public static partial void UnexpectedErrorInGetSessionAsync(ILogger logger, Exception exception, int poolId, string exceptionMessage);

	[LoggerMessage(EventIds.ReceivingSessionBack, LogLevel.Trace, "Pool {PoolId} receiving session {SessionId} back")]
	public static partial void ReceivingSessionBack(ILogger logger, int poolId, string sessionId);

	[LoggerMessage(EventIds.ReceivedInvalidSession, LogLevel.Information, "Pool {PoolId} received invalid session {SessionId}; destroying it")]
	public static partial void ReceivedInvalidSession(ILogger logger, int poolId, string sessionId);

	[LoggerMessage(EventIds.ReceivedExpiredSession, LogLevel.Debug, "Pool {PoolId} received expired session {SessionId}; destroying it")]
	public static partial void ReceivedExpiredSession(ILogger logger, int poolId, string sessionId);

	[LoggerMessage(EventIds.ClearingConnectionPool, LogLevel.Information, "Pool {PoolId} clearing connection pool")]
	public static partial void ClearingConnectionPool(ILogger logger, int poolId);

	[LoggerMessage(EventIds.DisposingConnectionPool, LogLevel.Debug, "Pool {PoolId} disposing connection pool")]
	public static partial void DisposingConnectionPool(ILogger logger, int poolId);

	[LoggerMessage(EventIds.RecoveredNoSessions, LogLevel.Trace, "Pool {PoolId} recovered no sessions")]
	public static partial void RecoveredNoSessions(ILogger logger, int poolId);

	[LoggerMessage(EventIds.RecoveredSessionCount, LogLevel.Warning, "Pool {PoolId} recovered {SessionCount} sessions")]
	public static partial void RecoveredSessionCount(ILogger logger, int poolId, int sessionCount);

	[LoggerMessage(EventIds.FoundSessionToCleanUp, LogLevel.Debug, "Pool {PoolId} found session {SessionId} to clean up")]
	public static partial void FoundSessionToCleanUp(ILogger logger, int poolId, string sessionId);

	[LoggerMessage(EventIds.HasServerRedirectionHeader, LogLevel.Trace, "Session {SessionId} has server redirection header {Header}")]
	public static partial void HasServerRedirectionHeader(ILogger logger, string sessionId, string header);

	[LoggerMessage(EventIds.ServerRedirectionIsDisabled, LogLevel.Trace, "Pool {PoolId} server redirection is disabled; ignoring redirection")]
	public static partial void ServerRedirectionIsDisabled(ILogger logger, int poolId);

	[LoggerMessage(EventIds.OpeningNewConnection, LogLevel.Debug, "Pool {PoolId} opening new connection to {Host}:{Port} as {User}")]
	public static partial void OpeningNewConnection(ILogger logger, int poolId, string host, int port, string user);

	[LoggerMessage(EventIds.FailedToConnectRedirectedSession, LogLevel.Information, "Pool {PoolId} failed to connect redirected session {SessionId}")]
	public static partial void FailedToConnectRedirectedSession(ILogger logger, Exception ex, int poolId, string sessionId);

	[LoggerMessage(EventIds.ClosingSessionToUseRedirectedSession, LogLevel.Trace, "Pool {PoolId} closing session {SessionId} to use redirected session {RedirectedSessionId} instead")]
	public static partial void ClosingSessionToUseRedirectedSession(ILogger logger, int poolId, string sessionId, string redirectedSessionId);

	[LoggerMessage(EventIds.SessionAlreadyConnectedToServer, LogLevel.Trace, "Session {SessionId} is already connected to this server; ignoring redirection")]
	public static partial void SessionAlreadyConnectedToServer(ILogger logger, string sessionId);

	[LoggerMessage(EventIds.RequiresServerRedirection, LogLevel.Error, "Pool {PoolId} requires server redirection but server doesn't support it")]
	public static partial void RequiresServerRedirection(ILogger logger, int poolId);

	[LoggerMessage(EventIds.CreatedPoolWillNotBeUsed, LogLevel.Debug, "Pool {PoolId} was created but will not be used (due to race)")]
	public static partial void CreatedPoolWillNotBeUsed(ILogger logger, int poolId);

	[LoggerMessage(EventIds.ReapingConnectionPool, LogLevel.Trace, "Pool {PoolId} reaping connection pool")]
	public static partial void ReapingConnectionPool(ILogger logger, int poolId);

	[LoggerMessage(EventIds.CheckingForDnsChanges, LogLevel.Trace, "Pool {PoolId} checking for DNS changes")]
	public static partial void CheckingForDnsChanges(ILogger logger, int poolId);

	[LoggerMessage(EventIds.DetectedDnsChange, LogLevel.Debug, "Pool {PoolId} detected DNS change for '{HostName}': {OldAddresses} to {NewAddresses}")]
	public static partial void DetectedDnsChange(ILogger logger, int poolId, string hostName, string oldAddresses, string newAddresses);

	[LoggerMessage(EventIds.DnsCheckFailed, LogLevel.Debug, "Pool {PoolId} DNS check failed; ignoring '{HostName}': {ExceptionMessage}")]
	public static partial void DnsCheckFailed(ILogger logger, Exception exception, int poolId, string hostName, string exceptionMessage);

	[LoggerMessage(EventIds.ClearingPoolDueToDnsChanges, LogLevel.Information, "Pool {PoolId} clearing pool due to DNS changes")]
	public static partial void ClearingPoolDueToDnsChanges(ILogger logger, int poolId);
}
