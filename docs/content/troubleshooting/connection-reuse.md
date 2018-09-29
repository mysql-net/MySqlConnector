---
lastmod: 2018-09-29
date: 2018-09-29
title: Connection Reuse
weight: 10
menu:
  main:
    parent: troubleshooting
---

# Connection Reuse

A `MySqlConnection` object may only be used for one operation at a time. It may not be shared
across multiple threads and used simultaneously, nor reused on the same thread while there is
an open `MySqlDataReader`.

## Examples of Prohibited Use

### Multiple Threads

You may not execute multiple operations in parallel, for example:

```csharp
using (var connection = new MySqlConnection("..."))
{
    await connection.OpenAsync();
    await Task.WhenAll( // don't do this
        connection.ExecuteAsync("SELECT 1;"),
        connection.ExecuteAsync("SELECT 2;"),
        connection.ExecuteAsync("SELECT 3;"));
}
```

### Nested Access on Single Thread

You may not reuse the connection when there is an open `MySqlDataReader:`

```csharp
using (var connection = CreateOpenConnection())
using (var command = new MySqlCommand("SELECT id FROM ...", connection))
using (var reader = command.ExecuteReader())
{
    while (reader.Read())
    {
        var idToUpdate = reader.GetValue(0);
        connection.Execute("UPDATE ... SET ..."); // don't do this
    }
}
```

## How to Fix

For the multithreaded scenario, if concurrent access to the database is truly necessary,
create and open a new `MySqlConnection` on each thread. But in most cases, you should
just write code that sequentially `await`s each asychronous operation (without performing
them in parallel).


For the nested access, read all the values from the `MySqlDataReader` into memory, close
the reader, then process the values. (If the data set is large, you may need to use a batching
approach where you read a limited number of rows in each batch.)