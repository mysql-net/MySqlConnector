# MySqlConnector Version History

## Known Issues

* The behaviour of cancellation is not well-defined in this release; cancelling a query
may leave the `MySqlConnection` in an unusable state.
* Not all MySQL data types are supported.
* Many `MySql.Data` connection string settings are not supported by this library.
* Compression and TLS are not supported.
* Stored Procedures aren't supported.
* Only the "`mysql_native_password`" authentication plugin is supported.

## Release Notes

### 0.2.1

* Added more diagnostics for unsupported auth plugins.

### 0.2.0

* Add `MySqlConnectionStringBuilder.ForceSynchronous`: #91

### 0.1.0

* First non-alpha release. Supports core data access scenarios with common ORMs.
