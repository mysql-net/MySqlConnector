---
lastmod: 2017-03-27
date: 2017-03-27
menu:
  main:
    parent: getting started
title: Known Issues
weight: 20
---

Known Issues
============

* The behaviour of cancellation is not well-defined in this release; cancelling a query
may leave the `MySqlConnection` in an unusable state.
* Not all MySQL data types are supported.
* Many `MySql.Data` connection string settings are not supported by this library. See
[Connection Options](connection-options/) for a list of supported options.
* Only the `mysql_native_password` and `mysql_old_password` authentication plugins are supported.
