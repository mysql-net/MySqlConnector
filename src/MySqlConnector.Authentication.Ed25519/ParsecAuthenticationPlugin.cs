using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MySqlConnector.Authentication.Ed25519;

/// <summary>
/// Provides an implementation of the Parsec authentication plugin for MariaDB.
/// </summary>
public sealed class ParsecAuthenticationPlugin : IAuthenticationPlugin
{
	/// <summary>
	/// Registers the Parsec authentication plugin with MySqlConnector. You must call this method once before
	/// opening a connection that uses Parsec authentication.
	/// </summary>
	public static void Install()
	{
		if (Interlocked.CompareExchange(ref s_isInstalled, 1, 0) == 0)
			AuthenticationPlugins.Register(new ParsecAuthenticationPlugin());
	}

	/// <summary>
	/// Gets the authentication plugin name.
	/// </summary>
	public string Name => "parsec";

	/// <summary>
	/// Creates the authentication response.
	/// </summary>
	public byte[] CreateResponse(string password, ReadOnlySpan<byte> authenticationData)
	{
		// first 32 bytes are server scramble
		var serverScramble = authenticationData.Slice(0, 32);

		// generate client scramble
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		Span<byte> clientScramble = stackalloc byte[32];
		RandomNumberGenerator.Fill(clientScramble);
#else
		var clientScramble = new byte[32];
		using var randomNumberGenerator = RandomNumberGenerator.Create();
		randomNumberGenerator.GetBytes(clientScramble);
#endif

		// parse extended salt from remaining authentication data and verify format
		var extendedSalt = authenticationData.Slice(32);
		if (extendedSalt[0] != (byte) 'P')
			throw new ArgumentException("Invalid extended salt", nameof(authenticationData));
		if (extendedSalt[1] is not (>= 0 and <= 3))
			throw new ArgumentException("Invalid iteration count", nameof(authenticationData));

		var iterationCount = 1024 << extendedSalt[1];
		var salt = extendedSalt.Slice(2);

		// derive private key using PBKDF2-SHA512
		byte[] privateKey;
#if NET6_0_OR_GREATER
		privateKey = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterationCount, HashAlgorithmName.SHA512, 32);
#elif NET472_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		using (var pbkdf2 = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), salt.ToArray(), iterationCount, HashAlgorithmName.SHA512))
			privateKey = pbkdf2.GetBytes(32);
#else
		privateKey = Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2(
			password, salt.ToArray(), Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf.HMACSHA512,
			iterationCount, numBytesRequested: 32);
#endif
		var expandedPrivateKey = Chaos.NaCl.Ed25519.ExpandedPrivateKeyFromSeed(privateKey);

		// generate Ed25519 keypair and sign concatenated scrambles
		var message = new byte[serverScramble.Length + clientScramble.Length];
		serverScramble.CopyTo(message);
		clientScramble.CopyTo(message.AsSpan(serverScramble.Length));

		var signature = Chaos.NaCl.Ed25519.Sign(message, expandedPrivateKey);

		// return client scramble followed by signature
		var response = new byte[clientScramble.Length + signature.Length];
		clientScramble.CopyTo(response.AsSpan());
		signature.CopyTo(response.AsSpan(clientScramble.Length));
		
		return response;
	}

	private ParsecAuthenticationPlugin()
	{
	}

	private static int s_isInstalled;
}
