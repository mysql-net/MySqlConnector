---
lastmod: 2017-11-06
date: 2016-10-16
menu:
  main:
    parent: api
title: MySqlDataReader
weight: 30
---

MySqlDataReader
=================

MySqlDataReader implements the [ADO.NET DbDataReader class](https://docs.microsoft.com/en-us/dotnet/core/api/system.data.common.dbdatareader);
please refer to its documentation.

Additionally, MySqlDataReader provides the following public properties and methods that may be used:

### Additional Instance Methods

`public sbyte GetSByte(int ordinal)`

Gets the value of the specified column as an sbyte
***
`public DateTimeOffset GetDateTimeOffset(int ordinal)`

Gets the value of the specified column as a DateTimeOffset with an offset of 0
***
`public ReadOnlyCollection<DbColumn> GetColumnSchema()`

Implements the new [`IDbColumnSchemaGenerator.GetColumnSchema`](https://docs.microsoft.com/en-us/dotnet/api/system.data.common.idbcolumnschemagenerator.getcolumnschema) interface that returns metadata about the columns in the result set.
