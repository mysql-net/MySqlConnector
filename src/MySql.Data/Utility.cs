using System;
using System.Globalization;
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
	}
}
