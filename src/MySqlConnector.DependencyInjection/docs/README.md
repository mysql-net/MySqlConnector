## About

MySqlConnector.DependencyInjection helps set up MySqlConnector in applications that use dependency injection, most notably in ASP.NET.
It allows easy configuration of your MySQL connections and registers the appropriate services in your DI container.

## How to Use

For example, if using the ASP.NET minimal web API, use the following to register MySqlConnector:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMySqlDataSource("Server=server;User ID=test;Password=test;Database=test");
```

This registers a transient `MySqlConnection` which can get injected into your controllers:

```csharp
app.MapGet("/", async (MySqlConnection connection) =>
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
	command.CommandText = "SELECT name FROM users LIMIT 1";
    return "Hello World: " + await command.ExecuteScalarAsync();
});
```

You can use `MySqlDataSource` directly if you need more than one connection:

```csharp
app.MapGet("/", async (MySqlDataSource dataSource) =>
{
    await using var connection1 = await dataSource.OpenConnectionAsync();
    await using var connection2 = await dataSource.OpenConnectionAsync();
    // use the two connections...
});
```

## Advanced Usage

The `AddMySqlDataSource` method also accepts a lambda parameter allowing you to configure aspects of MySqlConnector beyond the connection string.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMySqlDataSource("Server=server;User ID=test;Password=test;Database=test",
	x => x.UseRemoteCertificateValidationCallback((sender, certificate, chain, sslPolicyErrors) => { /* custom logic */ })
);
```
