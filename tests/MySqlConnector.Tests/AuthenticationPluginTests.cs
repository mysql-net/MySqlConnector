using MySqlConnector.Authentication;

namespace MySqlConnector.Tests;

public class AuthenticationPluginTests : IDisposable
{
	public AuthenticationPluginTests()
	{
		m_server = new();
	}

	public void Dispose()
	{
		m_server.Stop();
	}

	[Fact]
	public async Task RegisteredPluginAnswersSwitchToItsMethod()
	{
		AuthenticationPlugins.Register(new PrefixPlugin(RegisteredMethod));
		m_server.AuthSwitchMethod = RegisteredMethod;
		m_server.AuthSwitchData = [7, 8, 9];
		m_server.Start();

		using var connection = new MySqlConnection(ConnectionString());
		await connection.OpenAsync();

		var response = await m_server.AuthSwitchResponse;
		Assert.Equal([(byte) 'p', (byte) 'w', 7, 8, 9], response);
	}

	[Fact]
	public async Task SwitchToUnregisteredMethodIsNotSupported()
	{
		m_server.AuthSwitchMethod = "no_such_authentication_method";
		m_server.Start();

		using var connection = new MySqlConnection(ConnectionString());
		var exception = await Assert.ThrowsAsync<NotSupportedException>(() => connection.OpenAsync());
		Assert.Equal("Authentication method 'no_such_authentication_method' is not supported.", exception.Message);
	}

	private string ConnectionString() =>
		new MySqlConnectionStringBuilder
		{
			Server = "localhost",
			Port = (uint) m_server.Port,
			UserID = "test",
			Password = "pw",
			Pooling = false,
		}.ConnectionString;

	// the response is the password followed by the server's authentication data, so a test can tell both arrived
	private sealed class PrefixPlugin(string name) : IAuthenticationPlugin
	{
		public string Name { get; } = name;

		public byte[] CreateResponse(string password, ReadOnlySpan<byte> authenticationData) =>
			[.. System.Text.Encoding.UTF8.GetBytes(password), .. authenticationData];
	}

	private const string RegisteredMethod = "test_registered_switch_method";

	private readonly FakeMySqlServer m_server;
}
