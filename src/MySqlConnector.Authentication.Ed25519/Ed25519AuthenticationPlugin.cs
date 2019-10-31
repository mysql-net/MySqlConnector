using System;
using System.Security.Cryptography;
using System.Text;
using Chaos.NaCl.Internal.Ed25519Ref10;

namespace MySqlConnector.Authentication.Ed25519
{
	/// <summary>
	/// Provides an implementation of the <c>client_ed25519</c> authentication plugin for MariaDB.
	/// </summary>
	/// <remarks>See <a href="https://mariadb.com/kb/en/library/authentication-plugin-ed25519/">Authentication Plugin - ed25519</a>.</remarks>
	public sealed class Ed25519AuthenticationPlugin : IAuthenticationPlugin
	{
		/// <summary>
		/// Registers the Ed25519 authentication plugin with MySqlConnector. You must call this method once before
		/// opening a connection that uses Ed25519 authentication.
		/// </summary>
		public static void Install()
		{
			if (!s_isInstalled)
			{
				AuthenticationPlugins.Register(new Ed25519AuthenticationPlugin());
				s_isInstalled = true;
			}
		}

		public string Name => "client_ed25519";

		public byte[] CreateResponse(string password, ReadOnlySpan<byte> authenticationData)
		{
			byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

			using var sha512 = SHA512.Create();
			byte[] az = sha512.ComputeHash(passwordBytes);
			ScalarOperations.sc_clamp(az, 0);


			byte[] sm = new byte[64 + authenticationData.Length];
			authenticationData.CopyTo(sm.AsSpan().Slice(64));
			Buffer.BlockCopy(az, 32, sm, 32, 32);
			byte[] nonce = sha512.ComputeHash(sm, 32, authenticationData.Length + 32);

			GroupOperations.ge_scalarmult_base(out var A, az, 0);
			GroupOperations.ge_p3_tobytes(sm, 32, ref A);

			ScalarOperations.sc_reduce(nonce);
			GroupOperations.ge_scalarmult_base(out var R, nonce, 0);
			GroupOperations.ge_p3_tobytes(sm, 0, ref R);

			var hram = sha512.ComputeHash(sm);
			ScalarOperations.sc_reduce(hram);
			var temp = new byte[32];
			ScalarOperations.sc_muladd(temp, hram, az, nonce);
			Buffer.BlockCopy(temp, 0, sm, 32, temp.Length);

			var result = new byte[64];
			Buffer.BlockCopy(sm, 0, result, 0, result.Length);
			return result;
		}

		private Ed25519AuthenticationPlugin()
		{
		}

		static bool s_isInstalled;
	}
}
