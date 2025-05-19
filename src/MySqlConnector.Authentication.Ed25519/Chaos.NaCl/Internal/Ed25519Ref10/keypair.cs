using System;
using System.Security.Cryptography;

namespace Chaos.NaCl.Internal.Ed25519Ref10
{
    internal static partial class Ed25519Operations
    {
        public static void crypto_sign_keypair(byte[] pk, int pkoffset, byte[] sk, int skoffset, byte[] seed, int seedoffset)
        {
            GroupElementP3 A;
            int i;

            Array.Copy(seed, seedoffset, sk, skoffset, 32);
#if NET5_0_OR_GREATER
            byte[] h = SHA512.HashData(sk.AsSpan(skoffset, 32));
#else
            using var hash = SHA512.Create();
            byte[] h = hash.ComputeHash(sk, skoffset, 32);
#endif
            ScalarOperations.sc_clamp(h, 0);

            GroupOperations.ge_scalarmult_base(out A, h, 0);
            GroupOperations.ge_p3_tobytes(pk, pkoffset, ref A);

            for (i = 0; i < 32; ++i) sk[skoffset + 32 + i] = pk[pkoffset + i];
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            CryptographicOperations.ZeroMemory(h);
#else
            CryptoBytes.Wipe(h);
#endif
        }
    }
}
