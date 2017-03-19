---
lastmod: 2016-10-16
date: 2016-10-16
menu:
  main:
    parent: tutorials
title: Use with .NET Core MVC
weight: 30
---

Use with .NET Core MVC
======================

This tutorial will walk through a basic .NET Core JSON API application that performs CRUD operations on
blog posts.  The code in this tutorial comes is an adaptation of [MySqlConnector.Performance](https://github.com/mysql-net/MySqlConnector/tree/master/tests/MySqlConnector.Performance),
the performance application that is used to stress test MySqlConnector.

### Initialize MySQL
Create a MySQL database and copy the following SQL to create a table called `BlogPost`:
```txt
CREATE TABLE IF NOT EXISTS `BlogPost` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `Content` longtext,
  `Title` longtext,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB;
```

### Initialize .NET Core MVC
Create a new directory for the project with a `project.json` file at the root:
```json
{
  "dependencies": {
    "Microsoft.AspNetCore.Mvc": "1.0.0",
    "Microsoft.AspNetCore.Server.Kestrel": "1.0.0",
    "Microsoft.Extensions.Configuration.EnvironmentVariables": "1.0.0",
    "Microsoft.Extensions.Configuration.FileExtensions": "1.0.0",
    "Microsoft.Extensions.Configuration.Json": "1.0.0",
    "Microsoft.Extensions.Configuration.CommandLine": "1.0.0",
    "Microsoft.Extensions.Logging": "1.0.0",
    "Microsoft.Extensions.Logging.Console": "1.0.0",
    "Microsoft.Extensions.Logging.Debug": "1.0.0",
    "Microsoft.Extensions.Options.ConfigurationExtensions": "1.0.0",
    "MySqlConnector": "0.*"
  },

  "frameworks": {
    "netcoreapp1.0": {
      "dependencies": {
        "Microsoft.NETCore.App": {
          "type": "platform",
          "version": "1.0.0"
        }
      }
    }
  },

  "runtimeOptions": {
    "gcServer": true
  },

  "buildOptions": {
    "emitEntryPoint": true,
    "preserveCompilationContext": true
  }
}
```

Run the command `dotnet restore` in this directory.

### Add Configuration Files

The first building block of our appplication is definig a couple JSON files to hold configuration:

`appsettings.json` holds .NET Core logging levels:
```json
{
    "Logging": {
        "IncludeScopes": false,
        "LogLevel": {
            "Default": "Error",
            "System": "Error",
            "Microsoft": "Error"
        }
    }
}
```

`config.json` holds the ADO.NET Connection String:
```json
{
    "Data": {
        "ConnectionString": "server=127.0.0.1;user id=mysqltest;password=test;port=3306;database=blog;"
    }
}
```

`AppConfig.cs` is a static class that builds a Configuration object from these files:
```csharp
using System.IO;
using Microsoft.Extensions.Configuration;

namespace MySqlConnector.Performance
{
    public static class AppConfig
    {
        public static IConfigurationRoot Config = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile("config.json")
        .Build();
    }
}
```

`AppDb.cs` is a disposable [Application Database Object](overview/configuration/), adapted to read the ConnectionString
from the Configuration Object:
```csharp
using System;
using MySql.Data.MySqlClient;

namespace MySqlConnector.Performance
{
    public class AppDb : IDisposable
    {

        public MySqlConnection Connection;

        public AppDb()
        {
            Connection = new MySqlConnection(AppConfig.Config["Data:ConnectionString"]);
        }

        public void Dispose()
        {
            Connection.Close();
        }
    }
}
```

### .NET Core Program.cs and Startup.cs Files

`Program.cs` contains the application entry point:
```csharp
using Microsoft.AspNetCore.Hosting;

namespace MySqlConnector.Performance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();
            host.Run();
        }
    }
}
```

`Startup.cs` contains runtime configuration and framework services:
```csharp
using System.Buffers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MySqlConnector.Performance
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            Configuration = AppConfig.Config;
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc(options =>
            {
                options.OutputFormatters.Clear();
                options.OutputFormatters.Add(new JsonOutputFormatter(new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                }, ArrayPool<char>.Shared));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseMvc();
        }
    }
}
```

Now our app is configured and we can focus on writing the core functionality!

### Models

`BlogPost.cs` represents a single Blog Post, and contains Insert, Update, and Delete methods.

```csharp
using System.Data;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace MySqlConnector.Performance.Models
{
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }

        [JsonIgnore]
        public AppDb Db { get; set; }

        public BlogPost(AppDb db=null)
        {
            Db = db;
        }

        public async Task InsertAsync()
        {
            var cmd = Db.Connection.CreateCommand() as MySqlCommand;
            cmd.CommandText = @"INSERT INTO `BlogPost` (`Title`, `Content`) VALUES (@title, @content);";
            BindParams(cmd);
            await cmd.ExecuteNonQueryAsync();
            Id = (int) cmd.LastInsertedId;
        }

        public async Task UpdateAsync()
        {
            var cmd = Db.Connection.CreateCommand() as MySqlCommand;
            cmd.CommandText = @"UPDATE `BlogPost` SET `Title` = @title, `Content` = @content WHERE `Id` = @id;";
            BindParams(cmd);
            BindId(cmd);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync()
        {
            var cmd = Db.Connection.CreateCommand() as MySqlCommand;
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
using MySql.Data.MySqlClient;

namespace MySqlConnector.Performance.Models
{
    public class BlogPostQuery
    {

        public readonly AppDb Db;
        public BlogPostQuery(AppDb db)
        {
            Db = db;
        }

        public async Task<BlogPost> FindOneAsync(int id)
        {
            var cmd = Db.Connection.CreateCommand() as MySqlCommand;
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
            var cmd = Db.Connection.CreateCommand();
            cmd.CommandText = @"SELECT `Id`, `Title`, `Content` FROM `BlogPost` ORDER BY `Id` DESC LIMIT 10;";
            return await ReadAllAsync(await cmd.ExecuteReaderAsync());
        }

        public async Task DeleteAllAsync()
        {
            var txn = await Db.Connection.BeginTransactionAsync();
            try
            {
                var cmd = Db.Connection.CreateCommand();
                cmd.CommandText = @"DELETE FROM `BlogPost`";
                await cmd.ExecuteNonQueryAsync();
                await txn.CommitAsync();
            }
            catch
            {
                await txn.RollbackAsync();
                throw;
            }
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
                        Id = await reader.GetFieldValueAsync<int>(0),
                        Title = await reader.GetFieldValueAsync<string>(1),
                        Content = await reader.GetFieldValueAsync<string>(2)
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

`AsyncController.cs` expose Async API Endpoints for CRUD operations on Blog Posts:

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector.Performance.Models;

namespace MySqlConnector.Performance.Controllers
{
    [Route("api/[controller]")]
    public class AsyncController : Controller
    {
        // GET api/async
        [HttpGet]
        public async Task<IActionResult> GetLatest()
        {
            using (var db = new AppDb())
            {
                await db.Connection.OpenAsync();
                var query = new BlogPostQuery(db);
                var result = await query.LatestPostsAsync();
                return new OkObjectResult(result);
            }
        }

        // GET api/async/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(int id)
        {
            using (var db = new AppDb())
            {
                await db.Connection.OpenAsync();
                var query = new BlogPostQuery(db);
                var result = await query.FindOneAsync(id);
                if (result == null)
                return new NotFoundResult();
                return new OkObjectResult(result);
            }
        }

        // POST api/async
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]BlogPost body)
        {
            using (var db = new AppDb())
            {
                await db.Connection.OpenAsync();
                body.Db = db;
                await body.InsertAsync();
                return new OkObjectResult(body);
            }
        }

        // PUT api/async/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOne(int id, [FromBody]BlogPost body)
        {
            using (var db = new AppDb())
            {
                await db.Connection.OpenAsync();
                var query = new BlogPostQuery(db);
                var result = await query.FindOneAsync(id);
                if (result == null)
                    return new NotFoundResult();
                result.Title = body.Title;
                result.Content = body.Content;
                await result.UpdateAsync();
                return new OkObjectResult(result);
            }
        }

        // DELETE api/async/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOne(int id)
        {
            using (var db = new AppDb())
            {
                await db.Connection.OpenAsync();
                var query = new BlogPostQuery(db);
                var result = await query.FindOneAsync(id);
                if (result == null)
                    return new NotFoundResult();
                await result.DeleteAsync();
                return new OkResult();
            }
        }

        // DELETE api/async
        [HttpDelete]
        public async Task<IActionResult> DeleteAll()
        {
            using (var db = new AppDb())
            {
                await db.Connection.OpenAsync();
                var query = new BlogPostQuery(db);
                await query.DeleteAllAsync();
                return new OkResult();
            }
        }
    }
}
```

### Run the App

Congratulations, you should have a fully functional app at this point!  You should be able to run `dotnet run` to start your application.

The following API Endpoints should work.  Note to set `Content-Type: application/json` headers on `POST` and `PUT` methods.

```txt
POST http://localhost:5000/api/async
{ "Title": "One", "Content": "First Blog Post!" }

POST http://localhost:5000/api/async
{ "Title": "Two", "Content": "Second Blog Post!" }

POST http://localhost:5000/api/async
{ "Title": "Three", "Content": "Third Blog Post!" }

GET http://localhost:5000/api/async
// Output:
[
    { "Id": 3, "Title": "Three", "Content": "Third Blog Post!" },
    { "Id": 2, "Title": "Two", "Content": "Second Blog Post!" },
    { "Id": 1, "Title": "One", "Content": "First Blog Post!"}
]

DELETE http://localhost:5000/api/async/1
// blog post 1 is gone

PUT http://localhost:5000/api/async/2
{ "Title": "Two", "Content": "Second Blog Post Revised" }

GET http://localhost:5000/api/async
// Output:
[
    { "Id": 3, "Title": "Three", "Content": "Third Blog Post!" },
    { "Id": 2, "Title": "Two", "Content": "Second Blog Post Revised" },
]

DELETE http://localhost:5000/api/async
// all blog posts are gone

GET http://localhost:5000/api/async
// Output:
[]
```

If you would like to see all of this code and more on GitHub, check out [MySqlConnector.Performance](https://github.com/mysql-net/MySqlConnector/tree/master/tests/MySqlConnector.Performance).
