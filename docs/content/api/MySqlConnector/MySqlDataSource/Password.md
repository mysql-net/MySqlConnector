---
title: MySqlDataSource.Password property
---

# MySqlDataSource.Password property

Sets the password that will be used by the next [`MySqlConnection`](../../MySqlConnectionType/) created from this [`MySqlDataSource`](../../MySqlDataSourceType/).

```csharp
public string Password { set; }
```

## Remarks

This can be used to update the password for database servers that periodically rotate authentication tokens, without affecting connection pooling. The [`Password`](../../MySqlConnectionStringBuilder/Password/) property must not be specified in order for this field to be used.

Consider using [`UsePeriodicPasswordProvider`](../../MySqlDataSourceBuilder/UsePeriodicPasswordProvider/) instead.

## See Also

* class [MySqlDataSource](../../MySqlDataSourceType/)
* namespace [MySqlConnector](../../MySqlDataSourceType/)
* assembly [MySqlConnector](../../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
