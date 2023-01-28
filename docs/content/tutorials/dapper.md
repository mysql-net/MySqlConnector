---
date: 2023-01-28
title: Dapper
customtitle: "Tutorial: Connect to MySQL with Dapper"
description: How to create a .NET application that connects to MySQL with MySqlConnector and Dapper.
menu:
  main:
    parent: tutorials
weight: 11
---

# Tutorial: Connect to MySQL with Dapper

## Introduction

[Dapper](https://github.com/DapperLib/Dapper/blob/main/Readme.md) is a popular "micro-ORM" for connecting to databases from .NET.
It can be used with MySqlConnector to connect to MySQL and retrieve data.
Here's how.

## 1. Create Your Project

If you don't already have a .NET project, create one using Visual Studio, or by running `dotnet new console` or `dotnet new webapi` at the command line.

## 2. Install NuGet Packages

You will need to install the following NuGet packages:

* [MySqlConnector](https://www.nuget.org/packages/MySqlConnector/): `dotnet add package MySqlConnector`
* [Dapper](https://www.nuget.org/packages/Dapper/): `dotnet add package Dapper`

## 3. Build Your Connection String

Build your connection string by substituting the appropriate values in this template:

```
Server=YOURSERVER; User ID=YOURUSERID; Password=YOURPASSWORD; Database=YOURDATABASE
```

In an ASP.NET Core Web Application or Web API, store this connection string in `appsettings.json`:

```json
{
  ....
  "ConnectionStrings": {
    "Default": "Server=YOURSERVER; User ID=YOURUSERID; Password=YOURPASSWORD; Database=YOURDATABASE"
  }
}
```

In a console application, you can store this in a constant string:

```csharp
const string connectionString = "Server=YOURSERVER; User ID=YOURUSERID; Password=YOURPASSWORD; Database=YOURDATABASE";
```

## 4. Create Your Connection

In an ASP.NET Core Web Application or Web API, you can use dependency injection to create a connection in `Program.cs`:

```csharp
builder.Services.AddTransient(x =>
  new MySqlConnection(builder.Configuration.GetConnectionString("Default")));
```

In a console application, you can create a connection in your `Main` method:

```csharp
using var connection = new MySqlConnection(connectionString);
```

## 5. Query Your Database

You can now use Dapper to query your database by using the extension methods it adds on `IDbConnection`:

For ASP.NET Core with minimal APIs in .NET 7.0:

```csharp
app.MapGet("/users/{userId}", (int userId, [FromServices] MySqlConnection connection) =>
{
    var users = connection.Query<string>("select user_name from users where user_id = @userId", new { userId });
    if (users.FirstOrDefault() is string userName)
        return Results.Ok(new { Name = userName });
    else
        return Results.NotFound();
});
```

For a console application:

```csharp
var userId = 1;
var users = connection.Query<string>("select user_name from users where user_id = @userId", new { userId });
Console.WriteLine($"Name: {users.FirstOrDefault()}");
```

## 6. Use More Dapper Features

To learn about more advanced Dapper features, such as list support and multi-mapping, see the [Dapper documentation](https://github.com/DapperLib/Dapper/blob/main/Readme.md#features).
