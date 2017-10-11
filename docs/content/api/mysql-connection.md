---
lastmod: 2017-11-06
date: 2016-10-16
menu:
  main:
    parent: api
title: MySqlConnection
weight: 10
---

MySqlConnection
=================

MySqlConnection implements the [ADO.NET DbConnection class](https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbconnection);
please refer to its documentation.

Additionally, MySqlConnection provides the following public properties and methods that may be used:

### Constructors
`public MySqlConnection()`

Parameterless constructor
***
`public MySqlConnection(string connectionString)`

Constructor that set the connection string
***
### Additional Properties
`public int ServerThread`

Connection ID from MySQL Server
***
### Additional Instance Methods
`public Task<MySqlTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))`

Async version of BeginTransaction
***
`public Task<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken))`

Async version of BeginTransaction that supports setting Isolation Level
***
### Additional Static Methods
`public static void ClearPool(MySqlConnection connection)`

Clears the connection pool that the connection belongs to
***
`public static Task ClearPoolAsync(MySqlConnection connection)`

Async version of ClearPool
***
`public static Task ClearPoolAsync(MySqlConnection connection, CancellationToken cancellationToken)`

Async version of ClearPool with cancellation token support
***
`public static void ClearAllPools()`

Clears all connection pools in the entire application
***
`public static Task ClearAllPoolsAsync()`

Async version of ClearAllPoolsAsync
***
`public static Task ClearAllPoolsAsync(CancellationToken cancellationToken)`

Async version of ClearAllPoolsAsync with cancellation token support
***
