using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MySqlConnector.Authentication.Ed25519;

/// <summary>
/// Provides an implementation of the Parsec authentication plugin for MariaDB.
/// </summary>
public sealed class ParsecAuthenticationPlugin : IAuthenticationPlugin3
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
		CreateResponseAndPasswordHash(password, authenticationData, out var response, out _);
		return response;
	}

	/// <summary>
	/// Creates the authentication response.
	/// </summary>
	public void CreateResponseAndPasswordHash(string password, ReadOnlySpan<byte> authenticationData, out byte[] authenticationResponse, out byte[] passwordHash)
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
		byte[] privateKeySeed;
#if NET6_0_OR_GREATER
		privateKeySeed = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterationCount, HashAlgorithmName.SHA512, 32);
#elif NET472_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		using (var pbkdf2 = new Rfc2898DeriveBytes(Encoding.UTF8.GetBytes(password), salt.ToArray(), iterationCount, HashAlgorithmName.SHA512))
			privateKeySeed = pbkdf2.GetBytes(32);
#else
		privateKeySeed = Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2(
			password, salt.ToArray(), Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf.HMACSHA512,
			iterationCount, numBytesRequested: 32);
#endif
		Chaos.NaCl.Ed25519.KeyPairFromSeed(out var publicKey, out var privateKey, privateKeySeed);

		// generate Ed25519 keypair and sign concatenated scrambles
		var message = new byte[serverScramble.Length + clientScramble.Length];
		serverScramble.CopyTo(message);
		clientScramble.CopyTo(message.AsSpan(serverScramble.Length));

		var signature = Chaos.NaCl.Ed25519.Sign(message, privateKey);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
		CryptographicOperations.ZeroMemory(privateKey);
#endif

		// return client scramble followed by signature
		authenticationResponse = new byte[clientScramble.Length + signature.Length];
		clientScramble.CopyTo(authenticationResponse.AsSpan());
		signature.CopyTo(authenticationResponse.AsSpan(clientScramble.Length));

		// "password hash" for parsec is the extended salt followed by the public key
		passwordHash = [(byte) 'P', (byte) iterationCount, .. salt, .. publicKey];
	}

	private ParsecAuthenticationPlugin()
	{
	}

	private static int s_isInstalled;
}
