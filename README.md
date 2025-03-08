# MySQL Connector for .NET and .NET Core

[![NuGet](https://img.shields.io/nuget/vpre/MySqlConnector.svg)](https://www.nuget.org/packages/MySqlConnector/)

This is an [ADO.NET](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/) data
provider for [MySQL](https://www.mysql.com/) and other compatible servers including [MariaDB](https://www.mariadb.org).
It provides implementations of
`DbConnection`, `DbCommand`, `DbDataReader`, `DbTransaction`—the classes
needed to query and update databases from managed code.

Complete documentation is available at the [MySqlConnector Documentation Website](https://mysqlconnector.net/).

## Why Use This Library?

### Performance

This library outperforms MySQL Connector/NET (`MySql.Data`) on benchmarks:

![Benchmark](https://files.logoscdn.com/v1/assets/15435916/optimized)

(Client: MySqlConnector 2.3.1, MySql.Data 8.2.0, Ubuntu 23.04, .NET 8.0; Server: Azure Database for MySQL 8.0.34, TLS 1.2)

### Server Compatibility

This library is compatible with [many MySQL-compatible servers](https://mysqlconnector.net/#server-compatibility), including MySQL 5.5 and newer and MariaDB 10.x and newer.
MySql.Data [only supports MySQL Server](https://bugs.mysql.com/bug.php?id=109331).

### Bug Fixes

This library [fixes dozens of outstanding bugs](https://mysqlconnector.net/tutorials/migrating-from-connector-net/#fixed-bugs) in Connector/NET.

### Cutting Edge

This library implements the latest ADO.NET APIs, from async (introduced in .NET Framework 4.5), through `DbBatch` (.NET 6.0) and `DbDataSource` (.NET 7.0).

### License

This library is [MIT-licensed](LICENSE) and may be freely distributed with commercial software.
Commercial software that uses Connector/NET may have to purchase a [commercial license](https://www.mysql.com/about/legal/licensing/oem/)
from Oracle.

## Related Projects

This library is compatible with popular .NET ORMs including:

* [Dapper](https://dapperlib.github.io/Dapper/) ([GitHub](https://github.com/DapperLib/Dapper), [NuGet](https://www.nuget.org/packages/Dapper))
* [Faithlife.Data](https://faithlife.github.io/FaithlifeData/) ([GitHub](https://github.com/Faithlife/FaithlifeData), [NuGet](https://www.nuget.org/packages/Faithlife.Data))
* [FreeSql](https://freesql.net/) ([GitHub](https://github.com/dotnetcore/FreeSql), [NuGet](https://www.nuget.org/packages/FreeSql.Provider.MySqlConnector/))
* [LINQ to DB](https://linq2db.github.io/) ([GitHub](https://github.com/linq2db/linq2db), [NuGet](https://www.nuget.org/packages/linq2db.MySqlConnector)) including ClickHouse support
* [NHibernate](https://nhibernate.info/) ([GitHub](https://github.com/nhibernate/NHibernate.MySqlConnector), [NuGet](https://www.nuget.org/packages/NHibernate.Driver.MySqlConnector))
* [NReco.Data](https://www.nrecosite.com/dalc_net.aspx) ([GitHub](https://github.com/nreco/data), [NuGet](https://www.nuget.org/packages/NReco.Data))
* [Paradigm ORM](https://www.paradigm.net.co/) ([GitHub](https://github.com/MiracleDevs/Paradigm.ORM), [NuGet](https://www.nuget.org/packages/Paradigm.ORM.Data.MySql/))
* [RepoDb](https://repodb.net/) ([GitHub](https://github.com/mikependon/RepoDb/tree/master/RepoDb.MySqlConnector), [NuGet](https://www.nuget.org/packages/RepoDb.MySqlConnector))
* [ServiceStack.OrmLite](https://servicestack.net/ormlite) ([GitHub](https://github.com/ServiceStack/ServiceStack.OrmLite), [NuGet](https://www.nuget.org/packages/ServiceStack.OrmLite.MySqlConnector))
* [SimpleStack.Orm](https://simplestack.org/) ([GitHub](https://github.com/SimpleStack/simplestack.orm), [NuGet](https://www.nuget.org/packages/SimpleStack.Orm.MySQLConnector))

For Entity Framework support, use:

* Pomelo.EntityFrameworkCore.MySql ([GitHub](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql), [NuGet](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql))

For ASP.NET Core health checks, use:

* AspNetCore.HealthChecks.MySql ([GitHub](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks), [NuGet](https://www.nuget.org/packages/AspNetCore.HealthChecks.MySql))

## Build Status

[![AppVeyor](https://img.shields.io/appveyor/ci/mysqlnet/mysqlconnector/master.svg)](https://ci.appveyor.com/project/mysqlnet/mysqlconnector)
[![Azure Pipelines](https://dev.azure.com/mysqlnet/MySqlConnector/_apis/build/status/mysql-net.MySqlConnector?branchName=master)](https://dev.azure.com/mysqlnet/MySqlConnector/_build/latest?definitionId=2&branchName=master)

## Building

Install the latest [.NET](https://dotnet.microsoft.com/en-us/download).

To build and run the tests, clone the repo and execute:

```
dotnet restore
dotnet test tests\MySqlConnector.Tests
```

To run the integration tests, see [the instructions](tests/README.md).

## Goals

The goals of this project are:

1. **.NET Standard support:** It must run on the full .NET Framework and all platforms supported by .NET Core.
2. **Async:** All operations must be truly asynchronous whenever possible.
3. **High performance:** Avoid unnecessary allocations and copies when reading data.
4. **Lightweight:** Only the core of ADO.NET is implemented, not EF or Designer types.
5. **Managed:** Managed code only, no native code.
6. **Independent:** This is a clean-room reimplementation of the [MySQL Protocol](https://dev.mysql.com/doc/internals/en/client-server-protocol.html), not based on Connector/NET.

Cloning the full API of Connector/NET is not a goal of this project, although
it will try not to be gratuitously incompatible. For typical scenarios, [migrating to this package](https://mysqlconnector.net/tutorials/migrating-from-connector-net/) should
be easy.

## License

This library is licensed under the [MIT License](LICENSE).

## Contributing

If you'd like to contribute to MySqlConnector, please read our [contributing guidelines](.github/CONTRIBUTING.md).

## Acknowledgements

Development of MySqlConnector is supported by:

[![Devolutions](https://webdevolutions.blob.core.windows.net/images/projects/devolutions/logos/devolutions-icon-shadow.svg)](https://devolutions.net/)

[Devolutions](https://devolutions.net/)

[![Faithlife](https://files.logoscdn.com/v1/files/4319104/content.svg?signature=3szVb3XmOfYMAxIv-LmuNYL_290)](https://faithlife.com/about)

[Faithlife](https://faithlife.com/about) ([View jobs](https://faithlife.com/careers))
