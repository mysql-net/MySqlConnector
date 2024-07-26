namespace MySqlConnector.Authentication;

/// <summary>
/// The primary interface implemented by an authentication plugin.
/// </summary>
public interface IAuthenticationPlugin
{
	/// <summary>
	/// Gets the authentication plugin name.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Creates the authentication response.
	/// </summary>
	/// <param name="password">The client's password.</param>
	/// <param name="authenticationData">The authentication data supplied by the server; this is the <code>auth method data</code>
	/// from the <a href="https://dev.mysql.com/doc/internals/en/connection-phase-packets.html#packet-Protocol::AuthSwitchRequest">Authentication
	/// Method Switch Request Packet</a>.</param>
	/// <returns>The authentication response.</returns>
	byte[] CreateResponse(string password, ReadOnlySpan<byte> authenticationData);

	/// <summary>
	/// create password hash for fingerprint verification
	/// </summary>
	/// <param name="password">The client's password.</param>
	/// <param name="authenticationData">The authentication data supplied by the server; this is the <code>auth method data</code>
	/// from the <a href="https://dev.mysql.com/doc/internals/en/connection-phase-packets.html#packet-Protocol::AuthSwitchRequest">Authentication
	/// Method Switch Request Packet</a>.</param>
	/// <returns>password hash</returns>
	byte[] CreatePasswordHash(string password, ReadOnlySpan<byte> authenticationData);
}
