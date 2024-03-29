---
title: MySqlDataSource.CreateConnection method
---

# MySqlDataSource.CreateConnection method

Creates a new [`MySqlConnection`](../../MySqlConnectionType/) that can connect to the database represented by this [`MySqlDataSource`](../../MySqlDataSourceType/).

```csharp
public MySqlConnection CreateConnection()
```

## Remarks

The connection must be opened before it can be used.

It is the responsibility of the caller to properly dispose the connection returned by this method. Failure to do so may result in a connection leak.

## See Also

* class [MySqlConnection](../../MySqlConnectionType/)
* class [MySqlDataSource](../../MySqlDataSourceType/)
* namespace [MySqlConnector](../../MySqlDataSourceType/)
* assembly [MySqlConnector](../../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
