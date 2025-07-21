---
date: 2023-01-29
title: EF Core
customtitle: "Tutorial: Connect to MySQL with Entity Framework Core in C#"
description: How to create a C# .NET application that connects to MySQL using Entity Framework Core.
menu:
  main:
    parent: tutorials
weight: 12
---

# Tutorial: Connect to MySQL with Entity Framework Core

## Introduction

Entity Framework Core (EF Core) is Microsoft's open-source object/relational mapper that allows C# developers to work with a relational database using .NET objects.
Pomelo.EntityFrameworkCore.MySql is an EF Core provider for MySQL.
It uses MySqlConnector to provide the core database connectivity.

## 1. Create Your Project

In this tutorial, we'll use a console application to demonstrate the core concepts.
Create a console application, then add the Pomelo.EntityFrameworkCore.MySql NuGet package:

```txt
dotnet new console -o EFCoreMySQL
cd EFCoreMySQL
dotnet add package Pomelo.EntityFrameworkCore.MySql
 ```

## 2. Build Your Connection String

Build your connection string by substituting the appropriate values in this template:

```txt
Server=YOURSERVER; User ID=YOURUSERID; Password=YOURPASSWORD; Database=YOURDATABASE
```

In this tutorial application, we will store this in a constant string:

```csharp
const string connectionString = "Server=localhost; User ID=root; Password=pass; Database=blog";
```

## 3. Create a Database Context

The `BlogDataContext` is used for accessing application data through Entity Framework.
It derives from the Entity Framework `DbContext` class and has public properties for accessing data.

The `OnConfiguring()` method is used to connect to MySQL by using `options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));`.

We define two model classes, `Author` and `Post`, which will be stored in our database.

Add the following code to the end of `Program.cs`:

```csharp
public class BlogDataContext : DbContext
{
    static readonly string connectionString = "Server=localhost; User ID=root; Password=pass; Database=blog";

    public DbSet<Author> Authors { get; set; }
    public DbSet<Post> Posts { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    }
}

public class Post
{
    public int PostId { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public Author Author { get; set; }
}

public class Author
{
    public int AuthorId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }

    public List<Post> Posts { get; set; }
}
```

## 4. Create Your Database

Use [EF migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/) to create your database by running the following commands:

```
dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 5. Create and Save Data

Use LINQ queries on the properties of the `BlogDataContext` class to create and save data.
Add the following code to the top of `Program.cs` (before the `BlogDataContext` class):

```csharp
// create new blog posts
using (var context = new BlogDataContext())
{
    var john = new Author { Name = "John T. Author", Email = "john@example.com" };
    context.Authors.Add(john);

    var jane = new Author { Name = "Jane Q. Hacker", Email = "jane@example.com" };
    context.Authors.Add(jane);

    var post = new Post { Title = "Hello World", Content = "I wrote an app using EF Core!", Author = jane };
    context.Posts.Add(post);
    post = new Post { Title = "How to use EF Core", Content = "It's pretty easy", Author = john };
    context.Posts.Add(post);

    context.SaveChanges();
}

// query the blog posts, using a join between the two tables
using (var context = new BlogDataContext())
{
    var posts = context.Posts
        .Include(p => p.Author)
        .ToList();

    foreach (var post in posts)
    {
        Console.WriteLine($"{post.Title} by {post.Author.Name}");
    }
}
```

## 6. Run Your Application

Execute `dotnet run`. The program should run and print the following output:

```
How to use EF Core by John T. Author
Hello World by Jane Q. Hacker
```

## Further Reading

For more information, see:

* [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) (MSDN)
* [Pomelo.EntityFrameworkCore.MySql README](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/blob/master/README.md)
