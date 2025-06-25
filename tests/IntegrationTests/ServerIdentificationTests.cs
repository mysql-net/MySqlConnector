using System.Diagnostics;

namespace IntegrationTests;

public class ServerIdentificationTests : IClassFixture<DatabaseFixture>, IDisposable
{
	public ServerIdentificationTests(DatabaseFixture database)
	{
		m_database = database;
	}

	public void Dispose()
	{
	}

	[SkippableFact(ServerFeatures.Timeout)]
	public void CancelCommand_WithServerVerification()
	{
		// This test verifies that cancellation still works with server verification
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();
		
		using var cmd = new MySqlCommand("SELECT SLEEP(5)", connection);
		var task = Task.Run(async () =>
		{
			await Task.Delay(TimeSpan.FromSeconds(0.5));
			cmd.Cancel();
		});

		var stopwatch = Stopwatch.StartNew();
		TestUtilities.AssertExecuteScalarReturnsOneOrIsCanceled(cmd);
		Assert.InRange(stopwatch.ElapsedMilliseconds, 250, 2500);

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
		task.Wait(); // shouldn't throw
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

		TestUtilities.LogInfo("Cancellation with server verification completed successfully");
	}

	[SkippableFact(ServerFeatures.KnownCertificateAuthority)]  
	public void ServerHasServerIdentification()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		// Test that we can query server identification manually
		using var cmd = new MySqlCommand("SELECT @@server_id", connection);
		var serverId = cmd.ExecuteScalar();
		Assert.NotNull(serverId);
		TestUtilities.LogInfo($"Server ID: {serverId}");

		// Test server UUID if available (MySQL 5.6+)
		if (connection.ServerVersion.Version.Major > 5 || 
			(connection.ServerVersion.Version.Major == 5 && connection.ServerVersion.Version.Minor >= 6))
		{
			try
			{
				using var uuidCmd = new MySqlCommand("SELECT @@server_uuid", connection);
				var serverUuid = uuidCmd.ExecuteScalar();
				Assert.NotNull(serverUuid);
				TestUtilities.LogInfo($"Server UUID: {serverUuid}");
			}
			catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.UnknownSystemVariable)
			{
				// Some MySQL-compatible servers might not support server_uuid
				TestUtilities.LogInfo("Server UUID not supported on this server");
			}
		}
	}

	private readonly DatabaseFixture m_database;
}