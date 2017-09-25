using System.IO;
using System.Text;

namespace MySqlConnector.Tests
{
	public static class BinaryWriterExtensions
	{
		public static void WriteRaw(this BinaryWriter writer, string value) => writer.Write(Encoding.UTF8.GetBytes(value));

		public static void WriteNullTerminated(this BinaryWriter writer, string value)
		{
			writer.Write(Encoding.UTF8.GetBytes(value));
			writer.Write((byte) 0);
		}
	}
}
