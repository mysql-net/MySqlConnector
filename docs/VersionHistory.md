# MySqlConnector Version History

## Known Issues

* The behaviour of cancellation is not well-defined in this release; cancelling a query
may leave the `MySqlConnection` in an unusable state.
* Not all MySQL data types are supported.
* Many `MySql.Data` connection string settings are not supported by this library.
* Compression is not supported.
* Stored Procedures aren't supported.
* Only the "`mysql_native_password`" authentication plugin is supported.

## Release Notes

### 0.3.0

* Add SSL support and `SslMode` connection string option: [#88](https://github.com/bgrainger/MySqlConnector/issues/88).
* Rewrite protocol serialization layer to support SSL and make adding compression easier: [#93](https://github.com/bgrainger/MySqlConnector/pull/93).

### 0.2.1

* Add more diagnostics for unsupported auth plugins.

### 0.2.0

* Add `MySqlConnectionStringBuilder.ForceSynchronous`: [#91](https://github.com/bgrainger/MySqlConnector/issues/91).

### 0.1.0

* First non-alpha release. Supports core data access scenarios with common ORMs.
