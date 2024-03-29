---
title: MySqlCommand.ExecuteNonQuery method
---

# MySqlCommand.ExecuteNonQuery method

Executes this command on the associated [`MySqlConnection`](../../MySqlConnectionType/).

```csharp
public override int ExecuteNonQuery()
```

## Return Value

The number of rows affected.

## Remarks

For UPDATE, INSERT, and DELETE statements, the return value is the number of rows affected by the command. For stored procedures, the return value is the number of rows affected by the last statement in the stored procedure, or zero if the last statement is a SELECT. For all other types of statements, the return value is -1.

## See Also

* class [MySqlCommand](../../MySqlCommandType/)
* namespace [MySqlConnector](../../MySqlCommandType/)
* assembly [MySqlConnector](../../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
