using System.IO;
using Xunit;
using MySqlConnector.Utilities;

namespace MySqlConnector.Tests
{
	public class Utf8Tests
	{
		[Theory]
		[InlineData("a", new byte[] { 0x61 })]
		[InlineData("\u007F", new byte[] { 0x7F })]
		[InlineData("\u0080", new byte[] { 0xC2, 0x80 })]
		[InlineData("Ā", new byte[] { 0xC4, 0x80 })]
		[InlineData("\u07FF", new byte[] { 0xDF, 0xBF })]
		[InlineData("\u0800", new byte[] { 0xE0, 0xA0, 0x80 })]
		[InlineData("\uFFFF", new byte[] { 0xEF, 0xBF, 0xBF })]
		[InlineData("\U00010000", new byte[] { 0xF0, 0x90, 0x80, 0x80 })]
		[InlineData("\U0010FFF0\U0010FFF1", new byte[] { 0xF4, 0x8F, 0xBF, 0xB0, 0xF4, 0x8F, 0xBF, 0xB1 })]
		public void Encode(string input, byte[] expected)
		{
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			{
				writer.WriteUtf8(input);
				Assert.Equal(expected, stream.ToArray());
			}
		}
	}
}
