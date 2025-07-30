---
lastmod: 2023-11-10
date: 2016-10-16
menu:
  main:
    parent: tutorials
title: Use with ASP.NET Core
customtitle: Use with ASP.NET Core Web API
weight: 30
---

# Use with ASP.NET Core Web API

This tutorial will walk through a basic ASP.NET Core JSON API application that performs CRUD operations on
blog posts.

### Initialize MySQL

Create a MySQL database and copy the following SQL to create a table called `BlogPost`:

```sql
CREATE SCHEMA blog;
USE blog;

CREATE TABLE IF NOT EXISTS `BlogPost` (
  Id INT NOT NULL AUTO_INCREMENT,
  Content LONGTEXT CHARSET utf8mb4,
  Title LONGTEXT CHARSET utf8mb4,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB;
```

### Initialize ASP.NET Core Web API

Create a folder named `BlogPostApi`, then run `dotnet new webapi` at the root to create the initial project.
Run `dotnet add package MySqlConnector.DependencyInjection`. You should have a working project at this point, use `dotnet run`
to verify the project builds and runs successfully.

### Update Configuration Files

`appsettings.json` holds .NET Core logging levels and the ADO.NET Connection String:
```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft": "Warning",
            "Microsoft.Hosting.Lifetime": "Information"
        }
    },
    "AllowedHosts": "*",
    "ConnectionStrings": {
        "Default": "Server=127.0.0.1;User ID=root;Password=pass;Port=3306;Database=blog"
    }
}
```

### .NET Core Startup

`Program.cs` contains runtime configuration and framework services.
Add this call (before `var app = builder.Build();`) to register a MySQL data source:

```csharp
builder.Services.AddMySqlDataSource(builder.Configuration.GetConnectionString("Default")!);
```

Now our app is configured and we can focus on writing the core functionality!

### Models

`BlogPost.cs` is a Plain Old C# Object that represents a single Blog Post.

```csharp
namespace BlogPostApi;

public class BlogPost
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
}
```

`BlogPostRepository.cs` contains commands to create, retrieve, update, and delete blog posts from the database:

```csharp
using System.Data.Common;
using MySqlConnector;

namespace BlogPostApi;

public class BlogPostRepository(MySqlDataSource database)
{
    public async Task<BlogPost?> FindOneAsync(int id)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT `Id`, `Title`, `Content` FROM `BlogPost` WHERE `Id` = @id";
        command.Parameters.AddWithValue("@id", id);
        var result = await ReadAllAsync(await command.ExecuteReaderAsync());
        return result.FirstOrDefault();
    }

    public async Task<IReadOnlyList<BlogPost>> LatestPostsAsync()
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"SELECT `Id`, `Title`, `Content` FROM `BlogPost` ORDER BY `Id` DESC LIMIT 10;";
        return await ReadAllAsync(await command.ExecuteReaderAsync());
    }

    public async Task DeleteAllAsync()
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"DELETE FROM `BlogPost`";
        await command.ExecuteNonQueryAsync();
    }

    public async Task InsertAsync(BlogPost blogPost)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO `BlogPost` (`Title`, `Content`) VALUES (@title, @content);";
        BindParams(command, blogPost);
        await command.ExecuteNonQueryAsync();
        blogPost.Id = (int)command.LastInsertedId;
    }

    public async Task UpdateAsync(BlogPost blogPost)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"UPDATE `BlogPost` SET `Title` = @title, `Content` = @content WHERE `Id` = @id;";
        BindParams(command, blogPost);
        BindId(command, blogPost);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DeleteAsync(BlogPost blogPost)
    {
        await using var connection = await database.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = @"DELETE FROM `BlogPost` WHERE `Id` = @id;";
        BindId(command, blogPost);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<IReadOnlyList<BlogPost>> ReadAllAsync(DbDataReader reader)
    {
        var posts = new List<BlogPost>();
        using (reader)
        {
            while (await reader.ReadAsync())
            {
                var post = new BlogPost
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Content = reader.GetString(2),
                };
                posts.Add(post);
            }
        }
        return posts;
    }

    private static void BindId(MySqlCommand cmd, BlogPost blogPost)
    {
        cmd.Parameters.AddWithValue("@id", blogPost.Id);
    }

    private static void BindParams(MySqlCommand cmd, BlogPost blogPost)
    {
        cmd.Parameters.AddWithValue("@title", blogPost.Title);
        cmd.Parameters.AddWithValue("@content", blogPost.Content);
    }
}
```

### Program.cs

`Program.cs` exposes Async API Endpoints for CRUD operations on Blog Posts (using an ASP.NET Minimal API).
Add the following methods before `app.Run();`:

```csharp
// GET api/blog
app.MapGet("/api/blog", async ([FromServices] MySqlDataSource db) =>
{
    var repository = new BlogPostRepository(db);
    return await repository.LatestPostsAsync();
});

// GET api/blog/5
app.MapGet("/api/blog/{id}", async ([FromServices] MySqlDataSource db, int id) =>
{
    var repository = new BlogPostRepository(db);
    var result = await repository.FindOneAsync(id);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

// POST api/blog
app.MapPost("/api/blog", async ([FromServices] MySqlDataSource db, [FromBody] BlogPost body) =>
{
    var repository = new BlogPostRepository(db);
    await repository.InsertAsync(body);
    return body;
});

// PUT api/blog/5
app.MapPut("/api/blog/{id}", async (int id, [FromServices] MySqlDataSource db, [FromBody] BlogPost body) =>
{
    var repository = new BlogPostRepository(db);
    var result = await repository.FindOneAsync(id);
    if (result is null)
        return Results.NotFound();
    result.Title = body.Title;
    result.Content = body.Content;
    await repository.UpdateAsync(result);
    return Results.Ok(result);
});

// DELETE api/blog/5
app.MapDelete("/api/blog/{id}", async ([FromServices] MySqlDataSource db, int id) =>
{
    var repository = new BlogPostRepository(db);
    var result = await repository.FindOneAsync(id);
    if (result is null)
        return Results.NotFound();
    await repository.DeleteAsync(result);
    return Results.NoContent();
});

// DELETE api/blog
app.MapDelete("/api/blog", async ([FromServices] MySqlDataSource db) =>
{
    var repository = new BlogPostRepository(db);
    await repository.DeleteAllAsync();
    return Results.NoContent();
});
```

### Run the App

Congratulations, you should have a fully functional app at this point!  You should be able to run `dotnet run` to start your application.

The following API Endpoints should work:

```http
@BlostPostApi_HostAddress = http://localhost:5001

GET {{BlostPostApi_HostAddress}}/api/blog
Accept: application/json
###
POST {{BlostPostApi_HostAddress}}/api/blog
Accept: application/json
Content-Type: application/json

{"title":"test", "content":"test content"}
###
PUT {{BlostPostApi_HostAddress}}/api/blog/1
Accept: application/json
Content-Type: application/json

{"title":"test put", "content":"test content updated"}
```
