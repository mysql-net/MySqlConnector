using MySqlConnector.Core;
using MySqlConnector.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MySqlConnector.Tests;

public class ServerIdentificationTests
{
	[Fact]
	public void VerifyServerIdentity_WithMatchingUuids_ReturnsTrue()
	{
		// Arrange
		var session1 = CreateServerSession();
		var session2 = CreateServerSession();
		session1.ServerUuid = "test-uuid-123";
		session1.ServerId = 1;
		session2.ServerUuid = "test-uuid-123";
		session2.ServerId = 2; // Different server ID, but UUIDs match

		// Act
		bool result = InvokeVerifyServerIdentity(session1, session2);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void VerifyServerIdentity_WithDifferentUuids_ReturnsFalse()
	{
		// Arrange
		var session1 = CreateServerSession();
		var session2 = CreateServerSession();
		session1.ServerUuid = "test-uuid-123";
		session1.ServerId = 1;
		session2.ServerUuid = "test-uuid-456";
		session2.ServerId = 1; // Same server ID, but UUIDs don't match

		// Act
		bool result = InvokeVerifyServerIdentity(session1, session2);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void VerifyServerIdentity_WithMatchingServerIds_ReturnsTrue()
	{
		// Arrange
		var session1 = CreateServerSession();
		var session2 = CreateServerSession();
		session1.ServerUuid = null; // No UUID available
		session1.ServerId = 1;
		session2.ServerUuid = null; // No UUID available
		session2.ServerId = 1;

		// Act
		bool result = InvokeVerifyServerIdentity(session1, session2);

		// Assert
		Assert.True(result);
	}

	[Fact]
	public void VerifyServerIdentity_WithDifferentServerIds_ReturnsFalse()
	{
		// Arrange
		var session1 = CreateServerSession();
		var session2 = CreateServerSession();
		session1.ServerUuid = null; // No UUID available
		session1.ServerId = 1;
		session2.ServerUuid = null; // No UUID available
		session2.ServerId = 2;

		// Act
		bool result = InvokeVerifyServerIdentity(session1, session2);

		// Assert
		Assert.False(result);
	}

	[Fact]
	public void VerifyServerIdentity_WithNoIdentification_ReturnsTrue()
	{
		// Arrange
		var session1 = CreateServerSession();
		var session2 = CreateServerSession();
		session1.ServerUuid = null;
		session1.ServerId = null;
		session2.ServerUuid = null;
		session2.ServerId = null;

		// Act
		bool result = InvokeVerifyServerIdentity(session1, session2);

		// Assert
		Assert.True(result); // Should allow operation for backward compatibility
	}

	[Fact]
	public void VerifyServerIdentity_UuidTakesPrecedenceOverServerId()
	{
		// Arrange
		var session1 = CreateServerSession();
		var session2 = CreateServerSession();
		session1.ServerUuid = "test-uuid-123";
		session1.ServerId = 1;
		session2.ServerUuid = "test-uuid-456"; // Different UUID
		session2.ServerId = 1; // Same server ID

		// Act
		bool result = InvokeVerifyServerIdentity(session1, session2);

		// Assert
		Assert.False(result); // Should use UUID comparison, not server ID
	}

	private static ServerSession CreateServerSession()
	{
		var pool = new TestConnectionPool();
		return new ServerSession(NullLogger.Instance, pool);
	}

	private static bool InvokeVerifyServerIdentity(ServerSession session1, ServerSession session2)
	{
		// Use reflection to call the private VerifyServerIdentity method
		var method = typeof(ServerSession).GetMethod("VerifyServerIdentity", 
			System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
		return (bool)method!.Invoke(session1, new object[] { session2 })!;
	}

	private class TestConnectionPool : IConnectionPoolMetadata
	{
		public int Id => 1;
		public int Generation => 1;
		public ConnectionPool? ConnectionPool => null;
		public int GetNewSessionId() => 1;
	}
}