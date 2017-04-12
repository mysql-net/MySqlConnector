---
lastmod: 2016-10-16
date: 2016-10-16
menu:
  main:
    parent: tutorials
title: Migrating from Connector/NET
weight: 20
---

Migrating from Connector/NET
============================

### Connection String Differences

MySqlConnector has some different default connection string options:

<table class="table table-striped table-hover">
  <thead>
    <th style="width:20%">Option</th>
    <th style="width:20%">MySqlConnector</th>
    <th style="width:20%">Oracle's Connector/NET</th>
    <th style="width:40%">Notes</th>
  </thead>
  <tr>
    <td><code>ConnectionReset</code></td>
    <td>Default is <code>true</code></td>
    <td>Default is <code>false</code></td>
    <td>MySqlConnector takes an extra command to reset pooled connections by default so that the connection is always in a known state</td>
  </tr>
  <tr>
    <td><code>UseAffectedRows</code></td>
    <td>Default is <code>true</code></td>
    <td>Default is <code>false</code></td>
    <td>This also affects the behavior of the <code>ROW_COUNT</code> function.  <code>UseAffectedRows=true</code> is the default in most other languages (C++, PHP, others)</td>
  </tr>
</table>

Some command line options that are supported in Connector/NET are not supported in MySqlConnector.  For a full list of options that are
supported in MySqlConnector, see the [Connection Options](connection-options)

### Bugs present in Connector/NET that are fixed in MySqlConnector

* [#66476](https://bugs.mysql.com/bug.php?id=66476): Connection pool uses queue instead of stack
* [#70111](https://bugs.mysql.com/bug.php?id=70111): `Async` methods execute synchronously
* [#70686](https://bugs.mysql.com/bug.php?id=70686): `TIME(3)` and `TIME(6)` fields serialize milliseconds incorrectly
* [#73610](https://bugs.mysql.com/bug.php?id=73610): Invalid password exception has wrong number
* [#73788](https://bugs.mysql.com/bug.php?id=73788): Can't use `DateTimeOffset`
* [#77421](https://bugs.mysql.com/bug.php?id=77421): Connection is not reset when pulled from the connection pool
* [#78426](https://bugs.mysql.com/bug.php?id=78426): Unknown database exception has wrong number
* [#78760](https://bugs.mysql.com/bug.php?id=78760): Error when using tabs and newlines in SQL statements
* [#78917](https://bugs.mysql.com/bug.php?id=78917): `TINYINT(1)` values start being returned as `sbyte` after `NULL`
* [#81650](https://bugs.mysql.com/bug.php?id=81650): `Server` connection string option may now contain multiple, comma separated hosts that will be tried in order until a connection succeeds
* [#84220](https://bugs.mysql.com/bug.php?id=84220): Cannot call a stored procedure with `.` in its name
* [#85185](https://bugs.mysql.com/bug.php?id=85185): `ConnectionReset=True` does not preserve connection charset
