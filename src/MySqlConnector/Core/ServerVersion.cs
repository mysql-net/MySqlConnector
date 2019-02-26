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

			Version = new Version(major, minor, build);
		}

		public string OriginalString { get; }
		public Version Version { get; }
	}
}
