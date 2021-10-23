---
title: MySqlConnection
---

# MySqlConnection class

[`MySqlConnection`](../MySqlConnectionType/) represents a connection to a MySQL database.

```csharp
public sealed class MySqlConnection : DbConnection, ICloneable
```

## Public Members

| name | description |
| --- | --- |
| [MySqlConnection](../MySqlConnection/MySqlConnection/)() | The default constructor. |
| [MySqlConnection](../MySqlConnection/MySqlConnection/)(…) |  |
| [CanCreateBatch](../MySqlConnection/CanCreateBatch/) { get; } |  |
| override [ConnectionString](../MySqlConnection/ConnectionString/) { get; set; } |  |
| override [ConnectionTimeout](../MySqlConnection/ConnectionTimeout/) { get; } | Gets the time (in seconds) to wait while trying to establish a connection before terminating the attempt and generating an error. This value is controlled by [`ConnectionTimeout`](../MySqlConnectionStringBuilder/ConnectionTimeout/), which defaults to 15 seconds. |
| override [Database](../MySqlConnection/Database/) { get; } |  |
| override [DataSource](../MySqlConnection/DataSource/) { get; } |  |
| [ProvideClientCertificatesCallback](../MySqlConnection/ProvideClientCertificatesCallback/) { get; set; } | Gets or sets the delegate used to provide client certificates for connecting to a server. |
| [ProvidePasswordCallback](../MySqlConnection/ProvidePasswordCallback/) { get; set; } | Gets or sets the delegate used to generate a password for new database connections. |
| [RemoteCertificateValidationCallback](../MySqlConnection/RemoteCertificateValidationCallback/) { get; set; } | Gets or sets the delegate used to verify that the server's certificate is valid. |
| [ServerThread](../MySqlConnection/ServerThread/) { get; } | The connection ID from MySQL Server. |
| override [ServerVersion](../MySqlConnection/ServerVersion/) { get; } |  |
| override [State](../MySqlConnection/State/) { get; } |  |
| event [InfoMessage](../MySqlConnection/InfoMessage/) |  |
| [BeginTransaction](../MySqlConnection/BeginTransaction/)() | Begins a database transaction. |
| [BeginTransaction](../MySqlConnection/BeginTransaction/)(…) | Begins a database transaction. (2 methods) |
| [BeginTransactionAsync](../MySqlConnection/BeginTransactionAsync/)(…) | Begins a database transaction asynchronously. (3 methods) |
| override [ChangeDatabase](../MySqlConnection/ChangeDatabase/)(…) |  |
| override [ChangeDatabaseAsync](../MySqlConnection/ChangeDatabaseAsync/)(…) |  |
| [Clone](../MySqlConnection/Clone/)() |  |
| [CloneWith](../MySqlConnection/CloneWith/)(…) | Returns an unopened copy of this connection with a new connection string. If the `Password` in *connectionString* is not set, the password from this connection will be used. This allows creating a new connection with the same security information while changing other options, such as database or pooling. |
| override [Close](../MySqlConnection/Close/)() |  |
| override [CloseAsync](../MySqlConnection/CloseAsync/)() |  |
| [CreateBatch](../MySqlConnection/CreateBatch/)() | Creates a [`MySqlBatch`](../MySqlBatchType/) object for executing batched commands. |
| [CreateCommand](../MySqlConnection/CreateCommand/)() |  |
| override [DisposeAsync](../MySqlConnection/DisposeAsync/)() |  |
| override [EnlistTransaction](../MySqlConnection/EnlistTransaction/)(…) |  |
| override [GetSchema](../MySqlConnection/GetSchema/)() | Returns schema information for the data source of this [`MySqlConnection`](../MySqlConnectionType/). |
| override [GetSchema](../MySqlConnection/GetSchema/)(…) | Returns schema information for the data source of this [`MySqlConnection`](../MySqlConnectionType/). (2 methods) |
| [GetSchemaAsync](../MySqlConnection/GetSchemaAsync/)(…) | Asynchronously returns schema information for the data source of this [`MySqlConnection`](../MySqlConnectionType/). (3 methods) |
| override [Open](../MySqlConnection/Open/)() |  |
| override [OpenAsync](../MySqlConnection/OpenAsync/)(…) |  |
| [Ping](../MySqlConnection/Ping/)() |  |
| [PingAsync](../MySqlConnection/PingAsync/)(…) |  |
| [ResetConnectionAsync](../MySqlConnection/ResetConnectionAsync/)(…) | Resets the session state of the current open connection; this clears temporary tables and user-defined variables. |
| static [ClearAllPools](../MySqlConnection/ClearAllPools/)() | Clears all connection pools. |
| static [ClearAllPoolsAsync](../MySqlConnection/ClearAllPoolsAsync/)(…) | Asynchronously clears all connection pools. |
| static [ClearPool](../MySqlConnection/ClearPool/)(…) | Clears the connection pool that *connection* belongs to. |
| static [ClearPoolAsync](../MySqlConnection/ClearPoolAsync/)(…) | Asynchronously clears the connection pool that *connection* belongs to. |

## Protected Members

| name | description |
| --- | --- |
| override [DbProviderFactory](../MySqlConnection/DbProviderFactory/) { get; } |  |
| override [BeginDbTransaction](../MySqlConnection/BeginDbTransaction/)(…) | Begins a database transaction. |
| override [BeginDbTransactionAsync](../MySqlConnection/BeginDbTransactionAsync/)(…) | Begins a database transaction asynchronously. |
| override [CreateDbCommand](../MySqlConnection/CreateDbCommand/)() |  |
| override [Dispose](../MySqlConnection/Dispose/)(…) |  |

## See Also

* namespace [MySqlConnector](../../MySqlConnectorNamespace/)
* assembly [MySqlConnector](../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
