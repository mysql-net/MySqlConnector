using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Chaos.NaCl.Internal.Ed25519Ref10;

namespace MySqlConnector.Authentication.Ed25519;

/// <summary>
/// Provides an implementation of the <c>client_ed25519</c> authentication plugin for MariaDB.
/// </summary>
/// <remarks>See <a href="https://mariadb.com/kb/en/library/authentication-plugin-ed25519/">Authentication Plugin - ed25519</a>.</remarks>
public sealed class Ed25519AuthenticationPlugin : IAuthenticationPlugin2
{
	/// <summary>
	/// Registers the Ed25519 authentication plugin with MySqlConnector. You must call this method once before
	/// opening a connection that uses Ed25519 authentication.
	/// </summary>
	public static void Install()
	{
		if (Interlocked.CompareExchange(ref s_isInstalled, 1, 0) == 0)
			AuthenticationPlugins.Register(new Ed25519AuthenticationPlugin());
	}

	/// <summary>
	/// Gets the authentication plugin name.
	/// </summary>
	public string Name => "client_ed25519";

	/// <summary>
	/// Creates the authentication response.
	/// </summary>
	public byte[] CreateResponse(string password, ReadOnlySpan<byte> authenticationData)
	{
		CreateResponseAndHash(password, authenticationData, out _, out var authenticationResponse);
		return authenticationResponse;
	}

	/// <summary>
	/// Creates the Ed25519 password hash.
	/// </summary>
	public byte[] CreatePasswordHash(string password, ReadOnlySpan<byte> authenticationData)
	{
		CreateResponseAndHash(password, authenticationData, out var passwordHash, out _);
		return passwordHash;
	}

	private static void CreateResponseAndHash(string password, ReadOnlySpan<byte> authenticationData, out byte[] passwordHash, out byte[] authenticationResponse)
	{
		// Java reference: https://github.com/MariaDB/mariadb-connector-j/blob/master/src/main/java/org/mariadb/jdbc/internal/com/send/authentication/Ed25519PasswordPlugin.java
		// C reference:  https://github.com/MariaDB/server/blob/592fe954ef82be1bc08b29a8e54f7729eb1e1343/plugin/auth_ed25519/ref10/sign.c#L7

		/*** Java
			byte[] bytePwd;
			if (passwordCharacterEncoding != null && !passwordCharacterEncoding.isEmpty()) {
				bytePwd = password.getBytes(passwordCharacterEncoding);
			} else {
				bytePwd = password.getBytes();
			}
		*/
		byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

		/*** Java
			MessageDigest hash = MessageDigest.getInstance("SHA-512");

			byte[] az = hash.digest(bytePwd);
			az[0] &= 248;
			az[31] &= 63;
			az[31] |= 64;
		*/
		/*** C
			unsigned char az[64];

			crypto_hash_sha512(az,pw,pwlen);
			az[0] &= 248;
			az[31] &= 63;
			az[31] |= 64;
		*/

		using var sha512 = SHA512.Create();
		byte[] az = sha512.ComputeHash(passwordBytes);
		ScalarOperations.sc_clamp(az, 0);

		/*** Java
			int mlen = seed.length;
			final byte[] sm = new byte[64 + mlen];

			System.arraycopy(seed, 0, sm, 64, mlen);
			System.arraycopy(az, 32, sm, 32, 32);

			byte[] buff = Arrays.copyOfRange(sm, 32, 96);
			hash.reset();
			byte[] nonce = hash.digest(buff);
		*/
		/*** C
			unsigned char nonce[64];
			unsigned char hram[64];

			memmove(sm + 64,m,mlen);
			memmove(sm + 32,az + 32,32);
			crypto_hash_sha512(nonce,sm + 32,mlen + 32);
		*/

		byte[] sm = new byte[64 + authenticationData.Length];
		authenticationData.CopyTo(sm.AsSpan().Slice(64));
		Buffer.BlockCopy(az, 32, sm, 32, 32);
		byte[] nonce = sha512.ComputeHash(sm, 32, authenticationData.Length + 32);

		/*** Java
			ScalarOps scalar = new ScalarOps();

			EdDSAParameterSpec spec = EdDSANamedCurveTable.getByName("Ed25519");
			GroupElement elementAvalue = spec.getB().scalarMultiply(az);
			byte[] elementAarray = elementAvalue.toByteArray();
			System.arraycopy(elementAarray, 0, sm, 32, elementAarray.length);
		*/
		/*** C
			ge_p3 A;

			ge_scalarmult_base(&A,az);
			ge_p3_tobytes(sm + 32,&A);
		*/

		GroupOperations.ge_scalarmult_base(out var A, az, 0);
		GroupOperations.ge_p3_tobytes(sm, 32, ref A);

		passwordHash = new byte[32];
		Array.Copy(sm, 32, passwordHash, 0, 32);

		/*** Java
			nonce = scalar.reduce(nonce);
			GroupElement elementRvalue = spec.getB().scalarMultiply(nonce);
			byte[] elementRarray = elementRvalue.toByteArray();
			System.arraycopy(elementRarray, 0, sm, 0, elementRarray.length);
		*/
		/*** C
			ge_p3 R;

			sc_reduce(nonce);
			ge_scalarmult_base(&R,nonce);
			ge_p3_tobytes(sm,&R);
		*/
		ScalarOperations.sc_reduce(nonce);
		GroupOperations.ge_scalarmult_base(out var R, nonce, 0);
		GroupOperations.ge_p3_tobytes(sm, 0, ref R);

		/*** Java
			hash.reset();
			byte[] hram = hash.digest(sm);
			hram = scalar.reduce(hram);
			byte[] tt = scalar.multiplyAndAdd(hram, az, nonce);
			System.arraycopy(tt, 0, sm, 32, tt.length);

			return Arrays.copyOfRange(sm, 0, 64);
		*/
		/*** C
			unsigned char hram[64];

			crypto_hash_sha512(hram,sm,mlen + 64);
			sc_reduce(hram);
			sc_muladd(sm + 32,hram,az,nonce);

			return 0;
		*/
		var hram = sha512.ComputeHash(sm);
		ScalarOperations.sc_reduce(hram);
		var temp = new byte[32];
		ScalarOperations.sc_muladd(temp, hram, az, nonce);
		Buffer.BlockCopy(temp, 0, sm, 32, temp.Length);

		var result = new byte[64];
		Buffer.BlockCopy(sm, 0, result, 0, result.Length);
		authenticationResponse = result;
	}

	private Ed25519AuthenticationPlugin()
	{
	}

	private static int s_isInstalled;
}
