# Async MySQL Connector for .NET and .NET Core

## Build Status

Ubuntu 14.04 | Windows x64
--- | ---
[![Travis CI](https://img.shields.io/travis/bgrainger/MySqlConnector.svg)](https://travis-ci.org/bgrainger/MySqlConnector) | [![AppVeyor](https://img.shields.io/appveyor/ci/BradleyGrainger/mysql-data.svg)](https://ci.appveyor.com/project/BradleyGrainger/mysql-data)

[![NuGet Pre Release](https://img.shields.io/nuget/vpre/MySqlConnector.svg?maxAge=2592000)](https://www.nuget.org/packages/MySqlConnector/)

## About

This is an [ADO.NET](https://msdn.microsoft.com/en-us/library/e80y5yhx.aspx) data
provider for [MySQL](https://www.mysql.com/). It provides implementations of
`DbConnection`, `DbCommand`, `DbDataReader`, `DbTransaction`—the classes
needed to query and update databases from managed code. It’s compatible with
popular data access libraries such as [Dapper](https://github.com/StackExchange/dapper-dot-net).

This is a clean-room reimplementation of the [MySQL Protocol](https://dev.mysql.com/doc/internals/en/client-server-protocol.html)
and is not based on the [official connector](https://github.com/mysql/mysql-connector-net). It’s
fully async, supporting the async ADO.NET methods added in .NET 4.5 without blocking
(or using `Task.Run` to run synchronous methods on a background thread). It’s also 100%
compatible with .NET Core.

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

1. **.NET Core support:** It must compile and run under .NET Core.
2. **Async:** All operations must be truly asynchronous whenever possible.
3. **High performance:** Avoid unnecessary allocations and copies when reading data.
4. **Lightweight:** Only the core of ADO.NET is implemented, not EF or Designer types.
5. **Managed:** Managed code only, no native code.

Cloning the full API of the official MySql.Data is not a goal of this project, although
it will try not to be gratuitously incompatible. For common scenarios, this package should
be a drop-in replacement.

## License

This library is licensed under the [MIT License](LICENSE).
