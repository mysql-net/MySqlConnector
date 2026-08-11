// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// https://github.com/SixLabors/ImageSharp/blob/master/src/ImageSharp/Compression/Zlib/Adler32.cs

#if !NET6_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace MySqlConnector.Utilities;

/// <summary>
/// Calculates the 32 bit Adler checksum of a given buffer according to
/// RFC 1950. ZLIB Compressed Data Format Specification version 3.3)
/// </summary>
internal static class Adler32
{
	/// <summary>
	/// The default initial seed value of a Adler32 checksum calculation.
	/// </summary>
	public const uint SeedValue = 1U;

	// Largest prime smaller than 65536
	private const uint BASE = 65521;

	// NMAX is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1
	private const uint NMAX = 5552;

	/// <summary>
	/// Calculates the Adler32 checksum with the bytes taken from the span.
	/// </summary>
	/// <param name="buffer">The readonly span of bytes.</param>
	/// <returns>The <see cref="uint"/>.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint Calculate(ReadOnlySpan<byte> buffer)
	{
		if (buffer.IsEmpty)
		{
			return SeedValue;
		}

		return CalculateScalar(buffer);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe uint CalculateScalar(ReadOnlySpan<byte> buffer)
	{
		uint s1 = SeedValue & 0xFFFF;
		uint s2 = (SeedValue >> 16) & 0xFFFF;
		uint k;

		fixed (byte* bufferPtr = buffer)
		{
			var localBufferPtr = bufferPtr;
			uint length = (uint) buffer.Length;

			while (length > 0)
			{
				k = length < NMAX ? length : NMAX;
				length -= k;

				while (k >= 16)
				{
					s2 += s1 += localBufferPtr[0];
					s2 += s1 += localBufferPtr[1];
					s2 += s1 += localBufferPtr[2];
					s2 += s1 += localBufferPtr[3];
					s2 += s1 += localBufferPtr[4];
					s2 += s1 += localBufferPtr[5];
					s2 += s1 += localBufferPtr[6];
					s2 += s1 += localBufferPtr[7];
					s2 += s1 += localBufferPtr[8];
					s2 += s1 += localBufferPtr[9];
					s2 += s1 += localBufferPtr[10];
					s2 += s1 += localBufferPtr[11];
					s2 += s1 += localBufferPtr[12];
					s2 += s1 += localBufferPtr[13];
					s2 += s1 += localBufferPtr[14];
					s2 += s1 += localBufferPtr[15];

					localBufferPtr += 16;
					k -= 16;
				}

				while (k-- > 0)
				{
					s2 += s1 += *localBufferPtr++;
				}

				s1 %= BASE;
				s2 %= BASE;
			}

			return (s2 << 16) | s1;
		}
	}
}
#endif
