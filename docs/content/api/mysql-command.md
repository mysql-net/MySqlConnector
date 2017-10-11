---
lastmod: 2017-11-06
date: 2016-10-16
menu:
  main:
    parent: api
title: MySqlCommand
weight: 20
---

MySqlCommand
==============

`MySqlCommand` implements the [ADO.NET DbCommand class](https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbcommand);
please refer to its documentation.

Additionally, `MySqlCommand` provides the following public properties and methods that may be used:

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

Holds the first automatically-generated ID for a value inserted in an `AUTO_INCREMENT` column in the last statement.
See [`LAST_INSERT_ID()`](https://dev.mysql.com/doc/refman/8.0/en/information-functions.html#function_last-insert-id) for more information.
***
