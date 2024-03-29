---
title: MySqlDataReader.RecordsAffected property
---

# MySqlDataReader.RecordsAffected property

Gets the number of rows changed, inserted, or deleted by execution of the SQL statement.

```csharp
public override int RecordsAffected { get; }
```

## Remarks

For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command. For stored procedures, the return value is the number of rows affected by the last statement in the stored procedure, or zero if the last statement is a SELECT. For all other types of statements, the return value is -1.

## See Also

* class [MySqlDataReader](../../MySqlDataReaderType/)
* namespace [MySqlConnector](../../MySqlDataReaderType/)
* assembly [MySqlConnector](../../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
