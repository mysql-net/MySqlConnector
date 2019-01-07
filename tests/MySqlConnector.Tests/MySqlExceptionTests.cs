using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using MySql.Data.MySqlClient;
using Xunit;

namespace MySqlConnector.Tests
{
	public class MySqlExceptionTests
	{
		[Fact]
		public void IsSerializable()
		{
			var exception = new MySqlException(1, "two", "three");
			MySqlException copy;

			using (var stream = new MemoryStream())
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(stream, exception);
				stream.Position = 0;
				copy = (MySqlException) formatter.Deserialize(stream);
			}

			Assert.Equal(exception.Number, copy.Number);
			Assert.Equal(exception.SqlState, copy.SqlState);
			Assert.Equal(exception.Message, copy.Message);
		}

		[Fact]
		public void Data()
		{
			var exception = new MySqlException(1, "two", "three");
			Assert.Equal(1, exception.Data["Server Error Code"]);
			Assert.Equal("two", exception.Data["SqlState"]);
		}
	}
}
