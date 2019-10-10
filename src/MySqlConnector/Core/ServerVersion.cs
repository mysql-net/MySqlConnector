using System;
using System.Buffers.Text;
using System.Text;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core
{
	internal sealed class ServerVersion
	{
		public ServerVersion(ReadOnlySpan<byte> versionString)
		{
			OriginalString = Encoding.ASCII.GetString(versionString);

			if (!Utf8Parser.TryParse(versionString, out int major, out var bytesConsumed) || versionString[bytesConsumed] != 0x2E)
				throw new InvalidOperationException("Error parsing " + OriginalString);
			versionString = versionString.Slice(bytesConsumed + 1);
			if (!Utf8Parser.TryParse(versionString, out int minor, out bytesConsumed) || versionString[bytesConsumed] != 0x2E)
				throw new InvalidOperationException("Error parsing " + OriginalString);
			versionString = versionString.Slice(bytesConsumed + 1);
			if (!Utf8Parser.TryParse(versionString, out int build, out bytesConsumed))
				throw new InvalidOperationException("Error parsing " + OriginalString);
			versionString = versionString.Slice(bytesConsumed);

			Version = new Version(major, minor, build);

			// check for MariaDB version appended to a fake MySQL version
			if (versionString.Length != 0 && versionString[0] == 0x2D)
			{
				versionString = versionString.Slice(1);
				var mariaDbIndex = versionString.IndexOf(MariaDb);
				if (mariaDbIndex != -1)
				{
					var totalBytesRead = 0;
					if (Utf8Parser.TryParse(versionString, out major, out bytesConsumed) && versionString[bytesConsumed] == 0x2E)
					{
						versionString = versionString.Slice(bytesConsumed + 1);
						totalBytesRead += bytesConsumed + 1;
						if (Utf8Parser.TryParse(versionString, out minor, out bytesConsumed) && versionString[bytesConsumed] == 0x2E)
						{
							versionString = versionString.Slice(bytesConsumed + 1);
							totalBytesRead += bytesConsumed + 1;
							if (Utf8Parser.TryParse(versionString, out build, out bytesConsumed) && versionString[bytesConsumed] == 0x2D)
							{
								totalBytesRead += bytesConsumed;
								if (totalBytesRead == mariaDbIndex)
									MariaDbVersion = new Version(major, minor, build);
							}
						}
					}
				}
			}
		}

		public string OriginalString { get; }
		public Version Version { get; }
		public Version? MariaDbVersion { get; }

		public static ServerVersion Empty { get; } = new ServerVersion();

		private ServerVersion()
		{
			OriginalString = "";
			Version = new Version(0, 0);
		}

		static ReadOnlySpan<byte> MariaDb => new byte[] { 0x2D, 0x4D, 0x61, 0x72, 0x69, 0x61, 0x44, 0x42 }; // -MariaDB
	}
}
