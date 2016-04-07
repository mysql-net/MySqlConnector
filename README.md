# WARNING

This is pre-release, alpha-quality software. There is no published NuGet package for it yet;
you must compile from source (see below).

# A Replacement for MySql.Data

This is an independent implementation of the core of ADO.NET: `DbConnection`, `DbCommand`,
`DbDataReader`, `DbTransaction` — enough types to let you create and query [MySQL](https://www.mysql.com/)
databases from managed code, including support for libraries such as
[Dapper](https://code.google.com/p/dapper-dot-net/).

The official ADO.NET wrapper for MySQL can be found at
[https://github.com/mysql/mysql-connector-net](https://github.com/mysql/mysql-connector-net).

## Goals

The goals of this project are:

1. .NET Core support
 * It must compile and run under Core CLR.
2. Async
 * All operations must be truly asynchronous whenever possible.
3. High performance
 * Avoid unnecessary allocations and copies when reading data.
4. Lightweight
 * Only the core of ADO.NET is implemented, not EF or Designer types.

Cloning the full API of the official MySql.Data is not a goal of this project, although
it will try not to be gratuitously incompatible.

## License

This library is licensed under [LGPL v3](COPYING.LESSER.md).

# Building

Install the latest [dotnet-cli](http://dotnet.github.io/).

To build and run the tests, clone the repo and execute:

```
dotnet restore
dotnet test tests\MySql.Data.Tests
```

To run the side-by-side tests, see [the instructions](tests/README.md).
