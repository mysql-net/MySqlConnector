namespace IntegrationTests;

public static class TestUtilities
{
	/// <summary>
	/// Asserts that two byte arrays are equal. This method is much faster than xUnit's <code>Assert.Equal</code>.
	/// </summary>
	/// <param name="expected">The expected byte array.</param>
	/// <param name="actual">The actual byte array.</param>
	public static void AssertEqual(byte[] expected, byte[] actual)
	{
		Assert.Equal(expected.Length, actual.Length);
		for (var i = 0; i < expected.Length; i++)
		{
			if (expected[i] != actual[i])
				Assert.Equal(expected[i], actual[i]);
		}
	}

	/// <summary>
	/// Verifies that <paramref name="value"/> is an integer (<see cref="int"/> or <see cref="long"/>) with the value <code>1</code>.
	/// </summary>
	public static void AssertIsOne(object value)
	{
		switch (value)
		{
			case int intValue:
				Assert.Equal(1, intValue);
				break;

			case long longValue:
				Assert.Equal(1L, longValue);
				break;

			default:
				Assert.Equal(1L, value);
				break;
		}
	}

	public static void AssertExecuteScalarReturnsOneOrIsCanceled(MySqlCommand command) =>
		AssertExecuteScalarReturnsOneOrThrowsException(command, MySqlErrorCode.QueryInterrupted);

#if !MYSQL_DATA
	public static void AssertExecuteScalarReturnsOneOrTimesOut(MySqlCommand command) =>
		AssertExecuteScalarReturnsOneOrThrowsException(command, MySqlErrorCode.CommandTimeoutExpired);
#endif

	private static void AssertExecuteScalarReturnsOneOrThrowsException(MySqlCommand command, MySqlErrorCode expectedCode)
	{
		if (AppConfig.SupportedFeatures.HasFlag(ServerFeatures.CancelSleepSuccessfully))
		{
			AssertIsOne(command.ExecuteScalar());
		}
		else
		{
			var ex = Assert.Throws<MySqlException>(command.ExecuteScalar);
#if MYSQL_DATA
			Assert.Equal((int) expectedCode, ex.Number);
#else
			Assert.Equal(expectedCode, ex.ErrorCode);
#endif
		}
	}

	public static async Task AssertExecuteScalarReturnsOneOrIsCanceledAsync(MySqlCommand command, CancellationToken token = default) =>
		await AssertExecuteScalarReturnsOneOrThrowsExceptionAsync(command, MySqlErrorCode.QueryInterrupted, token);

#if !MYSQL_DATA
	public static async Task AssertExecuteScalarReturnsOneOrTimesOutAsync(MySqlCommand command, CancellationToken token = default) =>
		await AssertExecuteScalarReturnsOneOrThrowsExceptionAsync(command, MySqlErrorCode.CommandTimeoutExpired, token);
#endif

	private static async Task AssertExecuteScalarReturnsOneOrThrowsExceptionAsync(MySqlCommand command, MySqlErrorCode expectedCode, CancellationToken token)
	{
		if (AppConfig.SupportedFeatures.HasFlag(ServerFeatures.CancelSleepSuccessfully))
		{
			AssertIsOne(await command.ExecuteScalarAsync(token));
		}
		else
		{
			var ex = await Assert.ThrowsAnyAsync<Exception>(async () => await command.ExecuteScalarAsync(token));
			MySqlException exception = ex as MySqlException;
			while (exception is null && ex is not null)
			{
				ex = ex.InnerException;
				exception = ex as MySqlException;
			}
			Assert.NotNull(exception);
#if MYSQL_DATA
			Assert.Equal((int) expectedCode, exception.Number);
#else
			Assert.Equal(expectedCode, exception.ErrorCode);
#endif
		}
	}

	/// <summary>
	/// Asserts that <paramref name="stopwatch"/> is in the range [minimumMilliseconds, minimumMilliseconds + lengthMilliseconds].
	/// </summary>
	/// <remarks>This method applies a scaling factor for delays encountered under Continuous Integration environments.</remarks>
	public static void AssertDuration(Stopwatch stopwatch, int minimumMilliseconds, int lengthMilliseconds)
	{
		var elapsed = stopwatch.ElapsedMilliseconds;
		Assert.InRange(elapsed, minimumMilliseconds, minimumMilliseconds + lengthMilliseconds * AppConfig.TimeoutDelayFactor);
	}

	public static string GetSkipReason(ServerFeatures serverFeatures, ConfigSettings configSettings)
	{
		if (!AppConfig.SupportedFeatures.HasFlag(serverFeatures))
			return $"Requires ServerFeatures.{serverFeatures}";

		if (configSettings == ConfigSettings.None)
			return null;

		var csb = AppConfig.CreateConnectionStringBuilder();
		if (configSettings.HasFlag(ConfigSettings.RequiresSsl) && (csb.SslMode == MySqlSslMode.Disabled
#if !MYSQL_DATA
		 || csb.SslMode == MySqlSslMode.Preferred
#endif
		 ))
			return "Requires SslMode=Required or higher in connection string";

		if (configSettings.HasFlag(ConfigSettings.TrustedHost) &&
			(csb.SslMode == MySqlSslMode.Disabled ||
#if !MYSQL_DATA
			csb.SslMode == MySqlSslMode.Preferred ||
#endif
			csb.SslMode == MySqlSslMode.Required))
		{
			return "Requires SslMode=VerifyCA or higher in connection string";
		}

		if (configSettings.HasFlag(ConfigSettings.UntrustedHost) &&
			(csb.SslMode == MySqlSslMode.VerifyCA || csb.SslMode == MySqlSslMode.VerifyFull))
		{
			return "Requires SslMode=Required or lower in connection string";
		}

		if (configSettings.HasFlag(ConfigSettings.KnownClientCertificate))
		{
			if (!((csb.CertificateFile?.EndsWith("ssl-client.pfx", StringComparison.OrdinalIgnoreCase) is true) || (csb.SslKey?.EndsWith("ssl-client-key.pem", StringComparison.OrdinalIgnoreCase) is true)))
				return "Requires CertificateFile=client.pfx in connection string";
		}

		if (configSettings.HasFlag(ConfigSettings.PasswordlessUser) && string.IsNullOrWhiteSpace(AppConfig.PasswordlessUser))
			return "Requires PasswordlessUser in config.json";

		if (configSettings.HasFlag(ConfigSettings.UserHasPassword) && csb.Password.Length == 0)
			return "Requires password in connection string";

		if (configSettings.HasFlag(ConfigSettings.GSSAPIUser) && string.IsNullOrWhiteSpace(AppConfig.GSSAPIUser))
			return "Requires GSSAPIUser in config.json";

		if (configSettings.HasFlag(ConfigSettings.HasKerberos) && !AppConfig.HasKerberos)
			return "Requires HasKerberos in config.json";

		if (configSettings.HasFlag(ConfigSettings.CsvFile) && string.IsNullOrWhiteSpace(AppConfig.MySqlBulkLoaderCsvFile))
			return "Requires MySqlBulkLoaderCsvFile in config.json";

		if (configSettings.HasFlag(ConfigSettings.LocalCsvFile) && string.IsNullOrWhiteSpace(AppConfig.MySqlBulkLoaderLocalCsvFile))
			return "Requires MySqlBulkLoaderLocalCsvFile in config.json";

		if (configSettings.HasFlag(ConfigSettings.TsvFile) && string.IsNullOrWhiteSpace(AppConfig.MySqlBulkLoaderTsvFile))
			return "Requires MySqlBulkLoaderTsvFile in config.json";

		if (configSettings.HasFlag(ConfigSettings.LocalTsvFile) && string.IsNullOrWhiteSpace(AppConfig.MySqlBulkLoaderLocalTsvFile))
			return "Requires MySqlBulkLoaderLocalTsvFile in config.json";

		if (configSettings.HasFlag(ConfigSettings.TcpConnection) && ((csb.Server.StartsWith("/", StringComparison.Ordinal) || csb.Server.StartsWith("./", StringComparison.Ordinal)) || csb.ConnectionProtocol != MySqlConnectionProtocol.Sockets))
			return "Requires a TCP connection";

		if (configSettings.HasFlag(ConfigSettings.SecondaryDatabase) && string.IsNullOrEmpty(AppConfig.SecondaryDatabase))
			return "Requires SecondaryDatabase in config.json";

		return null;
	}

#if MYSQL_DATA
	public static System.Threading.Tasks.Task PrepareAsync(this MySqlCommand command)
	{
		command.Prepare();
		return System.Threading.Tasks.Task.CompletedTask;
	}
#endif
}
