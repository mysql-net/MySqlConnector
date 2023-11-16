## About

MySqlConnector.DependencyInjection helps set up MySqlConnector in applications that use dependency injection, most notably in ASP.NET.
It allows easy configuration of your MySQL connections and registers the appropriate services in your DI container.
It also configures logging by integrating MySqlConnector with the `ILoggingFactory` registered with the service provider.

## How to Use

For example, if using the ASP.NET minimal web API, use the following to register MySqlConnector:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMySqlDataSource(builder.Configuration.GetConnectionString("Default"));
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

## Keyed Services

Use the `AddKeyedMySqlDataSource` method to register a `MySqlDataSource` as a [keyed service](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8#keyed-di-services).
This is useful if you have multiple connection strings or need to connect to multiple databases.
If the service key is a string, it will automatically be used as the `MySqlDataSource` name;
to customize this, call the `AddKeyedMySqlDataSource(object?, string, Action<MySqlDataSourceBuilder>)` overload and call `MySqlDataSourceBuilder.UseName`.

```csharp
builder.Services.AddKeyedMySqlDataSource("users", builder.Configuration.GetConnectionString("Users"));
builder.Services.AddKeyedMySqlDataSource("products", builder.Configuration.GetConnectionString("Products"));

app.MapGet("/users/{userId}", async (int userId, [FromKeyedServices("users")] MySqlConnection connection) =>
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM users WHERE user_id = @userId LIMIT 1";
    command.Parameters.AddWithValue("@userId", userId);
    return $"Hello, {await command.ExecuteScalarAsync()}";
});

app.MapGet("/products/{productId}", async (int productId, [FromKeyedServices("products")] MySqlConnection connection) =>
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM products WHERE product_id = @productId LIMIT 1";
    command.Parameters.AddWithValue("@productId", productId);
    return await command.ExecuteScalarAsync();
});
```
