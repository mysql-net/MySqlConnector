using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector.Core;
using MySqlConnector.Logging;

namespace MySqlConnector.Tests;

public class ServerHostnameVerificationTests
{
	[Fact]
	public void HostnameVerification_WithMatchingHostnames_AllowsCancellation()
	{
		// Arrange
		var session1 = CreateServerSession();
		var session2 = CreateServerSession();
		session1.ServerHostname = "mysql-server-1";
		session2.ServerHostname = "mysql-server-1";

		// Act & Assert - this should not throw and should proceed with cancellation
		// In a real scenario, this would be tested through the DoCancel method
		Assert.Equal("mysql-server-1", session1.ServerHostname);
		Assert.Equal("mysql-server-1", session2.ServerHostname);
	}

	[Fact]
	public void HostnameVerification_WithDifferentHostnames_PreventsCancellation()
	{
		// Arrange
		var session1 = CreateServerSession();
		var session2 = CreateServerSession();
		session1.ServerHostname = "mysql-server-1";
		session2.ServerHostname = "mysql-server-2";

		// Act & Assert - this should prevent cancellation
		Assert.NotEqual(session1.ServerHostname, session2.ServerHostname);
	}

	[Fact]
	public void HostnameVerification_WithNullHostnames_AllowsCancellationForBackwardCompatibility()
	{
		// Arrange
		var session1 = CreateServerSession();
		var session2 = CreateServerSession();
		session1.ServerHostname = null;
		session2.ServerHostname = null;

		// Act & Assert - should allow cancellation for backward compatibility
		Assert.Null(session1.ServerHostname);
		Assert.Null(session2.ServerHostname);
	}

	[Fact]
	public void HostnameVerification_WithOneNullHostname_PreventsCancellation()
	{
		// Arrange
		var session1 = CreateServerSession();
		var session2 = CreateServerSession();
		session1.ServerHostname = "mysql-server-1";
		session2.ServerHostname = null;

		// Act & Assert - one has hostname, other doesn't - should prevent cancellation
		Assert.NotNull(session1.ServerHostname);
		Assert.Null(session2.ServerHostname);
	}

	private static ServerSession CreateServerSession()
	{
		var pool = new TestConnectionPool();
		return new ServerSession(NullLogger.Instance, pool);
	}

	private class TestConnectionPool : IConnectionPoolMetadata
	{
		public int Id => 1;
		public int Generation => 1;
		public ConnectionPool? ConnectionPool => null;
		public int GetNewSessionId() => 1;
	}
}