---
date: 2019-06-23
menu:
  main:
    parent: api
title: MySqlBatch
weight: 10
---

# MySqlBatch

`MySqlBatch` implements the new [ADO.NET batching API](https://github.com/dotnet/corefx/issues/35135).
**It is currently experimental** and may change in the future.

When using MariaDB (10.2 or later), the commands will be sent in a single batch, reducing network
round-trip time. With other MySQL Servers, this may be no more efficient than executing the commands
individually.

## Example Code

```csharp
using (var connection = new MySqlConnection("...connection string..."))
{
	await connection.OpenAsync();
	using (var batch = new MySqlBatch(connection)
	{
		BatchCommands =
		{
			new MySqlBatchCommand("INSERT INTO departments(name) VALUES(@name);")
			{
				Parameters =
				{
					new MySqlParameter("@name", "Sales"),
				},
			},
			new MySqlBatchCommand("SET @dept_id = last_insert_id()"),
			new MySqlBatchCommand("INSERT INTO employees(name, department_id) VALUES(@name, @dept_id);")
			{
				Parameters =
				{
					new MySqlParameter("@name", "Jim Halpert"),
				},
			},
			new MySqlBatchCommand("INSERT INTO employees(name, department_id) VALUES(@name, @dept_id);")
			{
				Parameters =
				{
					new MySqlParameter("@name", "Dwight Schrute"),
				},
			},
		},
	})
	{
		await batch.ExecuteNonQueryAsync();
	}
}
```

## API Reference

### Constructors
`public MySqlBatch()`

Parameterless constructor.
***
`public MySqlBatch(MySqlConnection connection)`

Constructor that accepts a `MySqlConnection` and sets the `Connection` property.

### Properties

`public MySqlBatchCommandCollection BatchCommands { get; }`

The collection of commands that will be executed in the batch.

### Methods

`public void ExecuteNonQuery();`

`public Task ExecuteNonQueryAsync();`

Executes all the commands in the batch, returning nothing.
***

`public object ExecuteScalar();`

`public Task<object> ExecuteScalarAsync();`

Executes all the commands in the batch, returning the value from the first column in the first row of the first resultset.
***

`public MySqlDataReader ExecuteReader();`

`public Task<DbDataReader> ExecuteReaderAsync();`

Executes all the commands in the batch, return a `DbDataReader` that can iterate over the result sets. If multiple
resultsets are returned, use `DbDataReader.NextResult` (or `NextResultAsync`) to access them.
