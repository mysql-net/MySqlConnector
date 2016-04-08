# Async MySQL Connector for .NET Core

## WARNING

This is pre-release, alpha-quality software. There is no published NuGet package for it yet;
you must compile from source (see below).

## About

This is an [ADO.NET](https://msdn.microsoft.com/en-us/library/e80y5yhx.aspx) data
provider for [MySQL](https://www.mysql.com/). It provides implementations of
`DbConnection`, `DbCommand`, `DbDataReader`, `DbTransaction` — the classes
needed to query and update databases from managed code. It's compatible with
popular data access libraries such as [Dapper](https://github.com/StackExchange/dapper-dot-net).

## Build Status

Ubuntu 14.04 | Windows x64
--- | ---
[![Travis CI](https://img.shields.io/travis/bgrainger/MySql.Data.svg)](https://travis-ci.org/bgrainger/MySql.Data) | (none)

## Building

Install the latest [dotnet-cli](http://dotnet.github.io/).

To build and run the tests, clone the repo and execute:

```
dotnet restore
dotnet test tests\MySql.Data.Tests
```

To run the side-by-side tests, see [the instructions](tests/README.md).

## Goals

The goals of this project are:

1. **.NET Core support:** It must compile and run under CoreCLR.
2. **Async:** All operations must be truly asynchronous whenever possible.
3. **High performance:** Avoid unnecessary allocations and copies when reading data.
4. **Lightweight:** Only the core of ADO.NET is implemented, not EF or Designer types.

Cloning the full API of the official MySql.Data is not a goal of this project, although
it will try not to be gratuitously incompatible.

## License

This library is licensed under [LGPL v3](COPYING.LESSER.md).
