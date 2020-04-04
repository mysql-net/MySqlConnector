---
date: 2020-04-04
menu:
  main:
    parent: api
title: MySqlBulkLoader
weight: 18
---

# MySqlBulkLoader

`MySqlBulkLoader` lets you efficiently load a MySQL Server Table with data from a CSV or TSV file or `Stream`.

Due to [security features](../troubleshooting/load-data-local-infile/) in MySQL Server, the connection string
**must** have `AllowLoadLocalInfile=true` in order to use a local source.

## Example Code

```csharp
using (var connection = new MySqlConnection("...;AllowLoadLocalInfile=True"))
{
	await connection.OpenAsync();
	var bulkLoader = new MySqlBulkLoader(connection)
	{
		FileName = @"C:\Path\To\file.csv",
		TableName = "destination",
		CharacterSet = "UTF8",
		NumberOfLinesToSkip = 1,
		FieldTerminator = ",",
		FieldQuotationCharacter = '"',
		FieldQuotationOptional = true,
		Local = true,
	}
	var rowCount = await bulkLoader.LoadAsync();
}
```

## API Reference

### Constructors

`public MySqlBulkLoader(MySqlConnection connection)`

Initializes a `MySqlBulkLoader` with the specified connection.

### Properties

`public string? CharacterSet { get; set; }`

(Optional) The character set of the source data. By default, the database's character set is used.

`public List<string> Columns { get; }`

(Optional) A list of the column names in the destination table that should be filled with data from the input file.

`public MySqlBulkLoaderConflictOption ConflictOption { get; set; }`

A `MySqlBulkLoaderConflictOption` value that specifies how conflicts are resolved (default `None`).

`public MySqlConnection Connection { get; set; }`

The `MySqlConnection` to use.

`public char EscapeCharacter { get; set; }`

(Optional) The character used to escape instances of `FieldQuotationCharacter` within field values.

`public List<string> Expressions { get; }`

(Optional) A list of expressions used to set field values from the columns in the source data.

`public char FieldQuotationCharacter { get; set; }`

(Optional) The character used to enclose fields in the source data.

`public bool FieldQuotationOptional { get; set; }`

Whether quoting fields is optional (default `false`).

`public string? FieldTerminator { get; set; }`

(Optional) The string fields are terminated with.

`public string? FileName { get; set; }`

The name of the local (if `Local` is `true`) or remote (otherwise) file to load. Either this or `SourceStream` must be set.

`public string? LinePrefix { get; set; }`

(Optional) A prefix in each line that should be skipped when loading.

`public string? LineTerminator { get; set; }`

(Optional) The string lines are terminated with.

`public bool Local { get; set; }`

Whether a local file is being used (default `true`).

`public int NumberOfLinesToSkip { get; set; }`

The number of lines to skip at the beginning of the file (default `0`).

`public MySqlBulkLoaderPriority Priority { get; set; }`

A `MySqlBulkLoaderPriority` giving the priority to load with (default `None`).

`public Stream? SourceStream { get; set; }`

A `Stream` containing the data to load. Either this or `FileName` must be set. The `Local` property must be `true` if this is set.

`public string? TableName { get; set; }`

The name of the table to load into. If this is a reserved word or contains spaces, it must be quoted.

`public int Timeout { get; set; }`

The timeout (in milliseconds) to use.

### Methods

`public int Load();`

`public Task<int> LoadAsync();`

`public Task<int> LoadAsync(CancellationToken cancellationToken);`

Loads all data in the source file or `Stream` into the destination table. Returns the number of rows inserted.
