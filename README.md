# A Replacement for MySql.Data

This is an independent implementation of the core of ADO.NET: `IDbConnection`, `IDbCommand`,
`IDbDataReader`, `IDbTransaction` (plus a few helpers) — enough types to let you create and
query MySQL databases from managed code, including support for libraries such as
[Dapper](https://code.google.com/p/dapper-dot-net/).

If you’re looking for the official ADO.NET wrapper for [MySQL](https://www.mysql.com/), it can be
found at [https://github.com/mysql/mysql-connector-net](https://github.com/mysql/mysql-connector-net).

## Goals

The goals of this project are:

1. Lightweight
 * Only the core of ADO.NET is implemented, not EF or Designer types.
2. High performance
3. .NET Core support
 * It must compile and run under Core CLR.
4. Async
 * All operations should be truly asynchronous whenever possible.

## License

This library is licensed under [LGPL v3](COPYING.LESSER.md).