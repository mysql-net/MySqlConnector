## About

MySqlConnector is a C# [ADO.NET](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/) driver for [MySQL](https://www.mysql.com/), [MariaDB](https://mariadb.org/), [Amazon Aurora](https://aws.amazon.com/rds/aurora/), [Azure Database for MySQL](https://azure.microsoft.com/en-us/services/mysql/) and other MySQL-compatible databases.

More documentation is available at the [MySqlConnector website](https://mysqlconnector.net/).

## How to Use

```csharp
// set these values correctly for your database server
var builder = new MySqlConnectionStringBuilder
{
	Server = "your-server",
	UserID = "database-user",
	Password = "P@ssw0rd!",
	Database = "database-name",
};

// open a connection asynchronously
await using var connection = new MySqlConnection(builder.ConnectionString);
await connection.OpenAsync();

// create a DB command and set the SQL statement with parameters
await using var command = connection.CreateCommand();
command.CommandText = @"SELECT * FROM orders WHERE order_id = @OrderId;";
command.Parameters.AddWithValue("@OrderId", orderId);

// execute the command and read the results
await using var reader = await command.ExecuteReaderAsync();
while (reader.Read())
{
	var id = reader.GetInt32("order_id");
	var date = reader.GetDateTime("order_date");
	// ...
}
```

### ASP.NET

For ASP.NET, use the [MySqlConnector.DependencyInjection package](https://www.nuget.org/packages/MySqlConnector.DependencyInjection/) to integrate with dependency injection and logging.

```csharp
var builder = WebApplication.CreateBuilder(args);

// use AddMySqlDataSource to configure MySqlConnector
builder.Services.AddMySqlDataSource(builder.Configuration.GetConnectionString("Default"));

var app = builder.Build();

// use dependency injection to get a MySqlConnection in minimal APIs or in controllers
app.MapGet("/", async (MySqlConnection connection) =>
{
    // open and use the connection here
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM users LIMIT 1";
    return "Hello World: " + await command.ExecuteScalarAsync();
});

app.Run();
```

## Key Features

* Full support for async I/O
* High performance
* Supports .NET Framework, .NET Core, and .NET 5.0+

## Main Types

The main types provided by this library are:

* `MySqlConnection` (implementation of `DbConnection`)
* `MySqlCommand` (implementation of `DbCommand`)
* `MySqlDataReader` (implementation of `DbDataReader`)
* `MySqlBulkCopy`
* `MySqlBulkLoader`
* `MySqlConnectionStringBuilder`
* `MySqlConnectorFactory`
* `MySqlDataAdapter`
* `MySqlException`
* `MySqlTransaction` (implementation of `DbTransaction`)

## Related Packages

* Dependency Injection: [MySqlConnector.DependencyInjection](https://www.nuget.org/packages/MySqlConnector.DependencyInjection/)
* Entity Framework Core: [Pomelo.EntityFrameworkCore.MySql](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql/)

## Feedback

MySqlConnector is released as open source under the [MIT license](https://github.com/mysql-net/MySqlConnector/blob/master/LICENSE). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/mysql-net/MySqlConnector).
