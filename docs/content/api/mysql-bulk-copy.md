---
lastmod: 2020-04-04
date: 2019-11-11
menu:
  main:
    parent: api
title: MySqlBulkCopy
weight: 15
---

# MySqlBulkCopy

`MySqlBulkCopy` lets you efficiently load a MySQL Server Table with data from another source.
It is similar to the [`SqlBulkCopy`](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlbulkcopy) class
for SQL Server.

Due to [security features](../troubleshooting/load-data-local-infile/) in MySQL Server, the connection string
**must** have `AllowLoadLocalInfile=true` in order to use this class.

`MySqlBulkCopy` currently requires that the source data (`DataTable` or `IDataReader`) contain all
the columns of the destination table in the same order.

For data that is in CSV or TSV format, use [`MySqlBulkLoader`](api/mysql-bulk-loader/) to bulk load the file.

**Note:** This API is a unique feature of MySqlConnector; you must [switch to MySqlConnector](../../overview/installing/)
in order to use it. It is supported in version 0.62.0 and later.

## Example Code

```csharp
// NOTE: to copy data between tables in the same database, use INSERT ... SELECT
// https://dev.mysql.com/doc/refman/8.0/en/insert-select.html
var dataTable = GetDataTableFromExternalSource();

using (var connection = new MySqlConnection("...;AllowLoadLocalInfile=True"))
{
	await connection.OpenAsync();
	var bulkCopy = new MySqlBulkCopy(connection);
	bulkCopy.DestinationTableName = "some_table_name";
	await bulkCopy.WriteToServerAsync(dataTable);
}
```

## API Reference

### Constructors

`public MySqlBulkCopy(MySqlConnection connection, MySqlTransaction transaction = null)`

Initializes a `MySqlBulkCopy` with the specified connection, and optionally the active transaction.

### Properties

`public int BulkCopyTimeout { get; set; }`

The number of seconds for the operation to complete before it times out, or `0` for no timeout.

`public string DestinationTableName { get; set; }`

Name of the destination table on the server. (This name shouldn't be quoted or escaped.)

`public int NotifyAfter { get; set; }`

If non-zero, this defines the number of rows to be processed before generating a notification event.

### Methods

`public void WriteToServer(DataTable dataTable);`

`public Task WriteToServerAsync(DataTable dataTable, CancellationToken cancellationToken = default);`

Copies all rows in the supplied `DataTable` to the destination table specified by the `DestinationTableName` property of the `MySqlBulkCopy` object.
(This method is not available on `netstandard1.3`.)

***

`public void WriteToServer(IEnumerable<DataRow> dataRows, int columnCount)`

`public async Task WriteToServerAsync(IEnumerable<DataRow> dataRows, int columnCount, CancellationToken cancellationToken = default)`

Copies all rows in the supplied sequence of `DataRow` objects to the destination table specified by the `DestinationTableName` property of the `MySqlBulkCopy` object. The number of columns to be read from the `DataRow` objects must be specified in advance.
(This method is not available on `netstandard1.3`.)

***

`public void WriteToServer(IDataReader dataReader);`

`public Task WriteToServerAsync(IDataReader dataReader, CancellationToken cancellationToken = default);`

Copies all rows in the supplied `IDataReader` to the destination table specified by the `DestinationTableName` property of the `MySqlBulkCopy` object.

### Events

`public event MySqlRowsCopiedEventHandler RowsCopied;`

If `NotifyAfter` is non-zero, this event will be raised every time the number of rows specified by
`NotifyAfter` have been processed, and once after all rows have been copied (but duplicate events
will not be raised).

Receipt of a `RowsCopied` event does not imply that any rows have been sent to the server or committed.

The `MySqlRowsCopiedEventArgs.Abort` property can be set to `true` by the event handler to abort
the copy.
