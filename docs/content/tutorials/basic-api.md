---
lastmod: 2023-01-21
date: 2018-06-05
menu:
  main:
    parent: tutorials
title: Basic API
weight: 5
---

Basic API
=========

MySqlConnector aims to be fully ADO.NET-compatible; its API should feel almost identical to other .NET database drivers.
Here’s a basic code snippet to get you started.

```csharp
var connString = "Server=myserver;User ID=mylogin;Password=mypass;Database=mydatabase";

await using var connection = new MySqlConnection(connString);
await connection.OpenAsync();

// Insert some data
using (var cmd = new MySqlCommand())
{
    cmd.Connection = connection;
    cmd.CommandText = "INSERT INTO data (some_field) VALUES (@p)";
    cmd.Parameters.AddWithValue("p", "Hello world");
    await cmd.ExecuteNonQueryAsync();
}

// Retrieve all rows
await using var command = new MySqlCommand("SELECT some_field FROM data", connection);
await using var reader = await cmd.ExecuteReaderAsync();
while (await reader.ReadAsync())
    Console.WriteLine(reader.GetString(0));
```

You can find more info about the ADO.NET API in the [MSDN documentation](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/ado-net-overview) or in many tutorials on the Internet.
