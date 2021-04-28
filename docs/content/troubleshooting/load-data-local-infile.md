---
lastmod: 2019-08-21
date: 2019-05-23
title: Load Data Local Infile
customtitle: "Fix: Using Load Data Local Infile"
weight: 30
menu:
  main:
    parent: troubleshooting
---

# Using Load Data Local Infile

## Background

MySQL Server supports a `LOAD DATA` command that can bulk load data from a CSV or TSV file.
This normally loads data from a file on the server, but it can load from a file on the client by using
the `LOAD DATA LOCAL` statement, or by setting `MySqlBulkLoader.Local = true`.

## Errors

If you do this, you may receive one of the following errors:

* The used command is not allowed with this MySQL version
* To use MySqlBulkLoader.Local=true, set AllowLoadLocalInfile=true in the connection string.
* To use LOAD DATA LOCAL INFILE, set AllowLoadLocalInfile=true in the connection string.
* Use SourceStream or SslMode >= VerifyCA for LOAD DATA LOCAL INFILE.

## Cause

`LOAD DATA LOCAL INFILE` is disabled by default because it poses a security risk. A
malicious server or proxy could send a fake “local infile request” packet to the client and
read any file that the client has permission to open.

For more information, see [the MySQL documentation](https://dev.mysql.com/doc/refman/8.0/en/load-data-local-security.html).

## How to Fix

To allow `LOAD DATA LOCAL INFILE` to succeed, you must set `AllowLoadLocalInfile=true`
in the client’s connection string.

If you use `MySqlBulkLoader` and set `Local=true`, then everything should work by default.

If you are manually creating a `LOAD DATA LOCAL INFILE` statement, you must be connected
to a trusted server. This requires specifying `SslMode=VerifyCA` or `SslMode=VerifyFull` in the
connection string. Alternatively, rewrite the code to use `MySqlBulkLoader`.
