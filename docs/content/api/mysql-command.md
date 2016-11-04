---
lastmod: 2016-10-16
date: 2016-10-16
menu:
  main:
    parent: api
title: MySqlCommand
weight: 20
---

MySqlCommand
==============

MySqlCommand implements the [ADO.NET DbCommand class](https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbcommand),
please refer to its documentation.

Additionally, MySqlCommand provides the following public properties and methods that may be used:

### Constructors
`public MySqlCommand()`

Parameterless constructor
***
`public MySqlCommand(string commandText)`

constructor accepting command SQL
***
`public MySqlCommand(MySqlConnection connection, MySqlTransaction transaction)`

constructor accepting connection object and transaction object
***
`public MySqlCommand(string commandText, MySqlConnection connection)`

constructor accepting command SQL and connection object
***
`public MySqlCommand(string commandText, MySqlConnection connection, MySqlTransaction transaction)`

constructor accepting command SQL, connection object, and transaction object
***

### Additional Properties
`public long LastInsertedId`

Holds the value of the LastInsertedId after an SQL command inserting a row has been executed
***
