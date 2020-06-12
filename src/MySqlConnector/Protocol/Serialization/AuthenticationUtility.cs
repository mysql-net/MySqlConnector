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

			var passwordBytes = Encoding.UTF8.GetBytes(password);
			Span<byte> hashedPassword = stackalloc byte[20];
			sha1.TryComputeHash(passwordBytes, hashedPassword, out _);
			sha1.TryComputeHash(hashedPassword, combined.Slice(20), out _);

			Span<byte> xorBytes = stackalloc byte[20];
			sha1.TryComputeHash(combined, xorBytes, out _);
			for (int i = 0; i < hashedPassword.Length; i++)
				hashedPassword[i] ^= xorBytes[i];

			return hashedPassword.ToArray();
		}

		public static byte[] CreateScrambleResponse(byte[] nonce, string password)
		{
			var scrambleResponse = string.IsNullOrEmpty(password)
				? Utility.EmptyByteArray
				: HashPasswordWithNonce(nonce, password);

			return scrambleResponse;
		}

		private static byte[] HashPasswordWithNonce(byte[] nonce, string password)
		{
			using var sha256 = SHA256.Create();
			var passwordBytes = Encoding.UTF8.GetBytes(password);
			var hashedPassword = sha256.ComputeHash(passwordBytes);

			var doubleHashedPassword = sha256.ComputeHash(hashedPassword);
			var combined = new byte[doubleHashedPassword.Length + nonce.Length];

			Buffer.BlockCopy(doubleHashedPassword, 0, combined, 0, doubleHashedPassword.Length);
			Buffer.BlockCopy(nonce, 0, combined, doubleHashedPassword.Length, nonce.Length);

			var xorBytes = sha256.ComputeHash(combined);
			for (int i = 0; i < hashedPassword.Length; i++)
				hashedPassword[i] ^= xorBytes[i];

			return hashedPassword;
		}
	}
}
