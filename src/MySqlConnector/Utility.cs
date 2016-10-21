using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MySql.Data
{
	internal static class Utility
	{
		public static void Dispose<T>(ref T disposable)
			where T : class, IDisposable
		{
			if (disposable != null)
			{
				disposable.Dispose();
				disposable = null;
			}
		}

		public static string FormatInvariant(this string format, params object[] args)
		{
			return string.Format(CultureInfo.InvariantCulture, format, args);
		}

		public static string GetString(this Encoding encoding, ArraySegment<byte> arraySegment)
			=> encoding.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

#if NET45
		public static bool TryGetBuffer(this MemoryStream memoryStream, out ArraySegment<byte> buffer)
		{
			try
			{
				var rawBuffer = memoryStream.GetBuffer();
				buffer = new ArraySegment<byte>(rawBuffer, 0, checked((int) memoryStream.Length));
				return true;
			}
			catch (UnauthorizedAccessException)
			{
				buffer = default(ArraySegment<byte>);
				return false;
			}
		}
#endif

		public static void WriteUtf8(this BinaryWriter writer, string value) =>
			WriteUtf8(writer, value, 0, value.Length);

		public static void WriteUtf8(this BinaryWriter writer, string value, int startIndex, int length)
		{
			var endIndex = startIndex + length;
			while (startIndex < endIndex)
			{
				int codePoint = char.ConvertToUtf32(value, startIndex);
				startIndex++;
				if (codePoint < 0x80)
				{
					writer.Write((byte) codePoint);
				}
				else if (codePoint < 0x800)
				{
					writer.Write((byte) (0xC0 | ((codePoint >> 6) & 0x1F)));
					writer.Write((byte) (0x80 | (codePoint & 0x3F)));
				}
				else if (codePoint < 0x10000)
				{
					writer.Write((byte) (0xE0 | ((codePoint >> 12) & 0x0F)));
					writer.Write((byte) (0x80 | ((codePoint >> 6) & 0x3F)));
					writer.Write((byte) (0x80 | (codePoint & 0x3F)));
				}
				else
				{
					writer.Write((byte) (0xF0 | ((codePoint >> 18) & 0x07)));
					writer.Write((byte) (0x80 | ((codePoint >> 12) & 0x3F)));
					writer.Write((byte) (0x80 | ((codePoint >> 6) & 0x3F)));
					writer.Write((byte) (0x80 | (codePoint & 0x3F)));
					startIndex++;
				}
			}
		}
	}
}
