---
title: MySqlConnection.ClearPoolAsync method
---

# MySqlConnection.ClearPoolAsync method

Asynchronously clears the connection pool that *connection* belongs to.

```csharp
public static Task ClearPoolAsync(MySqlConnection connection, 
    CancellationToken cancellationToken = default)
```

| parameter | description |
| --- | --- |
| connection | The [`MySqlConnection`](../../MySqlConnectionType/) whose connection pool will be cleared. |
| cancellationToken | A token to cancel the asynchronous operation. |

## Return Value

A Task representing the asynchronous operation.

## See Also

* class [MySqlConnection](../../MySqlConnectionType/)
* namespace [MySqlConnector](../../MySqlConnectionType/)
* assembly [MySqlConnector](../../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
