# MySqlConnector Version History

## 0.1.0

First non-alpha release. Supports core data access scenarios with common ORMs.

### Known Issues

* The behaviour of cancellation is not well-defined in this release; cancelling a query
may leave the `MySqlConnection` in an unusable state.
* Not all MySQL data types are supported.
* Many `MySql.Data` connection string settings are not supported by this library.
* Compression and TLS are not supported.
