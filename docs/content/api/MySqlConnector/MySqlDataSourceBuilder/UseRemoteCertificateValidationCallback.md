---
title: MySqlDataSourceBuilder.UseRemoteCertificateValidationCallback method
---

# MySqlDataSourceBuilder.UseRemoteCertificateValidationCallback method

Sets the callback used to verify that the server's certificate is valid.

```csharp
public MySqlDataSourceBuilder UseRemoteCertificateValidationCallback(
    RemoteCertificateValidationCallback callback)
```

| parameter | description |
| --- | --- |
| callback | The callback used to verify that the server's certificate is valid. |

## Return Value

This builder, so that method calls can be chained.

## Remarks

[`SslMode`](../../MySqlConnectionStringBuilder/SslMode/) must be set to Preferred or Required in order for this delegate to be invoked. See the documentation for RemoteCertificateValidationCallback for more information on the values passed to this delegate.

## See Also

* class [MySqlDataSourceBuilder](../../MySqlDataSourceBuilderType/)
* namespace [MySqlConnector](../../MySqlDataSourceBuilderType/)
* assembly [MySqlConnector](../../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
