using System;
using System.Buffers.Text;
using System.Text;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal sealed class ServerVersion
{
	public ServerVersion(ReadOnlySpan<byte> versionString)
	{
		OriginalString = Encoding.ASCII.GetString(versionString);

		var minor = 0;
		var build = 0;
		if (Utf8Parser.TryParse(versionString, out int major, out var bytesConsumed))
		{
			versionString = versionString.Slice(bytesConsumed);
			if (versionString.Length > 0 && versionString[0] == 0x2E)
			{
				versionString = versionString.Slice(1);
				if (Utf8Parser.TryParse(versionString, out minor, out bytesConsumed))
				{
					versionString = versionString.Slice(bytesConsumed);
					if (versionString.Length > 0 && versionString[0] == 0x2E)
					{
						versionString = versionString.Slice(1);
						if (Utf8Parser.TryParse(versionString, out build, out bytesConsumed))
						{
							versionString = versionString.Slice(bytesConsumed);
						}
					}
				}
			}
		}

		Version = new Version(major, minor, build);

		// check for MariaDB version appended to a fake MySQL version
		if (versionString.Length != 0 && versionString[0] == 0x2D)
		{
			versionString = versionString.Slice(1);
			ReadOnlySpan<byte> mariaDb = new byte[] { 0x2D, 0x4D, 0x61, 0x72, 0x69, 0x61, 0x44, 0x42 }; // -MariaDB
			var mariaDbIndex = versionString.IndexOf(mariaDb);
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
								MariaDbVersion = new(major, minor, build);
						}
					}
				}
			}
		}
	}

	public string OriginalString { get; }
	public Version Version { get; }
	public Version? MariaDbVersion { get; }

	public static ServerVersion Empty { get; } = new();

	private ServerVersion()
	{
		OriginalString = "";
		Version = new Version(0, 0);
	}
}
