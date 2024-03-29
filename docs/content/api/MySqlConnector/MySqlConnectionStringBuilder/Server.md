---
title: MySqlConnectionStringBuilder.Server property
---

# MySqlConnectionStringBuilder.Server property

The host name or network address of the MySQL Server to which to connect. Multiple hosts can be specified in a comma-delimited list.

On Unix-like systems, this can be a fully qualified path to a MySQL socket file, which will cause a Unix socket to be used instead of a TCP/IP socket. Only a single socket name can be specified.

```csharp
public string Server { get; set; }
```

## See Also

* class [MySqlConnectionStringBuilder](../../MySqlConnectionStringBuilderType/)
* namespace [MySqlConnector](../../MySqlConnectionStringBuilderType/)
* assembly [MySqlConnector](../../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
