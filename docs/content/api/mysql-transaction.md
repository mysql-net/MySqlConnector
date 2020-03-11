---
lastmod: 2016-10-16
date: 2016-10-16
menu:
  main:
    parent: api
title: MySqlTransaction
weight: 50
---

MySqlTransaction
==================

MySqlTransaction implements the [ADO.NET DbTransaction class](https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbtransaction),
please refer to its documentation.

Additionally, MySqlTransaction provides the following public properties and methods that may be used:

### Additional Instance Methods

`public Task CommitAsync()`

`public Task CommitAsync(CancellationToken cancellationToken)`

Async version of Commit
***
`public Task RollbackAsync()`

`public Task RollbackAsync(CancellationToken cancellationToken)`

Async version of Rollback
***
`public Task Save(string savepointName)`

`public Task SaveAsync(string savepointName, CancellationToken cancellationToken = default)`

 Sets a named transaction savepoint with the specified `savepointName`. If the current transaction already has
 a savepoint with the same name, the old savepoint is deleted and a new one is set.
***
`public Task Release(string savepointName)`

`public Task ReleaseAsync(string savepointName, CancellationToken cancellationToken = default)`

 Removes the named transaction savepoint with the specified `savepointName`. No commit or rollback occurs.
***
***
`public Task Rollback(string savepointName)`

`public Task RollbackAsync(string savepointName, CancellationToken cancellationToken = default)`

 Rolls back the current transaction to the savepoint with the specified `savepointName` without aborting the transaction.
 The name must have been created with `Save`, but not released by calling `Release`.
***
