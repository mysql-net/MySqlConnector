---
lastmod: 2019-06-23
date: 2016-10-16
menu:
  main:
    parent: api
title: MySqlConnection
weight: 30
---

MySqlConnection
=================

MySqlConnection implements the [ADO.NET DbConnection class](https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbconnection);
please refer to its documentation.

Additionally, MySqlConnection provides the following public properties and methods that may be used:

### Constructors
`MySqlConnection()`

Parameterless constructor.
***
`MySqlConnection(string connectionString)`

Constructor that sets the `ConnectionString` property.

### Additional Properties
`int ServerThread`

Connection ID from MySQL Server.
***
`bool CanCreateBatch`

Returns `true`.

### Additional Instance Methods
`Task<MySqlTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))`

Async version of `BeginTransaction`.
***
`Task<MySqlTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default(CancellationToken))`

Async version of `BeginTransaction` that supports setting Isolation Level.
***
`MySqlBatch CreateBatch()`

Creates a `MySqlBatch` object for executing batched commands.
***
`MySqlBatchCommand CreateBatchCommand()`

Creates a `MySqlBatchCommand` object (that can be used with `MySqlBatch.BatchCommands`).

### Additional Static Methods
`static void ClearPool(MySqlConnection connection)`

Clears the connection pool that the connection belongs to.
***
`static Task ClearPoolAsync(MySqlConnection connection)`

Async version of `ClearPool`.
***
`static Task ClearPoolAsync(MySqlConnection connection, CancellationToken cancellationToken)`

Async version of `ClearPool` with cancellation token support.
***
`static void ClearAllPools()`

Clears all connection pools in the entire application.
***
`static Task ClearAllPoolsAsync()`

Async version of `ClearAllPoolsAsync`.
***
`static Task ClearAllPoolsAsync(CancellationToken cancellationToken)`

Async version of `ClearAllPoolsAsync` with cancellation token support.
