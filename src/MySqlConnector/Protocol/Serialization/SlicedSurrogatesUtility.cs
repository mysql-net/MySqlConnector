using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlConnector.Protocol.Serialization
{
#if !NET45 && !NET461 && !NET471 && !NETSTANDARD1_3 && !NETSTANDARD2_0 && !NETSTANDARD2_1 && !NETCOREAPP2_1

	internal static class SlicedSurrogatesUtility
	{
		public static int GetUtf8ByteCount(ReadOnlySpan<char> span, ref char? surrogatePartBuffer)
		{
			var containsSurrogateAtBeginning = BitConverter.IsLittleEndian
				? char.IsLowSurrogate(span[0])
				: char.IsHighSurrogate(span[0]);
			var containsSurrogateAtEnding = BitConverter.IsLittleEndian
				? char.IsHighSurrogate(span[^1])
				: char.IsLowSurrogate(span[^1]);

			if (containsSurrogateAtBeginning || containsSurrogateAtEnding)
			{
				var startPosition = containsSurrogateAtBeginning ? 1 : 0;
				var length = containsSurrogateAtEnding ? span.Length - 1 : span.Length;

				var operationalSpan = span[startPosition..length];

				var totalCount = Encoding.UTF8.GetByteCount(operationalSpan);

				if (containsSurrogateAtBeginning)
				{
					totalCount += Encoding.UTF8.GetByteCount(new[] { surrogatePartBuffer!.Value, span[0] });
				}

				surrogatePartBuffer = containsSurrogateAtEnding ? span[^1] : null;

				return totalCount;
			}
			else
			{
				return Encoding.UTF8.GetByteCount(span);
			}
		}

		public static void PrepareSurrogateFreeSpan(ReadOnlyMemory<char> mem, ref char? surrogatePartBuffer, out string? previousSurrogatePair, out ReadOnlySpan<char> nextSlice)
		{
			var firstChar = mem.Slice(0, 1).Span[0];
			var lastChar = mem.Slice(mem.Length - 1, 1).Span[0];

			var containsSurrogateAtBeginning = BitConverter.IsLittleEndian
				? char.IsLowSurrogate(firstChar)
				: char.IsHighSurrogate(firstChar);
			var containsSurrogateAtEnding = BitConverter.IsLittleEndian
				? char.IsHighSurrogate(lastChar)
				: char.IsLowSurrogate(lastChar);

			previousSurrogatePair = null;

			if (containsSurrogateAtBeginning || containsSurrogateAtEnding)
			{
				var startPosition = containsSurrogateAtBeginning ? 1 : 0;
				var length = containsSurrogateAtEnding ? mem.Length - 1 : mem.Length;

				nextSlice = mem[startPosition..length].Span;

				if (containsSurrogateAtBeginning)
				{
					previousSurrogatePair = new string(new[] { surrogatePartBuffer!.Value, firstChar });
				}

				surrogatePartBuffer = containsSurrogateAtEnding ? lastChar : null;
			}
			else
			{
				nextSlice = mem.Span;
			}
		}
	}
#endif
}
