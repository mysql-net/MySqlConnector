using System;
using System.Security.Cryptography;
using System.Text;
using MySqlConnector.Utilities;

namespace MySqlConnector.Protocol.Serialization
{
	internal static class AuthenticationUtility
	{
		public static byte[] CreateAuthenticationResponse(ReadOnlySpan<byte> challenge, string password) =>
			string.IsNullOrEmpty(password) ? Utility.EmptyByteArray : HashPassword(challenge, password);

		/// <summary>
		/// Hashes a password with the "Secure Password Authentication" method.
		/// </summary>
		/// <param name="challenge">The 20-byte random challenge (from the "auth-plugin-data" in the initial handshake).</param>
		/// <param name="password">The password to hash.</param>
		/// <returns>A 20-byte password hash.</returns>
		/// <remarks>See <a href="https://dev.mysql.com/doc/internals/en/secure-password-authentication.html">Secure Password Authentication</a>.</remarks>
		public static byte[] HashPassword(ReadOnlySpan<byte> challenge, string password)
		{
			using var sha1 = SHA1.Create();
			Span<byte> combined = stackalloc byte[40];
			challenge.CopyTo(combined);

			var passwordByteCount = Encoding.UTF8.GetByteCount(password);
			Span<byte> passwordBytes = stackalloc byte[passwordByteCount];
			Encoding.UTF8.GetBytes(password.AsSpan(), passwordBytes);
			Span<byte> hashedPassword = stackalloc byte[20];
			sha1.TryComputeHash(passwordBytes, hashedPassword, out _);
			sha1.TryComputeHash(hashedPassword, combined.Slice(20), out _);

			Span<byte> xorBytes = stackalloc byte[20];
			sha1.TryComputeHash(combined, xorBytes, out _);
			for (int i = 0; i < hashedPassword.Length; i++)
				hashedPassword[i] ^= xorBytes[i];

			return hashedPassword.ToArray();
		}

		public static byte[] CreateScrambleResponse(ReadOnlySpan<byte> nonce, string password)
		{
			var scrambleResponse = string.IsNullOrEmpty(password)
				? Utility.EmptyByteArray
				: HashPasswordWithNonce(nonce, password);

			return scrambleResponse;
		}

		private static byte[] HashPasswordWithNonce(ReadOnlySpan<byte> nonce, string password)
		{
			using var sha256 = SHA256.Create();
			var passwordByteCount = Encoding.UTF8.GetByteCount(password);
			Span<byte> passwordBytes = stackalloc byte[passwordByteCount];
			Encoding.UTF8.GetBytes(password.AsSpan(), passwordBytes);

			Span<byte> hashedPassword = stackalloc byte[32];
			sha256.TryComputeHash(passwordBytes, hashedPassword, out _);

			Span<byte> combined = stackalloc byte[32 + nonce.Length];
			sha256.TryComputeHash(hashedPassword, combined, out _);
			nonce.CopyTo(combined.Slice(32));

			Span<byte> xorBytes = stackalloc byte[32];
			sha256.TryComputeHash(combined, xorBytes, out _);
			for (int i = 0; i < hashedPassword.Length; i++)
				hashedPassword[i] ^= xorBytes[i];

			return hashedPassword.ToArray();
		}
	}
}
