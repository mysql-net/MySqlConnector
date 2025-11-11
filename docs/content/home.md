---
lastmod: 2025-11-11
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

await using var command = new MySqlCommand("SELECT field FROM table;", connection);
await using var reader = await command.ExecuteReaderAsync();
while (await reader.ReadAsync())
    Console.WriteLine(reader.GetString(0));
```

## Server Compatibility

MySqlConnector is compatible with the following servers.
Version numbers in **bold** indicate versions that are regularly tested by the [integration tests](https://dev.azure.com/mysqlnet/MySqlConnector/_build?definitionId=2&_a=summary) run on every commit.

Server  | Versions | Notes
--- | --- | ---
Amazon Aurora RDS | 2.x, 3.x | Use `Pipelining=False` [for Aurora 2.x](https://mysqlconnector.net/troubleshooting/aurora-freeze/)
Azure Database for MySQL | 5.7, 8.0 | Single Server and Flexible Server
Google Cloud SQL for MySQL | 5.6, 5.7, 8.0 |
MariaDB | 10.x (**10.6**, **10.11**), 11.x (**11.4**, **11.8**) |
MySQL | 5.5, 5.6, 5.7, 8.x (**8.0**, **8.4**), 9.x (**9.4**, **9.5**) | 5.5 is EOL and has some [compatibility issues](https://github.com/mysql-net/MySqlConnector/issues/1192); 5.6 and 5.7 are EOL
Percona Server | 5.6, 5.7, 8.0 |
PlanetScale | | See PlanetScale [MySQL compatibility notes](https://planetscale.com/docs/reference/mysql-compatibility)
ProxySQL | 2.x | Some [compatibility issues](https://github.com/search?q=repo%3Amysql-net%2FMySqlConnector+proxysql&type=issues)
SingleStoreDB | |
TiDB | |

## Performance

MySqlConnector outperforms Connector/NET (MySql.Data) on benchmarks:

<p><img src="https://files.logoscdn.com/v1/assets/15435916/optimized" alt="Benchmark results for MySql.Data vs MySqlConnector" width="736" height="454"></p>

(Client: MySqlConnector 2.3.1, MySql.Data 8.2.0, Ubuntu 23.04, .NET 8.0; Server: Azure Database for MySQL 8.0.34, TLS 1.2)

## Why use MySqlConnector over Oracle’s MySQL Connector/NET?

MySqlConnector is a clean-room reimplementation of the [MySQL Protocol](https://dev.mysql.com/doc/internals/en/client-server-protocol.html)
and is not based on [Oracle’s MySQL Connector/NET](https://github.com/mysql/mysql-connector-net).

See [MySqlConnector vs MySql.Data](/tutorials/migrating-from-connector-net/) for reasons to switch to MySqlConnector and details on migrating.
