---
lastmod: 2016-10-16
date: 2016-10-16
menu:
  main:
    parent: api
title: MySqlTransaction
weight: 40
---

MySqlTransaction
==================

MySqlTransaction implements the [ADO.NET DbTransaction class](https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbtransaction),
please refer to its documentation.

Additionally, MySqlTransaction provides the following public properties and methods that may be used:

### Additional Instance Methods

`public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken))`

Async version of Commit
***
`public Task RollbackAsync(CancellationToken cancellationToken = default(CancellationToken))`

Async version of Rollback
***