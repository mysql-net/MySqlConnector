---
lastmod: 2023-01-21
date: 2016-10-16
title: Home
weight: 10
menu:
  main:
    url: "/"
---

# MySqlConnector: High Performance .NET MySQL Driver

## About

MySqlConnector is a C# [ADO.NET](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/) driver for [MySQL Server](https://www.mysql.com/), [MariaDB](https://mariadb.org/),
[Amazon Aurora](https://aws.amazon.com/rds/aurora/),
[Azure Database for MySQL](https://azure.microsoft.com/en-us/products/mysql/),
[Google Cloud SQL for MySQL](https://cloud.google.com/sql/docs/mysql/),
[Percona Server](https://www.percona.com/software/mysql-database/percona-server) and more. It provides implementations of
`DbConnection`, `DbCommand`, `DbDataReader`, `DbTransaction` &mdash; the classes
needed to query and update databases from C# code.

## Getting Started

Install MySqlConnector from [NuGet](https://www.nuget.org/packages/MySqlConnector/): `dotnet add package MySqlConnector`

Connecting to your database is simple. For example, in C#:

```csharp
using var connection = new MySqlConnection("Server=myserver;User ID=mylogin;Password=mypass;Database=mydatabase");
connection.Open();

using var command = new MySqlCommand("SELECT field FROM table;", connection);
using var reader = command.ExecuteReader();
while (reader.Read())
    Console.WriteLine(reader.GetString(0));
```

For more information, see [how to install](./overview/installing/) and a [basic example](./tutorials/basic-api/) of using the API.
[Many ORMs](/overview/use-with-orms/) are supported.


### Asynchronous I/O

MySqlConnector also fully supports asynchronous I/O. The C# example above can be rewritten as:

```csharp
await using var connection = new MySqlConnection("Server=myserver;User ID=mylogin;Password=mypass;Database=mydatabase");
await connection.OpenAsync();

using var command = new MySqlCommand("SELECT field FROM table;", connection);
await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
    Console.WriteLine(reader.GetString(0));
```

## Performance

MySqlConnector outperforms Connector/NET (MySql.Data) on benchmarks:

<p><img src="https://files.logoscdn.com/v1/files/63673908/assets/13928411/content.png?signature=MVHBZxDfB0J-0Pueja8NtvuLD9A" alt="Benchmark results for MySql.Data vs MySqlConnector" width="800" height="534"></p>

(Client: MySqlConnector 2.2.0, Ubuntu 20.04, .NET 7.0; Server: Azure Database for MySQL 8.0.28, TLS 1.2)

## Why use MySqlConnector over Oracle’s MySQL Connector/NET?

MySqlConnector is a clean-room reimplementation of the [MySQL Protocol](https://dev.mysql.com/doc/internals/en/client-server-protocol.html)
and is not based on [Oracle’s MySQL Connector/NET](https://github.com/mysql/mysql-connector-net).

* **Asynchronous:** True asynchronous I/O for maximum throughput
* **Fast:** Optimized for speed and low memory usage
* **Reliable:** Fixes [dozens of open bugs](/tutorials/migrating-from-connector-net/#fixed-bugs) in Oracle’s Connector/NET; passes all [ADO.NET Specification Tests](https://mysql-net.github.io/AdoNetResults/)
* **Leading Edge:** First MySQL library to support .NET Core; uses the latest .NET features
* **Open:** MIT license; development [happens on GitHub](https://github.com/mysql-net/MySqlConnector) with publicly visible roadmaps, issues, and PRs

<table class="table table-striped table-hover" style="max-width: 650px">
  <thead>
    <th style="width:25%"></th>
    <th style="width:25%">MySqlConnector</th>
    <th style="width:25%">Oracle’s Connector/NET</th>
    <th style="width:25%">MySqlConnector Advantage</th>
  </thead>
  <tr>
    <td><strong>Async</strong></td>
    <td><strong>Fully asynchronous</strong> I/O</td>
    <td>Async calls map to synchronous I/O</td>
    <td>Uses fewer thread pool threads; higher throughput</td>
  </tr>
  <tr>
    <td><strong>Development</strong></td>
    <td>
      <strong>Open and Collaborative</strong> Development on
      <a href="https://github.com/mysql-net/MySqlConnector">GitHub</a>
    </td>
    <td>
      Closed Development Roadmap. Code is viewable on
      <a href="https://github.com/mysql/mysql-connector-net">GitHub</a>,
      some issues addressed in <a href="http://forums.mysql.com/list.php?38">forums</a>
    </td>
    <td>
      <a href="https://github.com/mysql-net/MySqlConnector/issues?q=is%3Aissue+is%3Aopen+label%3A%22up+for+grabs%22">Get involved!</a> View
      <a href="https://github.com/mysql-net/MySqlConnector/milestones">our roadmap</a>,
      discuss <a href="https://github.com/mysql-net/MySqlConnector/issues">issues</a>,
      contribute <a href="https://github.com/mysql-net/MySqlConnector/pulls">pull requests</a>
    </td>
  </tr>
  <tr>
    <td><strong>License</strong></td>
    <td>
      The <strong><a href="https://github.com/mysql-net/MySqlConnector/blob/master/LICENSE">MIT License</a></strong>
    </td>
    <td>
      <a href="http://www.gnu.org/licenses/old-licenses/gpl-2.0.html">GPLv2</a>
      with <a href="http://www.mysql.com/about/legal/licensing/foss-exception/">FOSS Exception</a>; or
      <a href="https://www.mysql.com/about/legal/licensing/oem/">commercial license</a>
    </td>
    <td>More Permissive</td>
  </tr>
</table>

