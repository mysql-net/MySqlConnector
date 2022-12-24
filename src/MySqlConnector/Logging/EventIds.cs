namespace MySqlConnector.Logging;

internal static class EventIds
{
	public const int DataSourceCreatedWithPool = 1000;
	public const int DataSourceCreatedWithoutPool = 1001;

	public const int CreatedNonPooledSession = 2000;
	public const int ResettingConnection = 2001;

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
}
