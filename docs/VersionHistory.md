# MySqlConnector Version History

## Known Issues

* The behaviour of cancellation is not well-defined in this release; cancelling a query
may leave the `MySqlConnection` in an unusable state.
* Not all MySQL data types are supported.
* Many `MySql.Data` connection string settings are not supported by this library. See
[Connection Options](https://mysql-net.github.io/MySqlConnector/connection-options/) for a list
of supported options.
* Stored Procedures aren't supported.
* Only the "`mysql_native_password`" authentication plugin is supported.

## Release Notes

### 0.6.2

* Fix `NullReferenceException` when `MySqlParameter.Value == null`: [#126](https://github.com/mysql-net/MySqlConnector/issues/126).

### 0.6.1

* Fix `AggregateException` going unhandled in `OpenAsync`: [#124](https://github.com/mysql-net/MySqlConnector/issues/124).
* Fix SSL over Unix domain sockets.
* Reduce allocations when using SSL certificates.

### 0.6.0

* Implement `UseCompression` connection string option: [#31](https://github.com/mysql-net/MySqlConnector/issues/31).
* Add support for Unix domain sockets: [#118](https://github.com/mysql-net/MySqlConnector/issues/118).

### 0.5.0

* Implement `UseAffectedRows` connection string option. (Note that the default value is `true`, unlike `MySql.Data`.)

### 0.4.0

* Rename `SslMode` enum to `MySqlSslMode` (for compatibility with `MySql.Data`):[#102](https://github.com/mysql-net/MySqlConnector/pull/93).

### 0.3.0

* Add SSL support and `SslMode` connection string option: [#88](https://github.com/mysql-net/MySqlConnector/issues/88).
* Rewrite protocol serialization layer to support SSL and make adding compression easier: [#93](https://github.com/mysql-net/MySqlConnector/pull/93).

### 0.2.1

* Add more diagnostics for unsupported auth plugins.

### 0.2.0

* Add `MySqlConnectionStringBuilder.ForceSynchronous`: [#91](https://github.com/mysql-net/MySqlConnector/issues/91).

### 0.1.0

* First non-alpha release. Supports core data access scenarios with common ORMs.
