---
title: MySqlConnectionStringBuilder
---

# MySqlConnectionStringBuilder class

[`MySqlConnectionStringBuilder`](../MySqlConnectionStringBuilderType/) allows you to construct a MySQL connection string by setting properties on the builder then reading the ConnectionString property.

```csharp
public sealed class MySqlConnectionStringBuilder : DbConnectionStringBuilder
```

## Public Members

| name | description |
| --- | --- |
| [MySqlConnectionStringBuilder](../MySqlConnectionStringBuilder/MySqlConnectionStringBuilder/)() | Initializes a new [`MySqlConnectionStringBuilder`](../MySqlConnectionStringBuilderType/). |
| [MySqlConnectionStringBuilder](../MySqlConnectionStringBuilder/MySqlConnectionStringBuilder/)(…) | Initializes a new [`MySqlConnectionStringBuilder`](../MySqlConnectionStringBuilderType/) with properties set from the specified connection string. |
| [AllowLoadLocalInfile](../MySqlConnectionStringBuilder/AllowLoadLocalInfile/) { get; set; } | Allows the `LOAD DATA LOCAL` command to request files from the client. |
| [AllowPublicKeyRetrieval](../MySqlConnectionStringBuilder/AllowPublicKeyRetrieval/) { get; set; } | Allows the client to automatically request the RSA public key from the server. |
| [AllowUserVariables](../MySqlConnectionStringBuilder/AllowUserVariables/) { get; set; } | Allows user-defined variables (prefixed with `@`) to be used in SQL statements. |
| [AllowZeroDateTime](../MySqlConnectionStringBuilder/AllowZeroDateTime/) { get; set; } | Returns `DATETIME` fields as [`MySqlDateTime`](../MySqlDateTimeType/) objects instead of DateTime objects. |
| [ApplicationName](../MySqlConnectionStringBuilder/ApplicationName/) { get; set; } | Sets the `program_name` connection attribute passed to MySQL Server. |
| [AutoEnlist](../MySqlConnectionStringBuilder/AutoEnlist/) { get; set; } | Automatically enlists this connection in any active TransactionScope. |
| [CancellationTimeout](../MySqlConnectionStringBuilder/CancellationTimeout/) { get; set; } | The length of time (in seconds) to wait for a query to be canceled when [`CommandTimeout`](../MySqlCommand/CommandTimeout/) expires, or zero for no timeout. |
| [CertificateFile](../MySqlConnectionStringBuilder/CertificateFile/) { get; set; } | The path to a certificate file in PKCS #12 (.pfx) format containing a bundled Certificate and Private Key used for mutual authentication. |
| [CertificatePassword](../MySqlConnectionStringBuilder/CertificatePassword/) { get; set; } | The password for the certificate specified using the [`CertificateFile`](../MySqlConnectionStringBuilder/CertificateFile/) option. Not required if the certificate file is not password protected. |
| [CertificateStoreLocation](../MySqlConnectionStringBuilder/CertificateStoreLocation/) { get; set; } | Uses a certificate from the specified Certificate Store on the machine. The default value of None means the certificate store is not used; a value of CurrentUser or LocalMachine uses the specified store. |
| [CertificateThumbprint](../MySqlConnectionStringBuilder/CertificateThumbprint/) { get; set; } | Specifies which certificate should be used from the Certificate Store specified in [`CertificateStoreLocation`](../MySqlConnectionStringBuilder/CertificateStoreLocation/). This option must be used to indicate which certificate in the store should be used for authentication. |
| [CharacterSet](../MySqlConnectionStringBuilder/CharacterSet/) { get; set; } | Supported for backwards compatibility; MySqlConnector always uses `utf8mb4`. |
| [ConnectionIdlePingTime](../MySqlConnectionStringBuilder/ConnectionIdlePingTime/) { get; set; } | The delay (in seconds) before idle connections are pinged (to determine liveness) when being retrieved from the pool. |
| [ConnectionIdleTimeout](../MySqlConnectionStringBuilder/ConnectionIdleTimeout/) { get; set; } | The amount of time (in seconds) that a connection can remain idle in the pool. |
| [ConnectionLifeTime](../MySqlConnectionStringBuilder/ConnectionLifeTime/) { get; set; } | The maximum lifetime (in seconds) for any connection, or `0` for no lifetime limit. |
| [ConnectionProtocol](../MySqlConnectionStringBuilder/ConnectionProtocol/) { get; set; } | The protocol to use to connect to the MySQL Server. |
| [ConnectionReset](../MySqlConnectionStringBuilder/ConnectionReset/) { get; set; } | Whether connections are reset when being retrieved from the pool. |
| [ConnectionTimeout](../MySqlConnectionStringBuilder/ConnectionTimeout/) { get; set; } | The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error. The default value is 15. |
| [ConvertZeroDateTime](../MySqlConnectionStringBuilder/ConvertZeroDateTime/) { get; set; } | Whether invalid `DATETIME` fields should be converted to MinValue. |
| [Database](../MySqlConnectionStringBuilder/Database/) { get; set; } | (Optional) The case-sensitive name of the initial database to use. This may be required if the MySQL user account only has access rights to particular databases on the server. |
| [DateTimeKind](../MySqlConnectionStringBuilder/DateTimeKind/) { get; set; } | The [`DateTimeKind`](../MySqlConnectionStringBuilder/DateTimeKind/) to use when deserializing `DATETIME` values. |
| [DefaultCommandTimeout](../MySqlConnectionStringBuilder/DefaultCommandTimeout/) { get; set; } | The length of time (in seconds) each command can execute before the query is cancelled on the server, or zero to disable timeouts. |
| [ForceSynchronous](../MySqlConnectionStringBuilder/ForceSynchronous/) { get; set; } | Forces all async methods to execute synchronously. This can be useful for debugging. |
| [GuidFormat](../MySqlConnectionStringBuilder/GuidFormat/) { get; set; } | Determines which column type (if any) should be read as a Guid. |
| [IgnoreCommandTransaction](../MySqlConnectionStringBuilder/IgnoreCommandTransaction/) { get; set; } | Does not check the [`Transaction`](../MySqlCommand/Transaction/) property for validity when executing a command. |
| [IgnorePrepare](../MySqlConnectionStringBuilder/IgnorePrepare/) { get; set; } | Ignores calls to [`Prepare`](../MySqlCommand/Prepare/) and `PrepareAsync`. |
| [InteractiveSession](../MySqlConnectionStringBuilder/InteractiveSession/) { get; set; } | Instructs the MySQL server that this is an interactive session. |
| override [Item](../MySqlConnectionStringBuilder/Item/) { get; set; } | Retrieves an option value by name. |
| [Keepalive](../MySqlConnectionStringBuilder/Keepalive/) { get; set; } | TCP Keepalive idle time (in seconds), or 0 to use OS defaults. |
| [LoadBalance](../MySqlConnectionStringBuilder/LoadBalance/) { get; set; } | Specifies how load is distributed across backend servers. |
| [MaximumPoolSize](../MySqlConnectionStringBuilder/MaximumPoolSize/) { get; set; } | The maximum number of connections allowed in the pool. |
| [MinimumPoolSize](../MySqlConnectionStringBuilder/MinimumPoolSize/) { get; set; } | The minimum number of connections to leave in the pool if [`ConnectionIdleTimeout`](../MySqlConnectionStringBuilder/ConnectionIdleTimeout/) is reached. |
| [NoBackslashEscapes](../MySqlConnectionStringBuilder/NoBackslashEscapes/) { get; set; } | Doesn't escape backslashes in string parameters. For use with the `NO_BACKSLASH_ESCAPES` MySQL server mode. |
| [OldGuids](../MySqlConnectionStringBuilder/OldGuids/) { get; set; } | Use the [`GuidFormat`](../MySqlConnectionStringBuilder/GuidFormat/) property instead. |
| [Password](../MySqlConnectionStringBuilder/Password/) { get; set; } | The password for the MySQL user. |
| [PersistSecurityInfo](../MySqlConnectionStringBuilder/PersistSecurityInfo/) { get; set; } | If true, preserves security-sensitive information in the connection string retrieved from any open [`MySqlConnection`](../MySqlConnectionType/). |
| [PipeName](../MySqlConnectionStringBuilder/PipeName/) { get; set; } | The name of the Windows named pipe to use to connect to the server. You must also set [`ConnectionProtocol`](../MySqlConnectionStringBuilder/ConnectionProtocol/) to NamedPipe to used named pipes. |
| [Pooling](../MySqlConnectionStringBuilder/Pooling/) { get; set; } | Enables connection pooling. |
| [Port](../MySqlConnectionStringBuilder/Port/) { get; set; } | The TCP port on which MySQL Server is listening for connections. |
| [Server](../MySqlConnectionStringBuilder/Server/) { get; set; } | The host name or network address of the MySQL Server to which to connect. Multiple hosts can be specified in a comma-delimited list. |
| [ServerRedirectionMode](../MySqlConnectionStringBuilder/ServerRedirectionMode/) { get; set; } | Whether to use server redirection. |
| [ServerRsaPublicKeyFile](../MySqlConnectionStringBuilder/ServerRsaPublicKeyFile/) { get; set; } | The path to a file containing the server's RSA public key. |
| [ServerSPN](../MySqlConnectionStringBuilder/ServerSPN/) { get; set; } | The server’s Service Principal Name (for `auth_gssapi_client` authentication). |
| [SslCa](../MySqlConnectionStringBuilder/SslCa/) { get; set; } | The path to a CA certificate file in a PEM Encoded (.pem) format. This should be used with a value for the [`SslMode`](../MySqlConnectionStringBuilder/SslMode/) property of VerifyCA or VerifyFull to enable verification of a CA certificate that is not trusted by the operating system’s certificate store. |
| [SslCert](../MySqlConnectionStringBuilder/SslCert/) { get; set; } | The path to the client’s SSL certificate file in PEM format. [`SslKey`](../MySqlConnectionStringBuilder/SslKey/) must also be specified, and [`CertificateFile`](../MySqlConnectionStringBuilder/CertificateFile/) should not be. |
| [SslKey](../MySqlConnectionStringBuilder/SslKey/) { get; set; } | The path to the client’s SSL private key in PEM format. [`SslCert`](../MySqlConnectionStringBuilder/SslCert/) must also be specified, and [`CertificateFile`](../MySqlConnectionStringBuilder/CertificateFile/) should not be. |
| [SslMode](../MySqlConnectionStringBuilder/SslMode/) { get; set; } | Whether to use SSL/TLS when connecting to the MySQL server. |
| [TlsCipherSuites](../MySqlConnectionStringBuilder/TlsCipherSuites/) { get; set; } | The TLS cipher suites which may be used during TLS negotiation. The default value (the empty string) allows the OS to determine the TLS cipher suites to use; this is the recommended setting. |
| [TlsVersion](../MySqlConnectionStringBuilder/TlsVersion/) { get; set; } | The TLS versions which may be used during TLS negotiation, or empty to use OS defaults. |
| [TreatTinyAsBoolean](../MySqlConnectionStringBuilder/TreatTinyAsBoolean/) { get; set; } | Returns `TINYINT(1)` fields as Boolean values. |
| [UseAffectedRows](../MySqlConnectionStringBuilder/UseAffectedRows/) { get; set; } | Report changed rows instead of found rows. |
| [UseCompression](../MySqlConnectionStringBuilder/UseCompression/) { get; set; } | Compress packets sent to and from the server. |
| [UserID](../MySqlConnectionStringBuilder/UserID/) { get; set; } | The MySQL user ID. |
| [UseXaTransactions](../MySqlConnectionStringBuilder/UseXaTransactions/) { get; set; } | Use XA transactions to implement TransactionScope distributed transactions. |
| override [ContainsKey](../MySqlConnectionStringBuilder/ContainsKey/)(…) | Whether this [`MySqlConnectionStringBuilder`](../MySqlConnectionStringBuilderType/) contains a set option with the specified name. |
| override [Remove](../MySqlConnectionStringBuilder/Remove/)(…) | Removes the option with the specified name. |

## Protected Members

| name | description |
| --- | --- |
| override [GetProperties](../MySqlConnectionStringBuilder/GetProperties/)(…) | Fills in *propertyDescriptors* with information about the available properties on this object. |

## Remarks

See [Connection String Options](https://mysqlconnector.net/connection-options/) for more documentation on the options.

## See Also

* namespace [MySqlConnector](../../MySqlConnectorNamespace/)
* assembly [MySqlConnector](../../MySqlConnectorAssembly/)

<!-- DO NOT EDIT: generated by xmldocmd for MySqlConnector.dll -->
