using System;
using System.Globalization;

namespace SideBySide
{
	public class TestUtilities
	{
		public static Version ParseServerVersion(string serverVersion)
		{
			// copied from MySql.Data.MySqlClient.ServerVersion

			var last = 0;
			var index = serverVersion.IndexOf('.', last);
			var major = int.Parse(serverVersion.Substring(last, index - last), CultureInfo.InvariantCulture);
			last = index + 1;

			index = serverVersion.IndexOf('.', last);
			var minor = int.Parse(serverVersion.Substring(last, index - last), CultureInfo.InvariantCulture);
			last = index + 1;

			do
			{
				index++;
			} while (index < serverVersion.Length && serverVersion[index] >= '0' && serverVersion[index] <= '9');
			var build = int.Parse(serverVersion.Substring(last, index - last), CultureInfo.InvariantCulture);

			return new Version(major, minor, build);
		}

		public static bool SupportsJson(string serverVersion) =>
			ParseServerVersion(serverVersion).CompareTo(new Version(5, 7)) >= 0;
	}
}
