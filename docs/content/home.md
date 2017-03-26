---
lastmod: 2016-10-16
date: 2016-10-16
title: Home
weight: 10
menu:
  main:
    pre: "<i class='fa fa-home'></i>"
    url: ""
---

Home
====

MySqlConnector is an [ADO.NET](https://msdn.microsoft.com/en-us/library/e80y5yhx.aspx) data
provider for [MySQL](https://www.mysql.com/). It provides implementations of
`DbConnection`, `DbCommand`, `DbDataReader`, `DbTransaction` &ndash; the classes
needed to query and update databases from managed code.  Its features include:

* .NET Core Compatibility
* Truly Asynchronous: async functions implement asynchronous I/O
* High Performance: code is stress tested for performance bottlenecks
* Lightweight: Library only implements ADO.NET core

### Why use MySql over Oracle's Connector/NET?

MySqlConnector is a clean-room reimplementation of the [MySQL Protocol](https://dev.mysql.com/doc/internals/en/client-server-protocol.html)
and is not based on [Oracle's Connector/NET](https://github.com/mysql/mysql-connector-net).

<table class="table table-striped table-hover" style="max-width: 650px">
  <thead>
    <th style="width:25%"></th>
    <th style="width:25%">MySqlConnector</th>
    <th style="width:25%">Oracle's Connector/NET</th>
    <th style="width:25%">MySqlConnector Advantage</th>
  </thead>
  <tr>
    <td><strong>Async</strong></td>
    <td><strong>Fully asynchronous</strong> I/O</td>
    <td>Async calls map to synchronous I/O</td>
    <td>Uses fewer thread pool threads</td>
  </tr>
  <tr>
    <td><strong>Development</strong></td>
    <td>
      <strong>Open and Collaborative</strong> Development on
      <a href="https://github.com/mysql-net/MySqlConnector">GitHub</a>
    </td>
    <td>
      Closed Development Roadmap.  Code is viewable on
      <a href="https://github.com/mysql/mysql-connector-net">GitHub</a>,
      some issues addressed in <a href="http://forums.mysql.com/list.php?38">Forums</a>
    </td>
    <td>
      Get involved!  View
      <a href="https://github.com/mysql-net/MySqlConnector/projects/1">our roadmap</a>,
      discuss <a href="https://github.com/mysql-net/MySqlConnector/issues">issues</a>,
      contribute <a href="https://github.com/mysql-net/MySqlConnector/pulls">pull requests</a>
    </td>
  </tr>
  <tr>
    <td><strong>License</strong></td>
    <td>
      The <strong><a href="https://github.com/mysql-net/MySqlConnector/blob/master/LICENSE">MIT License</a></strong>
    </td>
    <td>
      <a href="http://www.gnu.org/licenses/old-licenses/gpl-2.0.html">GPLv2</a>
      with <a href="http://www.mysql.com/about/legal/licensing/foss-exception/">FOSS Exception</a>
    </td>
    <td>More Permissive</td>
  </tr>
</table>

