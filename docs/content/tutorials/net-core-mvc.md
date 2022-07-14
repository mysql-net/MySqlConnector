---
lastmod: 2020-06-14
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
Run `dotnet add package MySqlConnector`. You should have a working project at this point, use `dotnet run`
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
    "ConnectionStrings": {
        "DefaultConnection": "server=127.0.0.1;user id=root;password=pass;port=3306;database=blog;"
    }
}
```

`AppDb.cs` is a disposable [Application Database Object](/overview/configuration/), adapted to read the ConnectionString
from the Configuration Object:

```csharp
using System;
using MySqlConnector;

namespace BlogPostApi
{
    public class AppDb : IDisposable
    {
        public MySqlConnection Connection { get; }

        public AppDb(string connectionString)
        {
            Connection = new MySqlConnection(connectionString);
        }

        public void Dispose() => Connection.Dispose();
    }
}
```

### .NET Core Startup

`Startup.cs` contains runtime configuration and framework services. Add this call to `ConfigureServices` to make an instance of `AppDb` available to controller methods.

```csharp
services.AddTransient<AppDb>(_ => new AppDb(Configuration["ConnectionStrings:DefaultConnection"]));
```

Now our app is configured and we can focus on writing the core functionality!

### Models

`BlogPost.cs` represents a single Blog Post, and contains Insert, Update, and Delete methods.

```csharp
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;

namespace BlogPostApi
{
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        internal AppDb Db { get; set; }

        public BlogPost()
        {
        }

        internal BlogPost(AppDb db)
        {
            Db = db;
        }

        public async Task InsertAsync()
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO `BlogPost` (`Title`, `Content`) VALUES (@title, @content);";
            BindParams(cmd);
            await cmd.ExecuteNonQueryAsync();
            Id = (int) cmd.LastInsertedId;
        }

        public async Task UpdateAsync()
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"UPDATE `BlogPost` SET `Title` = @title, `Content` = @content WHERE `Id` = @id;";
            BindParams(cmd);
            BindId(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync()
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM `BlogPost` WHERE `Id` = @id;";
            BindId(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        private void BindId(MySqlCommand cmd)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = Id,
            });
        }

        private void BindParams(MySqlCommand cmd)
        {
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@title",
                DbType = DbType.String,
                Value = Title,
            });
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@content",
                DbType = DbType.String,
                Value = Content,
            });
        }
    }
}
```

`BlogPostQuery.cs` contains commands to query Blog Posts from the database:

```csharp
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using MySqlConnector;

namespace BlogPostApi
{
    public class BlogPostQuery
    {
        public AppDb Db { get; }

        public BlogPostQuery(AppDb db)
        {
            Db = db;
        }

        public async Task<BlogPost> FindOneAsync(int id)
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `Id`, `Title`, `Content` FROM `BlogPost` WHERE `Id` = @id";
            cmd.Parameters.Add(new MySqlParameter
            {
                ParameterName = "@id",
                DbType = DbType.Int32,
                Value = id,
            });
            var result = await ReadAllAsync(await cmd.ExecuteReaderAsync());
            return result.Count > 0 ? result[0] : null;
        }

        public async Task<List<BlogPost>> LatestPostsAsync()
        {
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `Id`, `Title`, `Content` FROM `BlogPost` ORDER BY `Id` DESC LIMIT 10;";
            return await ReadAllAsync(await cmd.ExecuteReaderAsync());
        }

        public async Task DeleteAllAsync()
        {
            using var txn = await Db.Connection.BeginTransactionAsync();
            using var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM `BlogPost`";
            await cmd.ExecuteNonQueryAsync();
            await txn.CommitAsync();
        }

        private async Task<List<BlogPost>> ReadAllAsync(DbDataReader reader)
        {
            var posts = new List<BlogPost>();
            using (reader)
            {
                while (await reader.ReadAsync())
                {
                    var post = new BlogPost(Db)
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
    }
}
```

### Controller

`BlogController.cs` expose Async API Endpoints for CRUD operations on Blog Posts:

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BlogPostApi.Controllers
{
    [Route("api/[controller]")]
    public class BlogController : ControllerBase
    {
        public BlogController(AppDb db)
        {
            Db = db;
        }

        // GET api/blog
        [HttpGet]
        public async Task<IActionResult> GetLatest()
        {
            await Db.Connection.OpenAsync();
            var query = new BlogPostQuery(Db);
            var result = await query.LatestPostsAsync();
            return new OkObjectResult(result);
        }

        // GET api/blog/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            await Db.Connection.OpenAsync();
            var query = new BlogPostQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            return new OkObjectResult(result);
        }

        // POST api/blog
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]BlogPost body)
        {
            await Db.Connection.OpenAsync();
            body.Db = Db;
            await body.InsertAsync();
            return new OkObjectResult(body);
        }

        // PUT api/blog/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOne(int id, [FromBody]BlogPost body)
        {
            await Db.Connection.OpenAsync();
            var query = new BlogPostQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            result.Title = body.Title;
            result.Content = body.Content;
            await result.UpdateAsync();
            return new OkObjectResult(result);
        }

        // DELETE api/blog/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOne(int id)
        {
            await Db.Connection.OpenAsync();
            var query = new BlogPostQuery(Db);
            var result = await query.FindOneAsync(id);
            if (result is null)
                return new NotFoundResult();
            await result.DeleteAsync();
            return new OkResult();
        }

        // DELETE api/blog
        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            await Db.Connection.OpenAsync();
            var query = new BlogPostQuery(Db);
            await query.DeleteAllAsync();
            return new OkResult();
        }

        public AppDb Db { get; }
    }
}
```

### Run the App

Congratulations, you should have a fully functional app at this point!  You should be able to run `dotnet run` to start your application.

The following API Endpoints should work.  Note to set `Content-Type: application/json` headers on `POST` and `PUT` methods.

```txt
POST https://localhost:5001/api/blog
{ "Title": "One", "Content": "First Blog Post!" }

POST https://localhost:5001/api/blog
{ "Title": "Two", "Content": "Second Blog Post!" }

POST https://localhost:5001/api/blog
{ "Title": "Three", "Content": "Third Blog Post!" }

GET https://localhost:5001/api/blog
// Output:
[
    { "id": 3, "title": "Three", "content": "Third Blog Post!" },
    { "id": 2, "title": "Two", "content": "Second Blog Post!" },
    { "id": 1, "title": "One", "content": "First Blog Post!"}
]

DELETE https://localhost:5001/api/blog/1
// blog post 1 is gone

PUT https://localhost:5001/api/blog/2
{ "Title": "Two", "Content": "Second Blog Post Revised" }

GET https://localhost:5001/api/blog
// Output:
[
    { "id": 3, "title": "Three", "content": "Third Blog Post!" },
    { "id": 2, "title": "Two", "content": "Second Blog Post Revised" },
]

DELETE https://localhost:5001/api/blog
// all blog posts are gone

GET https://localhost:5001/api/blog
// Output:
[]
```
