#if NET5_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using System.Security.Cryptography;
using System.Text;
#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
using MySqlConnector.Utilities;
#endif

namespace MySqlConnector.Protocol.Serialization;

#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms

internal static class AuthenticationUtility
{
	/// <summary>
	/// Returns the UTF-8 bytes for <paramref name="password"/>, followed by a null byte.
	/// </summary>
	public static byte[] GetNullTerminatedPasswordBytes(string password)
	{
		var passwordByteCount = Encoding.UTF8.GetByteCount(password);
		var passwordBytes = new byte[passwordByteCount + 1];
		Encoding.UTF8.GetBytes(password.AsSpan(), passwordBytes);
		return passwordBytes;
	}

	public static byte[] CreateAuthenticationResponse(ReadOnlySpan<byte> challenge, string password) =>
		string.IsNullOrEmpty(password) ? [] : HashPassword(challenge, password);

	/// <summary>
	/// Hashes a password with the "Secure Password Authentication" method.
	/// </summary>
	/// <param name="challenge">The 20-byte random challenge (from the "auth-plugin-data" in the initial handshake).</param>
	/// <param name="password">The password to hash.</param>
	/// <returns>A 20-byte password hash.</returns>
	/// <remarks>See <a href="https://dev.mysql.com/doc/internals/en/secure-password-authentication.html">Secure Password Authentication</a>.</remarks>
#if NET5_0_OR_GREATER
	[SkipLocalsInit]
#endif
	public static byte[] HashPassword(ReadOnlySpan<byte> challenge, string password)
	{
#if !NET5_0_OR_GREATER
		using var sha1 = SHA1.Create();
#endif
		Span<byte> combined = stackalloc byte[40];
		challenge.CopyTo(combined);

		var passwordByteCount = Encoding.UTF8.GetByteCount(password);
		Span<byte> passwordBytes = stackalloc byte[passwordByteCount];
		Encoding.UTF8.GetBytes(password.AsSpan(), passwordBytes);
		Span<byte> hashedPassword = stackalloc byte[20];
#if NET5_0_OR_GREATER
		SHA1.TryHashData(passwordBytes, hashedPassword, out _);
		SHA1.TryHashData(hashedPassword, combined[20..], out _);
#else
		sha1.TryComputeHash(passwordBytes, hashedPassword, out _);
		sha1.TryComputeHash(hashedPassword, combined[20..], out _);
#endif

		Span<byte> xorBytes = stackalloc byte[20];
#if NET5_0_OR_GREATER
		SHA1.TryHashData(combined, xorBytes, out _);
#else
		sha1.TryComputeHash(combined, xorBytes, out _);
#endif
		for (var i = 0; i < hashedPassword.Length; i++)
			hashedPassword[i] ^= xorBytes[i];

		return hashedPassword.ToArray();
	}

	public static byte[] CreateScrambleResponse(ReadOnlySpan<byte> nonce, string password) =>
		string.IsNullOrEmpty(password) ? [] : HashPasswordWithNonce(nonce, password);

#if NET5_0_OR_GREATER
	[SkipLocalsInit]
#endif
	private static byte[] HashPasswordWithNonce(ReadOnlySpan<byte> nonce, string password)
	{
#if !NET5_0_OR_GREATER
		using var sha256 = SHA256.Create();
#endif
		var passwordByteCount = Encoding.UTF8.GetByteCount(password);
		Span<byte> passwordBytes = stackalloc byte[passwordByteCount];
		Encoding.UTF8.GetBytes(password.AsSpan(), passwordBytes);

		Span<byte> hashedPassword = stackalloc byte[32];
#if NET5_0_OR_GREATER
		SHA256.TryHashData(passwordBytes, hashedPassword, out _);
#else
		sha256.TryComputeHash(passwordBytes, hashedPassword, out _);
#endif

		Span<byte> combined = stackalloc byte[32 + nonce.Length];
#if NET5_0_OR_GREATER
		SHA256.TryHashData(hashedPassword, combined, out _);
#else
		sha256.TryComputeHash(hashedPassword, combined, out _);
#endif
		nonce.CopyTo(combined[32..]);

		Span<byte> xorBytes = stackalloc byte[32];
#if NET5_0_OR_GREATER
		SHA256.TryHashData(combined, xorBytes, out _);
#else
		sha256.TryComputeHash(combined, xorBytes, out _);
#endif
		for (int i = 0; i < hashedPassword.Length; i++)
			hashedPassword[i] ^= xorBytes[i];

		return hashedPassword.ToArray();
	}
}
