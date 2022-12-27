namespace MySqlConnector.Logging;

internal static class EventIds
{
	public const int DataSourceCreatedWithPool = 1000;
	public const int DataSourceCreatedWithoutPool = 1001;

	public const int CreatedNonPooledSession = 2000;
	public const int ResettingConnection = 2001;
	public const int CreatedNewSession = 2002;
	public const int ReturningToPool = 2003;
	public const int SendingQuitCommand = 2004;
	public const int ConnectingFailed = 2005;
	public const int ServerSentAuthPluginName = 2006;
	public const int UnsupportedAuthenticationMethod = 2007;
	public const int AutoDetectedAurora57 = 2008;
	public const int SessionMadeConnection = 2009;
	public const int ServerDoesNotSupportSsl = 2010;
	public const int SessionDoesNotSupportSslProtocolsNone = 2011;
	public const int FailedNegotiatingTls = 2012;
	public const int CouldNotConnectToServer = 2013;
	public const int SendingPipelinedResetConnectionRequest = 2014;
	public const int SendingResetConnectionRequest = 2015;
	public const int SendingChangeUserRequest = 2016;
	public const int SendingChangeUserRequestDueToChangedDatabase = 2017;
	public const int OptimisticReauthenticationFailed = 2018;
	public const int IgnoringFailureInTryResetConnectionAsync = 2019;
	public const int SwitchingToAuthenticationMethod = 2020;
	public const int NeedsSecureConnection = 2021;
	public const int AuthenticationMethodNotSupported = 2022;
	public const int CouldNotLoadServerRsaPublicKey = 2023;
	public const int CouldNotLoadServerRsaPublicKeyFromFile = 2024;
	public const int CouldNotUseAuthenticationMethodForRsa = 2025;
	public const int FailedToResolveHostName = 2026;
	public const int ConnectingToIpAddress = 2027;
	public const int ConnectTimeoutExpired = 2028;
	public const int FailedToConnectToSingleIpAddress = 2029;
	public const int FailedToConnectToIpAddress = 2030;
	public const int ConnectedToIpAddress = 2031;
	public const int ConnectingToUnixSocket = 2032;
	public const int ConnectTimeoutExpiredForUnixSocket = 2033;
	public const int ConnectingToNamedPipe = 2034;
	public const int ConnectTimeoutExpiredForNamedPipe = 2035;
	public const int InitializingTlsConnection = 2036;
	public const int NoCertificatesFound = 2037;
	public const int CertificateNotFoundInStore = 2038;
	public const int CouldNotLoadCertificate = 2039;
	public const int NoPrivateKeyIncludedWithCertificateFile = 2040;
	public const int CouldNotLoadCertificateFromFile = 2041;
	public const int FailedToObtainClientCertificates = 2042;
	public const int LoadingCaCertificatesFromFile = 2043;
	public const int CouldNotLoadCaCertificateFromFile = 2044;
	public const int LoadingCaCertificate = 2045;
	public const int LoadedCaCertificatesFromFile = 2046;
	public const int NotUsingRemoteCertificateValidationCallbackDueToSslCa = 2047;
	public const int NotUsingRemoteCertificateValidationCallbackDueToSslMode = 2048;
	public const int UsingRemoteCertificateValidationCallback = 2049;
	public const int ConnectedTlsBasic = 2050;
	public const int ConnectedTlsDetailed = 2051;
	public const int CouldNotInitializeTlsConnection = 2052;
	public const int LoadingClientKeyFromKeyFile = 2053;
	public const int CouldNotLoadClientKeyFromKeyFile = 2054;
	public const int DetectedProxy = 2055;
	public const int ChangingConnectionId = 2056;
	public const int FailedToGetConnectionId = 2057;
	public const int ClosingStreamSocket = 2058;
	public const int CreatingConnectionAttributes = 2059;
	public const int ObtainingPasswordViaProvidePasswordCallback = 2060;
	public const int FailedToObtainPassword = 2061;

	public const int IgnoringCancellationForCommand = 2100;
	public const int CommandHasBeenCanceled = 2101;
	public const int IgnoringCancellationForClosedConnection = 2102;
	public const int CancelingCommandFailed = 2103;
	public const int WillCancelCommand = 2104;
	public const int CancelingCommandFromSession = 2105;
	public const int IgnoringCancellationForInactiveCommand = 2106;
	public const int CancelingCommand = 2107;
	public const int SendingSleepToClearPendingCancellation = 2108;

	public const int GettingCachedProcedure = 2200;
	public const int PoolDoesNotHaveSharedProcedureCache = 2201;
	public const int CouldNotNormalizeDatabaseAndName = 2202;
	public const int FailedToCacheProcedure = 2203;
	public const int CachingProcedure = 2204;
	public const int ProcedureCacheCount = 2205;
	public const int DidNotFindCachedProcedure = 2206;
	public const int ReturningCachedProcedure = 2207;

	public const int CannotExecuteNewCommandInState = 2300;
	public const int EnteringFinishQuerying = 2301;

	public const int PingingServer = 2400;
	public const int SuccessfullyPingedServer = 2401;
	public const int PingFailed = 2402;

	public const int FailedInReceiveReplyAsync = 2500;
	public const int ExpectedSessionState1 = 2501;
	public const int ExpectedSessionState6 = 2506;
	public const int FailedInSendReplyAsync = 2510;
	public const int SettingStateToFailed = 2511;
	public const int ErrorPayload = 2512;

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
