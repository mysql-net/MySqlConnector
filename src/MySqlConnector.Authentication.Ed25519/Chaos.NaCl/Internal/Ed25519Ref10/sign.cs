using System;
using System.Security.Cryptography;

namespace Chaos.NaCl.Internal.Ed25519Ref10
{
	internal static partial class Ed25519Operations
	{
		public static void crypto_sign2(
			byte[] sig, int sigoffset,
			byte[] m, int moffset, int mlen,
			byte[] sk, int skoffset)
		{
			byte[] az;
			byte[] r;
			byte[] hram;
			GroupElementP3 R;
			using (var hasher = SHA512.Create())
			{
				az = hasher.ComputeHash(sk, skoffset, 32);
				ScalarOperations.sc_clamp(az, 0);

				hasher.Initialize();
				hasher.TransformBlock(az, 32, 32, null, 0);
				hasher.TransformFinalBlock(m, moffset, mlen);
				r = hasher.Hash;

				ScalarOperations.sc_reduce(r);
				GroupOperations.ge_scalarmult_base(out R, r, 0);
				GroupOperations.ge_p3_tobytes(sig, sigoffset, ref R);

				hasher.Initialize();
				hasher.TransformBlock(sig, sigoffset, 32, null, 0);
				hasher.TransformBlock(sk, skoffset + 32, 32, null, 0);
				hasher.TransformFinalBlock(m, moffset, mlen);
				hram = hasher.Hash;

				ScalarOperations.sc_reduce(hram);
				var s = new byte[32];//todo: remove allocation
				Array.Copy(sig, sigoffset + 32, s, 0, 32);
				ScalarOperations.sc_muladd(s, hram, az, r);
				Array.Copy(s, 0, sig, sigoffset + 32, 32);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
				CryptographicOperations.ZeroMemory(s);
#else
				CryptoBytes.Wipe(s);
#endif
			}
		}
	}
}
