#if MYSQL_DATA
using MySql.Data.MySqlClient;
#endif
using Xunit;

namespace MySqlConnector.Tests;

public class MySqlExceptionTests
{
	[Fact]
	public void Data()
	{
		var exception = new MySqlException(MySqlErrorCode.No, "two", "three");
		Assert.Equal(1002, exception.Data["Server Error Code"]);
		Assert.Equal("two", exception.Data["SqlState"]);
	}
}
