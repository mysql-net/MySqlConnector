# Async MySQL Connector for .NET and .NET Core

This is an [ADO.NET](https://msdn.microsoft.com/en-us/library/e80y5yhx.aspx) data
provider for [MySQL](https://www.mysql.com/). It provides implementations of
`DbConnection`, `DbCommand`, `DbDataReader`, `DbTransaction`—the classes
needed to query and update databases from managed code.

Complete documentation is available at the [MySqlConnector Documentation Website](https://mysql-net.github.io/MySqlConnector/).

## Why Use This Library?

### Async Support

This library implements true asynchronous I/O for database operations, without blocking
(or using `Task.Run` to run synchronous methods on a background thread). This greatly
improves the throughput of a web server that performs database operations.

### Performance

This library outperforms Connector/NET (`MySql.Data`) on benchmarks:

![Benchmark 1](https://files.logoscdn.com/v1/files/12389056/content.png?signature=UE8FnU9ykb1f_7C68_D8lF2ZAzc) ![Benchmark 2](https://files.logoscdn.com/v1/files/12389051/content.png?signature=Gptw0KDjYREuulIk_37zuO6OToc)

(Client: MySqlConnector 0.44.0, Windows 10 x64; Server: MySQL Server 5.6.21, Unix)

### Bug Fixes

This library [fixes dozens of outstanding bugs](https://mysql-net.github.io/MySqlConnector/tutorials/migrating-from-connector-net/#bugs-present-in-connector-net-that-are-fixed-in-mysqlconnector) in Connector/NET.

### License

This library is [MIT-licensed](LICENSE) and may be freely distributed with commercial software.
Commercial software that uses Connector/NET may have to purchase a [commercial license](https://www.mysql.com/about/legal/licensing/oem/)
from Oracle.

## ORMs

This library is compatible with popular .NET ORMs including:

* [Dapper](https://stackexchange.github.io/Dapper/) ([GitHub](https://github.com/StackExchange/dapper-dot-net), [NuGet](https://www.nuget.org/packages/Dapper))
* [NReco.Data](https://www.nrecosite.com/dalc_net.aspx) ([GitHub](https://github.com/nreco/data), [NuGet](https://www.nuget.org/packages/NReco.Data))
* [Paradigm ORM](http://www.paradigm.net.co/) ([GitHub](https://github.com/MiracleDevs/Paradigm.ORM), [NuGet](https://www.nuget.org/packages/Paradigm.ORM.Data.MySql/))
* [ServiceStack.OrmLite](https://servicestack.net/ormlite) ([GitHub](https://github.com/ServiceStack/ServiceStack.OrmLite), [NuGet](https://www.nuget.org/packages/ServiceStack.OrmLite.MySqlConnector))
* SimpleStack.Orm ([GitHub](https://github.com/SimpleStack/simplestack.orm), [NuGet](https://www.nuget.org/packages/SimpleStack.Orm.MySQLConnector))

For Entity Framework support, use:

* Pomelo.EntityFrameworkCore.MySql ([GitHub](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql), [NuGet](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql))

## Build Status

Ubuntu 14.04 | Windows x64 | NuGet
--- | --- | ---
[![Travis CI](https://img.shields.io/travis/mysql-net/MySqlConnector/master.svg)](https://travis-ci.org/mysql-net/MySqlConnector) | [![AppVeyor](https://img.shields.io/appveyor/ci/mysqlnet/mysqlconnector/master.svg)](https://ci.appveyor.com/project/mysqlnet/mysqlconnector) | [![NuGet Pre Release](https://img.shields.io/nuget/vpre/MySqlConnector.svg)](https://www.nuget.org/packages/MySqlConnector/)

## Building

Install the latest [.NET Core](https://www.microsoft.com/net/core).

To build and run the tests, clone the repo and execute:

```
dotnet restore
dotnet test tests\MySqlConnector.Tests
```

To run the side-by-side tests, see [the instructions](tests/README.md).

## Goals

The goals of this project are:

1. **.NET Standard support:** It must run on the full .NET Framework and all platforms supported by .NET Core.
2. **Async:** All operations must be truly asynchronous whenever possible.
3. **High performance:** Avoid unnecessary allocations and copies when reading data.
4. **Lightweight:** Only the core of ADO.NET is implemented, not EF or Designer types.
5. **Managed:** Managed code only, no native code.
6. **Independent:** This is a clean-room reimplementation of the [MySQL Protocol](https://dev.mysql.com/doc/internals/en/client-server-protocol.html), not based on Connector/NET.

Cloning the full API of Connector/NET is not a goal of this project, although
it will try not to be gratuitously incompatible. For common scenarios, this package should
be a drop-in replacement.

## License

This library is licensed under the [MIT License](LICENSE).

## Contributing

If you'd like to contribute to MySqlConnector, please read our [contributing guidelines](.github/CONTRIBUTING.md).
