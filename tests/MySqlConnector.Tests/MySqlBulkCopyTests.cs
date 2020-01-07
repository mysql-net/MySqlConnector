using System;
using System.Text;
using MySql.Data.MySqlClient;
using Xunit;

namespace MySqlConnector.Tests
{
	public class MySqlBulkCopyTests
	{
		[Fact]
		public void WriteStringDoesNotWriteWhenBufferIsTooSmall()
		{
			var value = "input string value";

			var valueByteCount = Encoding.UTF8.GetByteCount(value);
			var output = new byte[valueByteCount - 1];

			Assert.False(MySqlBulkCopy.WriteString(value, output.AsSpan(), out var bytesWritten));
			Assert.Equal(0, bytesWritten);
		}

		[Fact]
		public void WriteStringDoesNotWriteWhenBufferIsFull()
		{
			var value = "input string value";

			var valueByteCount = Encoding.UTF8.GetByteCount(value);
			var output = new byte[valueByteCount];

			Assert.False(MySqlBulkCopy.WriteString(value, output.AsSpan(), out var bytesWritten));
			Assert.Equal(0, bytesWritten);
		}

		[Fact]
		public void WriteStringDoesWriteEncodedString()
		{
			var value = "input string value";

			var valueByteCount = Encoding.UTF8.GetByteCount(value);
			var output = new byte[valueByteCount + 1];

			Assert.True(MySqlBulkCopy.WriteString(value, output.AsSpan(), out var bytesWritten));
			Assert.Equal(valueByteCount, bytesWritten);

			var decodedValue = Encoding.UTF8.GetString(output.AsSpan(0, bytesWritten));
			Assert.Equal(value, decodedValue);
		}
	}
}
