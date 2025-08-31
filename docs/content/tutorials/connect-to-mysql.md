---
lastmod: 2023-11-10
date: 2019-11-18
menu:
  main:
    parent: tutorials
title: Connect to MySQL
customtitle: How to Connect to MySQL from .NET Core
weight: 3
---

# How to Connect to MySQL from .NET Core

This tutorial will teach you how to connect to MySQL from .NET Core using C#.

## 1. Install MySqlConnector

First, install the [MySqlConnector NuGet package](https://www.nuget.org/packages/MySqlConnector/). From
a command prompt, run:

```txt
dotnet add package MySqlConnector
```

Or right-click your project, choose **Manage NuGet Packages...**, in the **Search** box enter
`MySqlConnector`, and install the package in your project.

## 2. Connection String

A typical connection string for MySQL is:

```
Server=YOURSERVER;User ID=YOURUSERID;Password=YOURPASSWORD;Database=YOURDATABASE
```

Replace the values in that string with the appropriate settings for your database. For more advanced
settings, see [Connection Options](/connection-options/).

If you are using ASP.NET Core, your connection string will usually be stored in `appsettings.json`:

```json
{
  ....
  "ConnectionStrings": {
    "Default": "Server=YOURSERVER;User ID=YOURUSERID;Password=YOURPASSWORD;Database=YOURDATABASE"
  }
}
```

## 3. Configure Service (ASP.NET Core)

If using ASP.NET Core, you will want to register a database connection in `Program.cs`:

**Recommended** Install [MySqlConnector.DependencyInjection](https://www.nuget.org/packages/MySqlConnector.DependencyInjection/) via `dotnet add package MySqlConnector.DependencyInjection`. Then add the following to `Program.cs`:

```csharp
builder.Services.AddMySqlDataSource(builder.Configuration.GetConnectionString("Default")!);
```

Alternatively, register a transient `MySqlConnection` object explicitly:

```csharp
builder.services.AddTransient<MySqlConnection>(_ =>
    new MySqlConnection(builder.Configuration.GetConnectionString["Default"]));
```

The advantage of using `AddMySqlDataSource` is that it will automatically integrate with logging, and also register `DbDataSource` and `DbConnection` with the service collection.

## 4. Open and Use the Connection

In ASP.NET Core, the `MySqlConnection` object will be dependency-injected into your `Controller` class. For
other kinds of projects, you may need to explicitly create the connection:

```csharp
await using var connection = new MySqlConnection(yourConnectionString);
```

You can then open the connection and execute a query:

```csharp
await connection.OpenAsync();

await using var command = new MySqlCommand("SELECT field FROM table;", connection);
await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    var value = reader.GetValue(0);
    // do something with 'value'
}
```
