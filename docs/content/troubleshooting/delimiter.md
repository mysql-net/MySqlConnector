---
lastmod: 2023-01-28
date: 2021-07-27
title: Delimiter
customtitle: "Fix: Using DELIMITER in SQL"
description: DELIMITER $$ is unnecessary when using MySqlConnector and should be removed from SQL statements.
weight: 28
menu:
  main:
    parent: troubleshooting
---

# Fix: Using DELIMITER in SQL

In MySQL Workbench, it's common to use `DELIMITER $$` (or similar) when defining stored procedures or using other compound statements that include an embedded semicolon (`;`).

This is not required by MySQL Server, but is a workaround for [limitations in the mysql client](https://dev.mysql.com/doc/refman/8.0/en/stored-programs-defining.html):

> By default, mysql itself recognizes the semicolon as a statement delimiter, so you must redefine the delimiter temporarily to cause mysql to pass the entire stored program definition to the server.

This limitation does not exist in MySqlConnector, so using `DELIMITER` is unnecessary and it must be removed (to avoid sending invalid SQL to the server).

## Incorrect Code

```csharp
using var command = connection.CreateCommand();
command.CommandText = @"
DELIMITER $$
CREATE FUNCTION echo(
  name VARCHAR(63)
) RETURNS VARCHAR(63)
BEGIN
    RETURN name;
END
$$";
command.ExecuteNonQuery();
```

## Fixed Code

To fix the problem, remove the `DELIMITER` declaration and any trailing instances of the delimiter:

```csharp
using var command = connection.CreateCommand();
command.CommandText = @"
CREATE FUNCTION echo(
  name VARCHAR(63)
) RETURNS VARCHAR(63)
BEGIN
    RETURN name;
END;";
command.ExecuteNonQuery();
```
