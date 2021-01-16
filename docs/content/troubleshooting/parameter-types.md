---
date: 2021-01-15
title: MySqlParameter Types
customtitle: "Type of value supplied to MySqlParameter.Value isn’t supported"
weight: 35
menu:
  main:
    parent: troubleshooting
---

# Type of value supplied to MySqlParameter.Value isn’t supported

## Problem

If `MySqlParameter.Value` is assigned an object of an unsupported type, executing a `MySqlCommand`
with that parameter will throw a `NotSupportedException`: “Parameter type X is not supported.”

This happens because MySqlConnector doesn’t know how the object should be serialized to bytes and
sent to the MySQL Server. Calling `ToString()` on the object as a fallback is dangerous, as many `ToString()`
implementations are culture-sensitive. Calling `ToString()` on unknown types can result in hard-to-debug
data corruption issues when culture-sensitive conversions are performed.

## Fix

Convert your object to one of the supported types from the list below.

In some cases, this may be as simple as calling `.ToString()` or `.ToString(CultureInfo.InvariantCulture)`.

## Supported Types

* .NET primitives: `bool`, `byte`, `char`, `double`, `float`, `int`, `long`, `sbyte`, `short`, `uint`, `ulong`, `ushort`
* Common types: `DateTime`, `DateTimeOffset`, `decimal`, `enum`, `Guid`, `string`, `TimeSpan`
* BLOB types: `ArraySegment<byte>`, `byte[]`, `Memory<byte>`, `ReadOnlyMemory<byte>`
* Custom MySQL types: `MySqlGeometry`, `MySqlDateTime`
