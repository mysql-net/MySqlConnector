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

	[Fact]
	public void DbException()
	{
		var exception = new MySqlException(MySqlErrorCode.CommandTimeoutExpired, "The Command Timeout expired before the operation completed.");
		var dbException = (DbException) exception;
		Assert.Equal((int)MySqlErrorCode.CommandTimeoutExpired, dbException.ErrorCode);
		Assert.Equal((int)MySqlErrorCode.CommandTimeoutExpired, exception.Number);
	}
}
