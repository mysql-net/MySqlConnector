---
lastmod: 2016-10-16
date: 2016-10-16
menu:
  main:
    parent: getting started
title: Configuration
weight: 40
---

Configuration
============

MySqlConnector uses a connection string in order to connect to your database.

To connect to a database on `localhost` port `3306` with a user `mysqltest`, password `Password123`, and default schema `mysqldb`, the connection string would be:

`host=127.0.0.1;port=3306;user id=mysqltest;password=Password123;database=mysqldb;`

For all connection string options, view the [Connection Options Reference](connection-options/)

### Application Database Object Example

It's a good idea to use an IDisposable object that configures the connection string globally, and closes the connection automatically:

```csharp
public class AppDb : IDisposable
{
    public readonly MySqlConnection Connection;

    public AppDb()
    {
        Connection = new MySqlConnection("host=127.0.0.1;port=3306;user id=mysqltest;password=Password123;database=mysqldb;");
    }

    public void Dispose()
    {
        Connection.Close();
    }
}

```

Callers can use the Application Database Object object like so:

```csharp
public async Task AsyncMethod()
{
    using (var db = new AppDb())
    {
        await db.Connection.OpenAsync();
        // db.Connection is open and ready to use
    }
    // db.Connection was closed by AppDb.Dispose
}

public void SyncMethod()
{
    using (var db = new AppDb())
    {
        db.Connection.Open();
        // db.Connection is open and ready to use
    }
    // db.Connection was closed by AppDb.Dispose
}

```
