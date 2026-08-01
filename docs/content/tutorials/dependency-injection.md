---
lastmod: 2026-05-25
date: 2026-05-25
menu:
  main:
    parent: tutorials
title: Dependency Injection
customtitle: Use with ASP.NET Core Dependency Injection
weight: 6
---

# Use with ASP.NET Core Dependency Injection

`MySqlConnector` includes dependency injection support in the core package. This tutorial shows how to register `MySqlDataSource` and `MySqlConnection`, customize the data source builder, and use keyed services for multiple databases.

## 1. Install MySqlConnector

Install the [MySqlConnector NuGet package](https://www.nuget.org/packages/MySqlConnector/):

```txt
dotnet add package MySqlConnector
```

If you're upgrading from the old package, uninstall `MySqlConnector.DependencyInjection` first:

```txt
dotnet remove package MySqlConnector.DependencyInjection
dotnet add package MySqlConnector
```

If `MySqlConnector` is already installed, only the `dotnet remove package MySqlConnector.DependencyInjection` command is needed.

## 2. Register a Data Source

Add `using MySqlConnector;` to `Program.cs`, then register a data source:

```csharp
builder.Services.AddMySqlDataSource(builder.Configuration.GetConnectionString("Default")!);
```

This registers `MySqlDataSource` and `MySqlConnection` with the DI container. It also uses the `ILoggerFactory` configured by ASP.NET Core automatically; for more information, see [Logging](/diagnostics/logging/).

## 3. Inject `MySqlConnection`

The registered `MySqlConnection` is transient, so it can be injected into controllers, minimal APIs, and other services:

```csharp
app.MapGet("/", async (MySqlConnection connection) =>
{
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT name FROM users LIMIT 1";
    return "Hello World: " + await command.ExecuteScalarAsync();
});
```

## 4. Inject `MySqlDataSource`

Inject `MySqlDataSource` directly if you need more than one connection or want to control when connections are opened:

```csharp
app.MapGet("/", async (MySqlDataSource dataSource) =>
{
    await using var connection1 = await dataSource.OpenConnectionAsync();
    await using var connection2 = await dataSource.OpenConnectionAsync();
    // use the two connections...
});
```

## 5. Configure `MySqlDataSourceBuilder`

Use the `AddMySqlDataSource` overload that takes `Action<MySqlDataSourceBuilder>` to configure the data source:

```csharp
builder.Services.AddMySqlDataSource(
    "Server=server;User ID=test;Password=test;Database=test",
    dataSourceBuilder => dataSourceBuilder.UseRemoteCertificateValidationCallback(
        (sender, certificate, chain, sslPolicyErrors) => { /* custom logic */ }));
```

Any `MySqlDataSourceBuilder` APIs can be used here, such as `UseName`, `UseLoggerFactory`, or TLS configuration methods.

## 6. Use Other Services During Configuration

Use the `AddMySqlDataSource` overload that takes `Action<IServiceProvider, MySqlDataSourceBuilder>` when configuration depends on other registered services:

```csharp
builder.Services.AddMySqlDataSource(
    builder.Configuration.GetConnectionString("Default")!,
    (serviceProvider, dataSourceBuilder) =>
    {
        var hostEnvironment = serviceProvider.GetRequiredService<IHostEnvironment>();
        dataSourceBuilder.UseName(hostEnvironment.ApplicationName);
    });
```

This is useful when the data source name, certificates, or other options depend on application services or environment-specific configuration.

## 7. Register Keyed Data Sources

Use `AddKeyedMySqlDataSource` to register multiple `MySqlDataSource` instances as [keyed services](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/runtime#keyed-di-services):

```csharp
builder.Services.AddKeyedMySqlDataSource("users", builder.Configuration.GetConnectionString("Users")!);
builder.Services.AddKeyedMySqlDataSource("products", builder.Configuration.GetConnectionString("Products")!);

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

If the service key is a string, it is also used as the default `MySqlDataSource` name.

## 8. Customize Keyed Data Source Names

To override the default name for a keyed data source, use the overload that accepts a `MySqlDataSourceBuilder` action and call `UseName`:

```csharp
builder.Services.AddKeyedMySqlDataSource(
    "users",
    builder.Configuration.GetConnectionString("Users")!,
    dataSourceBuilder => dataSourceBuilder.UseName("UsersPool"));
```

The data source name is used by features such as metrics; see [Metrics](/diagnostics/metrics/) for more information.
