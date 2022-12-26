namespace MySqlConnector.Logging;

internal static class EventIds
{
	public const int DataSourceCreatedWithPool = 1000;
	public const int DataSourceCreatedWithoutPool = 1001;

	public const int CreatedNonPooledSession = 2000;
	public const int ResettingConnection = 2001;
	public const int CreatedNewSession = 2002;

	public const int IgnoringCancellationForCommand = 2100;
	public const int CancelingCommand = 2101;
	public const int IgnoringCancellationForClosedConnection = 2102;
	public const int CancelingCommandFailed = 2103;

	public const int GettingCachedProcedure = 2200;
	public const int PoolDoesNotHaveSharedProcedureCache = 2201;
	public const int CouldNotNormalizeDatabaseAndName = 2202;
	public const int FailedToCacheProcedure = 2203;
	public const int CachingProcedure = 2204;
	public const int ProcedureCacheCount = 2205;
	public const int DidNotFindCachedProcedure = 2206;
	public const int ReturningCachedProcedure = 2207;

	public const int WaitingForAvailableSession = 3000;
	public const int CreatingNewConnectionPool = 3001;
	public const int ScanningForLeakedSessions = 3002;
	public const int FoundExistingSession = 3003;
	public const int DiscardingSessionDueToWrongGeneration = 3004;
	public const int SessionIsUnusable = 3005;
	public const int ReturningPooledSession = 3006;
	public const int ReturningNewSession = 3007;
	public const int DisposingCreatedSessionDueToException = 3008;
	public const int UnexpectedErrorInGetSessionAsync = 3009;
	public const int ReceivingSessionBack = 3010;
	public const int ReceivedInvalidSession = 3011;
	public const int ReceivedExpiredSession = 3012;
	public const int ClearingConnectionPool = 3013;
	public const int DisposingConnectionPool = 3014;
	public const int RecoveredNoSessions = 3015;
	public const int RecoveredSessionCount = 3016;
	public const int FoundSessionToCleanUp = 3017;
	public const int PoolCreatedNewSession = 3018;
	public const int CreatedSessionToReachMinimumPoolCount = 3019;
	public const int CreatedPoolWillNotBeUsed = 3020;

	public const int HasServerRedirectionHeader = 3100;
	public const int ServerRedirectionIsDisabled = 3101;
	public const int OpeningNewConnection = 3102;
	public const int FailedToConnectRedirectedSession = 3103;
	public const int ClosingSessionToUseRedirectedSession = 3104;
	public const int SessionAlreadyConnectedToServer = 3105;
	public const int RequiresServerRedirection = 3106;

	public const int ReapingConnectionPool = 3200;
	public const int CheckingForDnsChanges = 3201;
	public const int DetectedDnsChange = 3202;
	public const int DnsCheckFailed = 3203;
	public const int ClearingPoolDueToDnsChanges = 3204;
}
