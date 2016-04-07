using System;
using System.Security.Cryptography;
using System.Text;

namespace MySql.Data.Serialization
{
	internal static class AuthenticationUtility
	{
		/// <summary>
		/// Hashes a password with the "Secure Password Authentication" method.
		/// </summary>
		/// <param name="challenge">The 20-byte random challenge (from the "auth-plugin-data" in the initial handshake).</param>
		/// <param name="offset">The offset of the start of the challenge within <paramref name="challenge"/>.</param>
		/// <param name="password">The password to hash.</param>
		/// <returns>A 20-byte password hash.</returns>
		/// <remarks>See <a href="https://dev.mysql.com/doc/internals/en/secure-password-authentication.html">Secure Password Authentication</a>.</remarks>
		public static byte[] HashPassword(byte[] challenge, int offset, string password)
		{
			using (var sha1 = SHA1.Create())
			{
				var combined = new byte[40];
				Array.Copy(challenge, offset, combined, 0, 20);

				var passwordBytes = Encoding.UTF8.GetBytes(password);
				var hashedPassword = sha1.ComputeHash(passwordBytes);

				var doubleHashedPassword = sha1.ComputeHash(hashedPassword);
				Array.Copy(doubleHashedPassword, 0, combined, 20, 20);

				var xorBytes = sha1.ComputeHash(combined);
				for (int i = 0; i < hashedPassword.Length; i++)
					hashedPassword[i] ^= xorBytes[i];

				return hashedPassword;
			}
		}
	}
}
