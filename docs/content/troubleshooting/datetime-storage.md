---
date: 2020-02-08
title: DateTime Storage
weight: 24
menu:
  main:
    parent: troubleshooting
---

# DateTime Storage

MySQL cannot store all the information from a `DateTime` or `DateTimeOffset` value,
so the following considerations should be kept in mind when storing `DateTime` and
`DateTimeOffset` values in MySQL `DATETIME` or `TIMESTAMP` columns.

## MySQL Column Types

There are two MySQL column types for date values: `TIMESTAMP` and `DATETIME`.

The [MySQL documentation](https://dev.mysql.com/doc/refman/8.0/en/datetime.html) should be consulted
to understand the behavior of these two column types, particularly around:

* Range: `1970-01-01 00:00:01` to `2038-01-19 03:14:07` for `TIMESTAMP`; `1000-01-01` to `9999-12-31` for `DATETIME`.
* Time zones: `TIMESTAMP` values will be converted to UTC for storage and from UTC for retrieval, which can lead to reading different values.

## Range and DateTime.MinValue/MaxValue

`DateTime.MinValue` and `DateTime.MaxValue` both exceed the range of a `DATETIME` column
(and are well outside the range of a `TIMESTAMP` column).

`DateTime.MinValue` is `0001-01-01 00:00:00`, but the minimum supported value for a `DATETIME`
column is `1000-01-01 00:00:00`. In many versions of MySQL Server, you _can_ successfully
insert this value; however, [it is not officially supported](https://bugs.mysql.com/bug.php?id=2106).

By default, inserting `DateTime.MaxValue` into a `DATETIME` column will fail with a "datetime field
overflow" error, because the timestamp is `23:59:59.9999999` but `DATETIME` can't store fractional
seconds. To fix this, declare the column as `DATETIME(6)`.

## DateTime Notes

The `DateTime.Kind` property cannot be round-tripped. By default, all `DateTime` values read from
MySQL will have a `Kind` property of `DateTimeKind.Unspecified`.

A best practice is to ensure that only UTC values are stored in a `DATETIME` column, to avoid
data loss when reading or comparing values across different timezones. To enforce this,
set `DateTimeKind=Utc` in the connection string. When this is set, all values will be retrieved
as `DateTimeKind.Utc`, and it is an error to insert `DateTimeKind.Local` values.

Conversely, this connection string option can also be set to `DateTimeKind=Local` to force
the storage and retrieval of only local values.

## DateTimeOffset Notes

It is not possible to store a `DateTimeOffset` in a `DATETIME` column. If you create a
`MySqlParameter` with a `Value` holding a `DateTimeOffset`, only the `UtcDateTime`
property will be stored in MySQL. The recommended approach to store and retrieve
`DateTimeOffset` values is to use two columns: one for the `LocalDateTime` and one
for the `Offset`.

### DateTimeOffset Table Schema

```sql
CREATE TABLE times (
    LocalDateTime DATETIME(6),
    Offset TIME
);
```

### Storing a DateTimeOffset

```csharp
DateTimeOffset dto;
using var cmd = connection.CreateCommand();
cmd.CommandText = "insert into times(LocalDateTime, Offset) values(@LocalDateTime, @Offset);";
cmd.Parameters.AddWithValue("@LocalDateTime", dto.LocalDateTime);
cmd.Parameters.AddWithValue("@Offset", dto.Offset);
cmd.ExecuteNonQuery();
```

### Reading a DateTimeOffset

```csharp
using var cmd = connection.CreateCommand();
cmd.CommandText = "select LocalDateTime, Offset from times;";
using var reader = cmd.ExecuteReader();
while (reader.Read())
{
    var dto = new DateTimeOffset(reader.GetDateTime(0), reader.GetTimeSpan(1));
}
```
