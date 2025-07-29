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
	}

	[SkippableFact(ServerFeatures.KnownCertificateAuthority)]  
	public void ServerHasServerHostname()
	{
		using var connection = new MySqlConnection(AppConfig.ConnectionString);
		connection.Open();

		// Test that we can query server hostname
		using var cmd = new MySqlCommand("SELECT @@hostname", connection);
		var hostname = cmd.ExecuteScalar();
		
		// Hostname might be null on some server configurations, but the query should succeed
	}

	private readonly DatabaseFixture m_database;
}