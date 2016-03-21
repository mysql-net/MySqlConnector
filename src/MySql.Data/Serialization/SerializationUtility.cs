using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MySql.Data.Serialization
{
	internal static class SerializationUtility
	{
		public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			while (count > 0)
			{
				int bytesRead = await stream.ReadAsync(buffer, offset, count, cancellationToken);
				if (bytesRead == 0)
					throw new EndOfStreamException();
				offset += bytesRead;
				count -= bytesRead;
			}
		}

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
	}
}
