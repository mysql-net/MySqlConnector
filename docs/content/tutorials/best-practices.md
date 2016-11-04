---
lastmod: 2016-10-16
date: 2016-10-16
menu:
  main:
    parent: tutorials
title: Best Practices
weight: 10
---

Best Practices
=============

## Asynchronous Operation

MySqlConnector is fully asynchronous, supporting the async ADO.NET methods added in .NET 4.5 without blocking
or using `Task.Run` to run synchronous methods on a background thread.  Programmers implementing MySqlConnector
should be familiar with [Async/Await - Best Practices in Asynchronous Programming](https://msdn.microsoft.com/en-us/magazine/jj991977.aspx).

### Always Use Async when possible

<table class="table table-bordered table-head-centered" style="max-width: 650px">
  <thead>
    <th style="width:30%">ADO.NET Class</th>
    <th class="success" style="width:40%">Asynchronous Method<br />(always use when possible)</th>
    <th class="warning" style="width:30%">Synchronous Method<br />(avoid when possible)</th>
  </thead>
  <tr>
    <td rowspan="2" style="vertical-align:middle">
      <a href="https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbconnection">DbConnection</a>
    </td>
    <td>OpenAsync</td>
    <td>Open</td>
  </tr>
  <tr>
    <td>
      <span class="text-danger">*</span><a href="api/mysql-connection">MySqlConnection</a>.BeginTransactionAsync
    </td>
    <td>BeginTransaction</td>
  </tr>
  <tr>
    <td rowspan="3" style="vertical-align:middle">
      <a href="https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbcommand">DbCommand</a>
    </td>
    <td>ExecuteNonQueryAsync</td>
    <td>ExecuteNonQuery</td>
  </tr>
  <tr>
    <td>ExecuteReaderAsync</td>
    <td>ExecuteReader</td>
  </tr>
  <tr>
    <td>ExecuteScalarAsync</td>
    <td>ExecuteScalar</td>
  </tr>
  <tr>
    <td rowspan="4" style="vertical-align:middle">
      <a href="https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbdatareader">DbDataReader</a>
    </td>
    <td>GetFieldValueAsync</td>
    <td>GetFieldValue</td>
  </tr>
  <tr>
    <td>IsDBNullAsync</td>
    <td>IsDBNull</td>
  </tr>
  <tr>
    <td>NextResultAsync</td>
    <td>NextResult</td>
  </tr>
  <tr>
    <td>ReadAsync</td>
    <td>Read</td>
  </tr>
  <tr>
    <td rowspan="2" style="vertical-align:middle">
      <a href="https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbtransaction">DbTransaction</a>
    </td>
    <td>
      <span class="text-danger">*</span><a href="api/mysql-transaction">MySqlTransaction</a>.CommitAsync
    </td>
    <td>Commit</td>
  </tr>
  <tr>
    <td>
      <span class="text-danger">*</span><a href="api/mysql-transaction">MySqlTransaction</a>.RollbackAsync
    </td>
    <td>Rollback</td>
  </tr>
</table>

<span class="text-danger">*</span>Async Transaction methods are not part of ADO.NET, they are provided by
MySqlConnector to allow database code to remain fully asynchronous.

### Example Console Application

In order to get the full benefit of asynchronous operation, every method in the call stack that eventually calls
MySqlConnector should be implemented as an async method.  The exception is the `static void Main` method in a Console Application.

Example assumes a [configured AppDb](overview/configuration) object in the `MySqlConnector.Examples` namespace.

```csharp
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MySqlConnector.Examples
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            var tasks = new List<Task>();
            for (var i=0; i<100; i++){
                tasks.Add(Controllers.SleepOne());
            }
            // these 100 queries should all complete in around
            // 1 second if "Max Pool Size=100" (the default)
            await Task.WhenAll(tasks);
        }
    }

    public class Controllers
    {
        public static async Task SleepOne()
        {
            using (var db = new AppDb())
            {
                await db.Connection.OpenAsync();
                using (var cmd = db.Connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT SLEEP(1)";
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
```


## Synchronous Operation

<div class="alert alert-warning">
  Using Synchronous Methods can have adverse effects on the managed thread pool and cause slowdowns or lock-ups
  if not properly tuned.  The recommended approach is to use all Asynchronous Methods.
</div>

If you must use synchronous methods, ensure that your thread pool is at least the size of the number of
concurrent connections you plan to support.  For example, if you are creating a web server using
synchronous methods that needs to support serving 500 Requests Per Second, set the minimum thread
pool size to 500.

Example `project.json` configuration:

```json
{
  "runtimeOptions": {
    "configProperties": {
      "System.GC.Server": true,
      "System.GC.Concurrent": true,
      "System.Threading.ThreadPool.MinThreads": 500
    }
  },
  // other config
}
```
