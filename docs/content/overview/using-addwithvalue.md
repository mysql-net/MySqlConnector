---
date: 2020-01-06
menu:
  main:
    parent: getting started
title: Using AddWithValue
weight: 70
---

# Start Using AddWithValue

There's [some advice](https://blogs.msmvps.com/jcoehoorn/blog/2014/05/12/can-we-stop-using-addwithvalue-already/) out there
that insists calling `cmd.Parameters.AddWithValue` "needs to stop".

That advice may be valid for Microsoft SQL Server, but is inapplicable to MySQL Server. If you use MySQL, **you should freely use
`cmd.Parameters.AddWithValue("@paramName", value);`**.

The primary reason that `AddWithValue` is OK to use is that MySQL's [text protocol](https://dev.mysql.com/doc/internals/en/text-protocol.html)
is not typed in a way that matters for client-side type inference.

All numbers are sent as ASCII digits (e.g., `1234`), whether they're typed as `DbType.Int32` or `MySqlDbType.NewDecimal`, or left untyped
and the value is just assigned a `long` or `float` or `decimal`. (Of course, if you're trying to store a floating point number in an integer column,
the server will have to convert/coerce it, but that isn't affected by the parameter type set in your C# code.)

Similarly, all strings are sent as UTF8-encoded bytes (e.g., `'abcd'`) regardless of the charset of the column they're being inserted into
(the server will perform a conversion if necessary).

It doesn't really matter what you set the `MySqlParameter.DbType` or `MySqlParameter.MySqlDbType` property values to, so don't worry about
it; just call `AddWithValue`, let MySqlConnector serialize the type on the wire based on its .NET type, and let MySQL Server perform
any conversion that might be necessary.
