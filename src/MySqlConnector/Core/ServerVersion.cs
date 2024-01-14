using System.Buffers.Text;
using System.Text;
using MySqlConnector.Utilities;

namespace MySqlConnector.Core;

internal sealed class ServerVersion
{
	public ServerVersion(ReadOnlySpan<byte> versionString)
	{
		OriginalString = Encoding.ASCII.GetString(versionString);
		if (versionString.StartsWith("5.5.5-"u8))
		{
			// for MariaDB < 11.0.1
			versionString = versionString[6..];
			IsMariaDb = true;
		}
		else if (versionString.IndexOf("MariaDB"u8) != -1)
		{
			IsMariaDb = true;
		}

		var minor = 0;
		var build = 0;
		if (Utf8Parser.TryParse(versionString, out int major, out var bytesConsumed))
		{
			versionString = versionString[bytesConsumed..];
			if (versionString is [0x2E, ..])
			{
				versionString = versionString[1..];
				if (Utf8Parser.TryParse(versionString, out minor, out bytesConsumed))
				{
					versionString = versionString[bytesConsumed..];
					if (versionString is [0x2E, ..])
					{
						versionString = versionString[1..];
						if (Utf8Parser.TryParse(versionString, out build, out bytesConsumed))
						{
							versionString = versionString[bytesConsumed..];
						}
					}
				}
			}
		}

		Version = new Version(major, minor, build);
	}

	public string OriginalString { get; }
	public Version Version { get; }
	public bool IsMariaDb { get; }

	public static ServerVersion Empty { get; } = new();

	private ServerVersion()
	{
		OriginalString = "";
		Version = new();
	}
}
