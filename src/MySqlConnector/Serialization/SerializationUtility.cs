using System;
using System.Net.Sockets;

namespace MySql.Data.Serialization
{
	internal static class SerializationUtility
	{
		public static uint ReadUInt32(byte[] buffer, int offset, int count)
		{
			uint value = 0;
			for (int i = 0; i < count; i++)
				value |= ((uint) buffer[offset + i]) << (8 * i);
			return value;
		}

		public static void WriteUInt32(uint value, byte[] buffer, int offset, int count)
		{
			for (int i = 0; i < count; i++)
			{
				buffer[offset + i] = (byte) (value & 0xFF);
				value >>= 8;
			}
		}

		public static int GetBytesPerCharacter(CharacterSet characterSet)
		{
			// not an exhaustive mapping, but should cover commonly-used character sets
			switch (characterSet)
			{
			case CharacterSet.Utf16Binary:
			case CharacterSet.Utf16GeneralCaseInsensitive:
			case CharacterSet.Utf16UnicodeCaseInsensitive:
			case CharacterSet.Utf16leBinary:
				return 2;

			case CharacterSet.Utf8Binary:
			case CharacterSet.Utf8GeneralCaseInsensitive:
			case CharacterSet.Utf8UnicodeCaseInsensitive:
				return 3;

			case CharacterSet.Utf8Mb4Binary:
			case CharacterSet.Utf8Mb4GeneralCaseInsensitive:
			case CharacterSet.Utf8Mb4UnicodeCaseInsensitive:
			case CharacterSet.Utf8Mb4Unicode520CaseInsensitive:
			case CharacterSet.Utf8Mb4Uca900AccentInsensitiveCaseInsensitive:
			case CharacterSet.Utf8Mb4Uca900AccentSensitiveCaseSensitive:
			case CharacterSet.Utf32Binary:
			case CharacterSet.Utf32GeneralCaseInsensitive:
			case CharacterSet.Utf32UnicodeCaseInsensitive:
			case CharacterSet.Utf32Unicode520CaseInsensitive:
				return 4;

			default:
				return 1;
			}
		}

		public static void SetKeepalive(Socket socket, uint keepAliveTimeSeconds)
		{
			// Always use the OS Default Keepalive settings
			socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
			if (keepAliveTimeSeconds == 0)
				return;

			// If keepAliveTimeSeconds > 0, override keepalive options on the socket
			const uint keepAliveIntervalMillis = 1000;
			if (Utility.IsWindows())
			{
				// http://stackoverflow.com/a/11834055/1419658
				// Windows takes time in milliseconds
				var keepAliveTimeMillis = keepAliveTimeSeconds > uint.MaxValue / 1000 ? uint.MaxValue : keepAliveTimeSeconds * 1000;
				var inOptionValues = new byte[sizeof(uint) * 3];
				BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
				BitConverter.GetBytes(keepAliveTimeMillis).CopyTo(inOptionValues, sizeof(uint));
				BitConverter.GetBytes(keepAliveIntervalMillis).CopyTo(inOptionValues, sizeof(uint) * 2);
				socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
			}
			// Unix not supported: The appropriate socket options to set Keepalive options are not exposd in .NET
			// https://github.com/dotnet/corefx/issues/14237
			// Unix will still respect the OS Default Keepalive settings
		}
	}
}
