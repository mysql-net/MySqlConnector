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

- `Server` connection string option may now contain multiple, comma separated hosts that will be tried in order until a connection succeeds
