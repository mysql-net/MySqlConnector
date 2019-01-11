---
lastmod: 2018-10-12
date: 2017-03-27
menu:
  main:
    parent: getting started
title: Version History
weight: 30
---

Version History
===============

### 0.49.0

* **Breaking** The default value for the `UseAffectedRows` connection string option has changed from `true` to `false`. This provides better compatibility with Connector/NET's defaults and also with other ADO.NET libraries: [#600](https://github.com/mysql-net/MySqlConnector/issues/600).
  * If you are upgrading from an earlier version of MySqlConnector, either audit your uses of the return value of `ExecuteNonQuery` (it will now return the number of rows matched by the `WHERE` clause for `UPDATE` statements, instead of the number of rows whose values are actually changed), or add `UseAffectedRows=true` to your connection string.
  * If you are migrating (or have recently migrated) from Connector/NET to MySqlConnector, then no changes need to be made: MySqlConnector now exhibits the same default behaviour as Connector/NET.
* Make `MySqlException` serializable: [#601](https://github.com/mysql-net/MySqlConnector/issues/601).
* Set `MySqlException.Number` to `MySqlErrorCode.UnableToConnectToHost` when connecting fails: [#599](https://github.com/mysql-net/MySqlConnector/issues/599).
* Populate `MySqlException.Data` dictionary: [#602](https://github.com/mysql-net/MySqlConnector/issues/602).

### 0.48.2

* Fix `InvalidCastException` in `MySqlDataReader.GetDateTime` when `AllowZeroDateTime=True`: [#597](https://github.com/mysql-net/MySqlConnector/issues/597).

### 0.48.1

* Add `net471` as target platform: [#595](https://github.com/mysql-net/MySqlConnector/issues/595).
* Support `IDbColumnSchemaGenerator` interface in `netcoreapp2.1` package.
* Fix error in binding parameter values for prepared statements.
* Fix exception when using more than 32,767 parameters in a prepared statement.

### 0.48.0

* **Breaking** Disallow duplicate parameter names after normalization: [#591](https://github.com/mysql-net/MySqlConnector/issues/591).
* **Potentially breaking** Change default connection collation from `utf8mb4_bin` to `utf8mb4_general_ci`: [#585](https://github.com/mysql-net/MySqlConnector/issues/585).
* **Potentially breaking** Update stored procedure metadata cache to use `mysql.proc` when available: [#569](https://github.com/mysql-net/MySqlConnector/issues/569).
  * This provides higher performance, but is a potentially-breaking change for any client using stored procedures.
* Change `System.Transactions` support:
  * Add `UseXaTransactions` connection string option to opt out of XA transactions (equivalent to Connector/NET behaviour): [#254](https://github.com/mysql-net/MySqlConnector/issues/254).
  * **Potentially breaking** Opening multiple (distinct) `MySqlConnection` objects within the same transaction will reuse the same server session: [#546](https://github.com/mysql-net/MySqlConnector/issues/546).
* Add `MySqlConnection.InfoMessage` event: [#594](https://github.com/mysql-net/MySqlConnector/issues/594).
* Implement `ICloneable` on `MySqlCommand`: [#583](https://github.com/mysql-net/MySqlConnector/issues/583).
* Fix logic for detecting variable names in SQL: [#195](https://github.com/mysql-net/MySqlConnector/issues/195), [#589](https://github.com/mysql-net/MySqlConnector/issues/589).
* Fix `NullReferenceException` when attempting to invoke a non-existent stored procedure.
* Support MySQL Server 5.1 (and earlier) by using `utf8` if `utf8mb4` isn't available.
* Reduce log message severity for session discarded due to `ConnectionLifeTime`: [#586](https://github.com/mysql-net/MySqlConnector/issues/586).
* Optimise `MySqlDataReader.GetStream`: [#592](https://github.com/mysql-net/MySqlConnector/issues/592).
* Use latest dotnet SourceLink package.

### 0.47.1

* Fix `NullReferenceException` in `GetSchemaTable`: [#580](https://github.com/mysql-net/MySqlConnector/issues/580).
* Return correct schema table for second result set: [#581](https://github.com/mysql-net/MySqlConnector/issues/581).

### 0.47.0

* Support <a href="https://mariadb.com/kb/en/library/authentication-plugin-gssapi/">MariaDB GSSAPI</a> authentication: [#577](https://github.com/mysql-net/MySqlConnector/pull/577).
* Log received error payloads at `Debug` level.
* Thanks to <a href="https://github.com/vaintroub">Vladislav Vaintroub</a> for contributions to this release.

### 0.46.2

* Fix missing `InnerException` on `MySqlConnection` created for a timeout: [#575](https://github.com/mysql-net/MySqlConnector/issues/575).

### 0.46.1

* Fix `CryptographicException` when loading a PFX certificate file: [#574](https://github.com/mysql-net/MySqlConnector/issues/574).

### 0.46.0

* Add `MySqlParameter.Clone`.
* Implement `ICloneable` on `MySqlParameter`: [#567](https://github.com/mysql-net/MySqlConnector/pull/567).
* Implement `MySqlParameterCollection.CopyTo`.
* Add logging for cached procedures.
* Thanks to [Jorge Rocha Gualtieri](https://github.com/jrocha) for contributions to this release.

### 0.45.1

* Fix error parsing SQL parameters: [#563](https://github.com/mysql-net/MySqlConnector/issues/563).
* Add documentation for common errors: [#565](https://github.com/mysql-net/MySqlConnector/issues/565).

### 0.45.0

* Implement `MySqlConnection.GetSchema("ReservedWords")`: [#559](https://github.com/mysql-net/MySqlConnector/issues/559).
* Optimisation: Use `ReadOnlySpan<byte>` when deserialising payloads.
* Thanks to [Federico Sasso](https://github.com/fedesasso) for contributions to this release.

### 0.44.1

* `MySqlCommand.Prepare` will cache the prepared command until the connection is reset.
* Improve performance of `MySqlCommand.Prepare`, especially when preparation is unnecessary.
* Lazily allocate `MySqlParameterCollection` (accessed via `MySqlCommand.Parameters`) for better performance when command parameters aren't used.
* Use `GC.SuppressFinalize` to improve performance when various objects (derived from `Component`) aren't properly disposed.

### 0.44.0

* Add `Application Name` connection string setting: [#547](https://github.com/mysql-net/MySqlConnector/pull/547).
* Clear connection pools on exit: [#545](https://github.com/mysql-net/MySqlConnector/pull/545).
* Allow `ConnectionString` to be set on a closed connection: [#543](https://github.com/mysql-net/MySqlConnector/pull/543).
* Fix intermittent `NotSupportedException` with SSL connections under .NET Core 2.1: [#509](https://github.com/mysql-net/MySqlConnector/pull/509).

### 0.43.0

* Add first version of prepared commands: [#534](https://github.com/mysql-net/MySqlConnector/pull/534).
  * Only single statements (and not stored procedures) are preparable.
  * The (new) `IgnorePrepare` connection string option defaults to `true` and must be set to `false` to use prepared commands.
* Add `CertificateStore` and `CertificateThumbprint` connection string options: [#536](https://github.com/mysql-net/MySqlConnector/issues/536).
* Fix bug that rejected sessions from the connection pool if `ChangeDatabase` had been called: [#515](https://github.com/mysql-net/MySqlConnector/issues/515).
* Don't map `TINYINT(1) UNSIGNED` as `bool`: [#530](https://github.com/mysql-net/MySqlConnector/issues/530).
* Thanks to [Jan Hajek](https://github.com/hajekj) for contributions to this release.

### 0.42.3

* Fix bug (introduced in 0.42.2) that caused extremely high memory usage: [#528](https://github.com/mysql-net/MySqlConnector/issues/528).
* Allow `DATE` columns with invalid `DateTime` values to be read as `MySqlDateTime`: [#529](https://github.com/mysql-net/MySqlConnector/issues/529).

### 0.42.2

* Fix bug that ignored last result set returned from a stored procedure if it was empty: [#524](https://github.com/mysql-net/MySqlConnector/issues/524).
* Fix extra memory usage if column definition payloads exceeded original size estimate.

### 0.42.1

* Fix `NotImplementedException` reading a `GEOMETRY` column as `byte[]`: [#70](https://github.com/mysql-net/MySqlConnector/issues/70).
* Fix negative `TIME` parsing: [#518](https://github.com/mysql-net/MySqlConnector/issues/518).
* Fix `ArgumentException` preparing a SQL statement: [#520](https://github.com/mysql-net/MySqlConnector/issues/520).

### 0.42.0

* Add `AllowZeroDateTime` connection string option: [#507](https://github.com/mysql-net/MySqlConnector/issues/507).
* Add `MySqlDateTime` type (to allow invalid `DateTime` values to be represented).

### 0.41.0

* Add `GuidFormat` connection string option (for better compatibility with MySQL 8.0 [`UUID_TO_BIN`](https://dev.mysql.com/doc/refman/8.0/en/miscellaneous-functions.html#function_uuid-to-bin)): [#497](https://github.com/mysql-net/MySqlConnector/issues/497).
* Add `InteractiveSession` connection string option: [#510](https://github.com/mysql-net/MySqlConnector/issues/510).
* Add `PipeName` connection string option: [#454](https://github.com/mysql-net/MySqlConnector/issues/454).
* Improve performance by using `ReadOnlySpan<byte>`, `Utf8Formatter`, and `Utf8Parser`: [#426](https://github.com/mysql-net/MySqlConnector/issues/426).
* Add `netcoreapp2.1` package.
* Remove unnecessary dependencies from `netstandard2.0` package.
* Fix error in loading multiple certificates on macOS.
* Fix bug in setting `MySqlParameter.Precision` and `Scale` under `netstandard1.3`.
* Thanks to [Ed Ball](https://github.com/ejball) for contributions to this release.

### 0.40.4

* Allow `CACertificateFile` to contain multiple (concatenated) certificates: [#499](https://github.com/mysql-net/MySqlConnector/issues/499).
* Avoid `IndexOutOfRangeException` in `ValidateRemoteCertificate`: [#498](https://github.com/mysql-net/MySqlConnector/issues/498).

### 0.40.3

* Fix `caching_sha2_password` authentication for MySQL Server 8.0.11 release: [#489](https://github.com/mysql-net/MySqlConnector/issues/489).
  * **Breaking** This authentication plugin can no longer be used with MySQL Server 8.0.4; you must update to the GA release.

### 0.40.2

* Reverted `ConnectionReset=true` optimisation (added in 0.40.0) that was incompatible with Aurora: [#486](https://github.com/mysql-net/MySqlConnector/issues/486).

### 0.40.1

* Always use new `DateTimeKind` setting: [#484](https://github.com/mysql-net/MySqlConnector/pull/484).

### 0.40.0

* Add `DateTimeKind` connection string setting: [#479](https://github.com/mysql-net/MySqlConnector/pull/479).
* Fix `ArgumentException` for `SslProtocols.None`: [#482](https://github.com/mysql-net/MySqlConnector/issues/482).
* Fix `IOException` being thrown at startup: [#475](https://github.com/mysql-net/MySqlConnector/issues/475).
* Fix race condition in `OpenTcpSocketAsync`: [#476](https://github.com/mysql-net/MySqlConnector/issues/476).
* Optimise `MySqlConnection.Open` when `ConnectionReset=true` (default): [#483](https://github.com/mysql-net/MySqlConnector/pull/483).
* Thanks to [Ed Ball](https://github.com/ejball) for contributions to this release.

#### MySqlConnector.Logging.Microsoft.Extensions.Logging

* Reduce dependencies to just `Microsoft.Extensions.Logging.Abstractions`.

### 0.39.0

* Add `IgnoreCommandTransaction` connection string setting: [#474](https://github.com/mysql-net/MySqlConnector/issues/474).

### 0.38.0

* Add Serilog logging provider: [#463](https://github.com/mysql-net/MySqlConnector/issues/463).
* Add NLog logging provider: [#470](https://github.com/mysql-net/MySqlConnector/pull/470).
* Implement `IDbDataParameter` on `MySqlParameter`: [#465](https://github.com/mysql-net/MySqlConnector/issues/465).
* Implement `MySqlDataReader.GetChar`: [#456](https://github.com/mysql-net/MySqlConnector/issues/456).
* Add `MySqlDataReader.GetFieldType(string)` overload: [#440](https://github.com/mysql-net/MySqlConnector/issues/440).
* Fix a connection pooling session leak in high contention scenarios: [#469](https://github.com/mysql-net/MySqlConnector/issues/469).
* Fix overhead of extra connection pools created in a race: [#468](https://github.com/mysql-net/MySqlConnector/issues/468).
* Thanks to [Marc Lewandowski](https://github.com/marcrocny) and [Rolf Kristensen](https://github.com/snakefoot) for contributions to this release.

### 0.37.1

* Serialize enum parameter values as strings when `MySqlParameter.MySqlDbType` is set to `MySqlDbType.String` or `VarChar`: [#459](https://github.com/mysql-net/MySqlConnector/issues/459).
* Require `ConnectionIdlePingTime` (added in 0.37.0) to be explicitly set to a non-zero value to avoid pinging the server.
* Thanks to [Naragato](https://github.com/naragato) for contributions to this release.

### 0.37.0

* Support TLS 1.2 on Windows clients: [#458](https://github.com/mysql-net/MySqlConnector/pull/458).
  * Use the best TLS version supported by the OS, as per [best practices](https://docs.microsoft.com/en-us/dotnet/framework/network-programming/tls).
* Add `ConnectionIdlePingTime` connection string setting _(Experimental)_: [#461](https://github.com/mysql-net/MySqlConnector/pull/461).
* Fix failure to log in for accounts with empty passwords that use `caching_sha2_password`.
* Throw `MySqlException` for invalid port number in connection string.

### 0.36.1

* Reap connections more frequently if `ConnectionIdleTimeout` is low: [#442](https://github.com/mysql-net/MySqlConnector/issues/442).
* Fix `ArgumentOutOfRangeException` when command times out: [#447](https://github.com/mysql-net/MySqlConnector/issues/447).
* Speed up connection pooling (particularly in applications that only have one connection string).
* Reduce heap allocations in common scenarios.

### 0.36.0

* **Breaking** Require `CertificateFile` to include private key (for mutual authentication): [#436](https://github.com/mysql-net/MySqlConnector/issues/436).
* Add `MySqlDataReader.GetX(string)` overloads: [#435](https://github.com/mysql-net/MySqlConnector/issues/435).
* Add `MySqlDataReader.GetTimeSpan`: [#438](https://github.com/mysql-net/MySqlConnector/issues/438).
* Add `MySqlDataReader.GetUInt16`, `GetUInt32`, `GetUInt64`: [#439](https://github.com/mysql-net/MySqlConnector/issues/439).
* Set `MySqlConnection.State` to `ConnectionState.Closed` when the connection fails: [#433](https://github.com/mysql-net/MySqlConnector/issues/433).
* Fix error parsing `--` comments in SQL: [#429](https://github.com/mysql-net/MySqlConnector/issues/429).
* Fix error parsing C-style comments in SQL.
* **Breaking** Wrap unhandled `SocketException` in `MySqlException`: [#434](https://github.com/mysql-net/MySqlConnector/issues/434).

### 0.35.0

* Add `MySqlCommandBuilder.DeriveParameters`: [#419](https://github.com/mysql-net/MySqlConnector/issues/419).
* Add `MySqlConnection.Ping` and `MySqlConnection.PingAsync`: [#260](https://github.com/mysql-net/MySqlConnector/issues/260).

### 0.34.2

* Fix exception when a stored procedure returns `NULL` for an `OUT` parameter: [#425](https://github.com/mysql-net/MySqlConnector/issues/425).

### 0.34.1

* Add overloads of `MySqlParameterCollection.Add`: [#424](https://github.com/mysql-net/MySqlConnector/issues/424).
* Fix conversion of `MySqlCommand.LastInsertedId`: [#422](https://github.com/mysql-net/MySqlConnector/issues/422).
* Fix "Expected state to be Failed but was Connected" `InvalidOperationException`: [#423](https://github.com/mysql-net/MySqlConnector/issues/423).
* Improve performance when calling stored procedures with no parameters (this was regressed in 0.34.0).
* Reduce severity of some logging statements.

### 0.34.0

* Implement `MySqlCommandBuilder`: [#303](https://github.com/mysql-net/MySqlConnector/issues/303).
* Add `Microsoft.Extensions.Logging` provider: [#418](https://github.com/mysql-net/MySqlConnector/issues/418).
* Add new `MySqlTransaction.Connection` property that returns an object typed as `MySqlConnection`.
* Support `CLIENT_SESSION_TRACK` protocol option: [#323](https://github.com/mysql-net/MySqlConnector/issues/323).
* Optimization: move procedure cache to connection pool: [#415](https://github.com/mysql-net/MySqlConnector/issues/415).
* Ignore extra data at end of column definition payload: [#413](https://github.com/mysql-net/MySqlConnector/pull/413).
* Handle failure to find procedure: [#282](https://github.com/mysql-net/MySqlConnector/issues/282).
* **Breaking** Clear `MySqlTransaction.Connection` when transaction is committed: [#61](https://github.com/mysql-net/MySqlConnector/issues/61).
  * This is a breaking API change from Connector/NET, but matches other ADO.NET connectors.

### 0.33.2

* **Breaking** Throw `InvalidCastException` instead of `MySqlException` from `MySqlDataReader.GetGuid`.
  * This is a breaking API change from Connector/NET, but matches other ADO.NET connectors.
* Fix default values of `MySqlParameter.ParameterName` and `.SourceColumn`; they now follow MSDN documentation.
* Fix `ObjectDisposedException` when a connection is returned to the pool: [#411](https://github.com/mysql-net/MySqlConnector/issues/411).
* Fix `NotSupportedException` when `MySqlParameter.Value` is set to a `char`: [#412](https://github.com/mysql-net/MySqlConnector/issues/412).

### 0.33.1

* Add missing `.ConfigureAwait(false)`
  * Fixes a potential deadlock in clients that blocked on `Task`s returned from async methods.

### 0.33.0

* Implement logging framework: [#390](https://github.com/mysql-net/MySqlConnector/issues/390).
  * Add new project, `MySqlConnector.Logging.log4net`, that adapts MySqlConnector logging for log4net.
* Implement `MySqlDataAdapter`: [#183](https://github.com/mysql-net/MySqlConnector/issues/183).
* Get correct connection ID for Azure Database for MySQL.
* Use `AdoNet.Specification.Tests` test suite to validate implementation.

### 0.32.0

* Implement more `MySqlParameter` constructor overloads: [#402](https://github.com/mysql-net/MySqlConnector/issues/402).
  * This improves compatibility with Connector/NET.
* Implement `MySqlParameter.Precision` and `MySqlParameter.Scale`.
  * The properties are provided only for source compatibility.
  * Not available on .NET 4.5.
* Implement `MySqlDataReader.GetChars`.
* Implement `MySqlDataReader.Depth`.
* Fix `NullReferenceException` in `MySqlDataReader` when reader is disposed.
* **Breaking** Throw `InvalidCastException` (instead of `MySqlException`) from `MySqlDataReader.GetGuid` if column is `NULL`.
* **Breaking** Throw `InvalidOperationException` (instead of `MySqlException`) from `MySqlConnection.ConnectionString` setter if connection is open.
* **Breaking** Throw `ArgumentException` (instead of `InvalidOperationException`) from `MySqlConnectionStringBuilder` for invalid option names.

### 0.31.3

* Fix return value of `ExecuteScalar` to be the first column from the first row of the first result set.
* Fix return value of `ExecuteNonQuery` to correctly return -1 for `SELECT` statements.
* Fix bug where `NextResult` returns `true` for a trailing comment in a SQL statement.
* **Breaking** Throw `InvalidOperationException` if `MySqlCommand.CommandText` is set while the command is active.
* **Breaking** Throw `InvalidOperationException` (instead of `MySqlException`) if a `MySqlCommand` is executed while there is an open reader.
* **Breaking** Throw `InvalidOperationException` from `MySqlCommand.Prepare` when preconditions aren't met.

### 0.31.2

* **Breaking** Throw `InvalidOperationException` when `MySqlCommand.Connection` can't be set (instead of `MySqlException`).
* **Breaking** Throw `InvalidOperationException` from `MySqlCommand.Prepare` when preconditions aren't met.
* Fix `NullReferenceException` when `MySqlCommand.Connection` isn't set (now correctly throws `InvalidOperationException`).

### 0.31.1

* Fix `InvalidOperationException` if `MySqlBulkLoader` is used inside a transaction (again): [#300](https://github.com/mysql-net/MySqlConnector/issues/300).
* **Breaking** Remove `MySqlBulkLoader.Transaction` property (added in 0.24.0); `MySqlBulkLoader` will always use the ambient transaction, if any. This matches Connector/NET API & behaviour.

### 0.31.0

* Implement `MinimumPoolSize`: [#85](https://github.com/mysql-net/MySqlConnector/issues/85).
* Implement server load balancing with new `LoadBalance` connection string setting: [#226](https://github.com/mysql-net/MySqlConnector/issues/226).
* Add SourceLink.
* Wrap `EndOfStreamException` in `MySqlException` when connecting fails: [#388](https://github.com/mysql-net/MySqlConnector/issues/388).
* Fix `StackOverflowException` when reading large BLOBs asynchronously.
* Don't set `Transaction` on new `MySqlCommand`: [#389](https://github.com/mysql-net/MySqlConnector/issues/389).
* Ignore `MySqlConnection.Cancel` when connection is broken: [#386](https://github.com/mysql-net/MySqlConnector/issues/386).
* Improve internal code organisation: [#376](https://github.com/mysql-net/MySqlConnector/issues/376).

### 0.30.0

* **Breaking** Remove `BufferResultSets` connection string option: [#378](https://github.com/mysql-net/MySqlConnector/pull/378).
* The assembly is now strong-named: [#224](https://github.com/mysql-net/MySqlConnector/issues/224).

### 0.29.4

* Fix exception in `MySqlTransaction.Dispose` if the underlying connection is closed or faulted: [#383](https://github.com/mysql-net/MySqlConnector/issues/383).

### 0.29.3

* Remove `System.Runtime.InteropServices.RuntimeInformation` dependency on full framework: [#381](https://github.com/mysql-net/MySqlConnector/issues/381).

### 0.29.2

* Fix an exception if `MySqlDataReader.GetOrdinal` was called before `Read`: [#379](https://github.com/mysql-net/MySqlConnector/issues/379).

### 0.29.1

* Work around Amazon Aurora `DateTime` conversion issue: [#364](https://github.com/mysql-net/MySqlConnector/issues/364).
* Fix `NotSupportedException` in `MySqlParameter`: [#367](https://github.com/mysql-net/MySqlConnector/pull/367).
* **Breaking** Remove a number of `MySqlErrorCode` enum values (to reduce library size).
* Thanks to [Duane Gilbert](https://github.com/dgilbert) and [Naragato](https://github.com/Naragato) for contributions to this release.

### 0.29.0

* **Breaking** Implement `MySqlConnectionStringBuilder.DefaultCommandTimeout` and `MySqlCommand.CommandTimeout` with a default of 30 seconds: [#67](https://github.com/mysql-net/MySqlConnector/issues/67).
  * This may cause long-running queries to throw an exception instead of succeeding; as a workaround, increase `CommandTimeout`.
* Expose `MySqlDbType` and `MySqlCommand.MySqlDbType`: [#362](https://github.com/mysql-net/MySqlConnector/issues/362).
  * MySqlConnector adds `MySqlDbType.Bool` to represent a `TINYINT(1)` column.
  * Return correct values for `ProviderType` in `GetColumnSchema`/`GetSchemaTable`.
* Implement `MySqlConnection.GetSchema`: [#361](https://github.com/mysql-net/MySqlConnector/issues/361).
* Update documentation for .NET Core 2.0: [#372](https://github.com/mysql-net/MySqlConnector/issues/372).
* Fix information disclosure vulnerability related to `LOAD DATA LOCAL INFILE`: [#334](https://github.com/mysql-net/MySqlConnector/issues/334).
* Improve async performance.
* Throw exception for unexpected API use: [#308](https://github.com/mysql-net/MySqlConnector/issues/308).
* Thanks to [Gabden Ayazbayev](https://github.com/Drake103), [Tuomas Hietanen](https://github.com/Thorium), and [Dustin Masters](https://github.com/dustinsoftware) for contributions to this release.

### 0.28.2

* Allow the auth plugin name in the initial handshake to be EOF-terminated: [#351](https://github.com/mysql-net/MySqlConnector/issues/351).

### 0.28.1

* Fix garbage data being returned by `GetColumnSchema`/`GetSchemaTable`: [#354](https://github.com/mysql-net/MySqlConnector/issues/354).
* Fix incorrect `NumericPrecision` for `decimal(n,0)` columns: [#356](https://github.com/mysql-net/MySqlConnector/issues/356).

### 0.28.0

* Support `caching_sha2_password` authentication for MySQL 8.0: [#329](https://github.com/mysql-net/MySqlConnector/issues/329).
* Fix inconsistent return value of `MySqlDataReader.HasRows`: [#348](https://github.com/mysql-net/MySqlConnector/issues/348).
* Thanks to [Drake103](https://github.com/Drake103) for contributions to this release.

### 0.27.0

* Implement `MySqlDataReader.GetColumnSchema`: [#182](https://github.com/mysql-net/MySqlConnector/issues/182).
* Implement `MySqlDataReader.GetSchemaTable`: [#307](https://github.com/mysql-net/MySqlConnector/issues/307).
* Support MySQL Server 8.0.3 and MariaDB 10.2 collations: [#336](https://github.com/mysql-net/MySqlConnector/issues/336), [#337](https://github.com/mysql-net/MySqlConnector/issues/337), [#338](https://github.com/mysql-net/MySqlConnector/issues/338).
* Reduce allocations to improve performance: [#342](https://github.com/mysql-net/MySqlConnector/pull/342), [#343](https://github.com/mysql-net/MySqlConnector/pull/343).
* Thanks to [Alex Lee](https://github.com/elemount) and [Dave Dunkin](https://github.com/ddunkin) for contributions to this release.

### 0.26.5

* Fix hang closing connection with ClearDB on Azure: [#330](https://github.com/mysql-net/MySqlConnector/issues/330).
* Thanks to [Marcin Badurowicz](https://github.com/ktos) for contributions to this release.

### 0.26.4

* Fix overly-broad exception handler introduced in 0.26.3.
* Improve efficiency of code added in 0.26.3.

### 0.26.3

* Fix `HasRows` incorrectly returning `false` after all rows have been read: [#327](https://github.com/mysql-net/MySqlConnector/issues/327).
* Fix `EndOfStreamException` when reusing a pooled connection with Amazon Aurora.
* Reduce network roundtrips when opening a pooled connection (with the default settings of `Pooling=True;Connection Reset=true`); see [#258](https://github.com/mysql-net/MySqlConnector/issues/258).
* Update `System.*` dependencies to 4.3.0 for .NET 4.5 and .NET 4.6 packages.
* Thanks to [Brad Nabholz](https://github.com/bnabholz) for contributions to this release.

### 0.26.2

* Support `CLIENT_DEPRECATE_EOF` flag: [#322](https://github.com/mysql-net/MySqlConnector/issues/322).
* Throw better exception when a malformed packet is detected.
* Don't allow sessions in an error state to be put back into the pool.
* Remove unsupported `CLIENT_PS_MULTI_RESULTS` flag (sent during connection handshaking).

### 0.26.1

* Throw better exception when MySQL Server sends an old authentication method switch request packet: [#316](https://github.com/mysql-net/MySqlConnector/pull/316).
* Capture InnerException in `ActivateResultSet`.
* Thanks to [kobake](https://github.com/kobake) for contributions to this release.

### 0.26.0

* Add convenience methods that return derived types: [#313](https://github.com/mysql-net/MySqlConnector/issues/313).

### 0.25.1

* Prevent exception being thrown from `MySqlSession.DisposeAsync`, which could cause leaked connections: [#305](https://github.com/mysql-net/MySqlConnector/issues/305).

### 0.25.0

* Add `netstandard2.0` compile target: [#270](https://github.com/mysql-net/MySqlConnector/issues/270).

### 0.24.2

* Fix leaked session when a `MySqlException` is thrown because a query contains a user-defined variable and `Allow User Variables=false`: [#305](https://github.com/mysql-net/MySqlConnector/issues/305).

### 0.24.1

* Recover leaked sessions when `MySqlDataReader` isn't disposed: [#306](https://github.com/mysql-net/MySqlConnector/issues/306).

### 0.24.0

* **Breaking** Add `AllowPublicKeyRetrieval` connection string setting, defaulted to `false`: [#286](https://github.com/mysql-net/MySqlConnector/issues/286).
  * Add `ServerRSAPublicKeyFile` connection string setting.
* Fix hang in `MySqlDataReader.Dispose` if function threw an exception: [#299](https://github.com/mysql-net/MySqlConnector/issues/299).
* Fix `InvalidOperationException` if `MySqlBulkLoader` is used inside a transaction: [#300](https://github.com/mysql-net/MySqlConnector/issues/300).

### 0.23.0

* Support .NET 4.5: [#295](https://github.com/mysql-net/MySqlConnector/issues/295).
* Send client connection attributes: [#293](https://github.com/mysql-net/MySqlConnector/issues/293).
* Dispose `X509Certificate2` objects (.NET 4.6 and later): [#275](https://github.com/mysql-net/MySqlConnector/issues/275).

### 0.22.0

* Add server certificate validation via `MySqlConnectionStringBuilder.CACertificateFile`: [#280](https://github.com/mysql-net/MySqlConnector/pull/280).
* Support `sha256_password` authentication: [#281](https://github.com/mysql-net/MySqlConnector/issues/281).
* Ignore `IOException` in `TryPingAsync`: [#289](https://github.com/mysql-net/MySqlConnector/issues/289).
* Fix "Aborted connection" server errors when `MySqlConnection` isn't disposed: [#290](https://github.com/mysql-net/MySqlConnector/issues/290).
* Run integration tests on MySQL Server 5.6, MySQL Server 5.7, MariaDB 10.3, Percona Server 5.7.

### 0.21.0

* Add `MySqlHelper.EscapeString`: [#277](https://github.com/mysql-net/MySqlConnector/issues/277).

### 0.20.2

* Fix bugs where objects holding unmanaged resources weren't disposed: [#275](https://github.com/mysql-net/MySqlConnector/issues/275).

### 0.20.1

* Fix bug retrieving a connection from the pool when using Amazon IAM Authentication: [#269](https://github.com/mysql-net/MySqlConnector/issues/269).

### 0.20.0

* Support [Amazon RDS](http://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Welcome.html) [IAM Authentication](http://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.IAMDBAuth.html): [#268](https://github.com/mysql-net/MySqlConnector/issues/268).

### 0.19.5

* Fix duration of transaction isolation level: [#263](https://github.com/mysql-net/MySqlConnector/issues/263).
* Fix crash sending a GUID containing `0x27` or `0x5C` when `OldGuids=true`: [#265](https://github.com/mysql-net/MySqlConnector/pull/265).
* Thanks to [Adam Poit](https://github.com/adampoit) for contributions to this release.

### 0.19.4

* Fix `NotImplementedException` in `GetFieldType` and `GetDataTypeName`: [#261](https://github.com/mysql-net/MySqlConnector/issues/261).

### 0.19.3

* Fix authentication against Azure Database for MySQL: [#259](https://github.com/mysql-net/MySqlConnector/issues/259).
* Support enum parameter values: [#255](https://github.com/mysql-net/MySqlConnector/issues/255).
* Fix `CancellationToken` being ignored by `ChangeDatabaseAsync`: [#253](https://github.com/mysql-net/MySqlConnector/pull/253).
* Fix `NullReferenceException` being thrown from `MySqlConnection.CloseDatabase`.
* Thanks to [Nicholas Schell](https://github.com/Nicholi) for contributions to this release.

### 0.19.2

* Fix connection pool exhaustion if connections aren't disposed: [#251](https://github.com/mysql-net/MySqlConnector/issues/251).
* Fix potential `NullReferenceException` in `MySqlDataReader.Dispose`.

### 0.19.1

* Fix incorrect return value from `ExecuteNonQuery`: [#250](https://github.com/mysql-net/MySqlConnector/pull/250).
* Improve performance when retrieving large BLOBs: [#249](https://github.com/mysql-net/MySqlConnector/pull/249).
* Thanks to [Adam Poit](https://github.com/adampoit) for contributions to this release.

### 0.19.0

* Improve performance of common scenarios: [#245](https://github.com/mysql-net/MySqlConnector/pull/245).

### 0.18.3

* Fix query interrupted exception after canceling a completed query: [#248](https://github.com/mysql-net/MySqlConnector/pull/248).
* Thanks to [Adam Poit](https://github.com/adampoit) for contributions to this release.

### 0.18.2

* Fix excessive memory usage with `BufferResultSets=true`: [#244](https://github.com/mysql-net/MySqlConnector/issues/244).

### 0.18.1

* Support new MySQL Server 8.0.1 collations: [#242](https://github.com/mysql-net/MySqlConnector/issues/242).
* Specify preferred collation when resetting connection: [#243](https://github.com/mysql-net/MySqlConnector/issues/243).

### 0.18.0

* Support [`System.Transactions` transaction processing](https://msdn.microsoft.com/en-us/library/ee818755.aspx): [#13](https://github.com/mysql-net/MySqlConnector/issues/13).
* Add `AutoEnlist` connection string option: [#241](https://github.com/mysql-net/MySqlConnector/pull/241).
* Throw better exception for unsupported `ParameterDirection`: [#234](https://github.com/mysql-net/MySqlConnector/issues/234).
* Fix `StackOverflowException` reading a large blob: [#239](https://github.com/mysql-net/MySqlConnector/issues/239).

### 0.17.0

* Implement cancellation of the active reader: [#3](https://github.com/mysql-net/MySqlConnector/issues/3).
* Add `MySqlErrorCode`: [#232](https://github.com/mysql-net/MySqlConnector/issues/232).
* Implement a connection pool reaper to close idle connections: [#217](https://github.com/mysql-net/MySqlConnector/issues/217).
 * Add `ConnectionIdleTimeout` connection string option: [#218](https://github.com/mysql-net/MySqlConnector/issues/218).
* Implement `ConnectionLifeTime` connection string option: [#212](https://github.com/mysql-net/MySqlConnector/issues/212).

### 0.16.2

* Fix exceptions when server resets the connection: [#221](https://github.com/mysql-net/MySqlConnector/issues/221).

### 0.16.1

* Throw a better exception when `max_allowed_packet` is exceeded: [#40](https://github.com/mysql-net/MySqlConnector/issues/40).

### 0.16.0

* Implement `MySqlParameterCollection.AddWithValue`: [#127](https://github.com/mysql-net/MySqlConnector/issues/127).
* Thanks to [michi84o](https://github.com/michi84o) for contributions to this release.

### 0.15.2

* Include help on `AllowUserVariables` in exception message: [#206](https://github.com/mysql-net/MySqlConnector/issues/206).

### 0.15.1

* Fix `NullReferenceException` in `MySqlConnection.Database`: [#205](https://github.com/mysql-net/MySqlConnector/issues/205).

### 0.15.0

* Implement `MySqlConnection.ChangeDatabase`: [#201](https://github.com/mysql-net/MySqlConnector/issues/201).
* Add `Buffer Result Sets` connection string option: [#202](https://github.com/mysql-net/MySqlConnector/issues/202).

### 0.14.1

* Fix exception when `MySqlDataReader` isn't disposed: [#196](https://github.com/mysql-net/MySqlConnector/issues/196).

### 0.14.0

* Update `System.*` package references: [#190](https://github.com/mysql-net/MySqlConnector/issues/190).

### 0.13.0

* Add `MySqlBulkLoader`: [#15](https://github.com/mysql-net/MySqlConnector/issues/15).
* Thanks to [gitsno](https://github.com/michi84o) for contributions to this release.

### 0.12.0

* Add support for `DateTimeOffset`: [#172](https://github.com/mysql-net/MySqlConnector/issues/172), [#175](https://github.com/mysql-net/MySqlConnector/issues/175).
* Thanks to [Sébastien Ros](https://github.com/sebastienros) for contributions to this release.

### 0.11.6

* Fix `PlatformNotSupportedException` on AWS Lambda: [#170](https://github.com/mysql-net/MySqlConnector/issues/170).
* Thanks to [Sebastian](https://github.com/SebastianC) for contributions to this release.

### 0.11.5

* Further improve async and sync performance: [#164](https://github.com/mysql-net/MySqlConnector/issues/164).

### 0.11.4

* No changes in this release.

### 0.11.3

* Improve async performance: [#164](https://github.com/mysql-net/MySqlConnector/issues/164).

### 0.11.2

* Fix InvalidCastException when using aggregate functions: [#54](https://github.com/mysql-net/MySqlConnector/issues/54).

### 0.11.1

* Handle `IOException` in `MySqlSession.DisposeAsync`: [#159](https://github.com/mysql-net/MySqlConnector/issues/159).

### 0.11.0

* Implement the `SslMode=Preferred` connection string option and make it the default: [#158](https://github.com/mysql-net/MySqlConnector/pull/158).

### 0.10.0

* Change minimum supported .NET Framework version to .NET 4.5.1: [#154](https://github.com/mysql-net/MySqlConnector/issues/154).

### 0.9.2

* Fix MySqlConnection.DataSource with Unix Domain Socket: [#152](https://github.com/mysql-net/MySqlConnector/issues/152).

### 0.9.1

* Fix `SocketException` when calling `OpenAsync`: [#150](https://github.com/mysql-net/MySqlConnector/issues/150).

### 0.9.0

* Implement `Treat Tiny As Boolean` connection string option: [#141](https://github.com/mysql-net/MySqlConnector/issues/141).

### 0.8.0

* Implement `Keep Alive` connection string option: [#132](https://github.com/mysql-net/MySqlConnector/issues/132).

### 0.7.4

* Fix `Packet received out-of-order` exception with `UseCompression=true`: [#146](https://github.com/mysql-net/MySqlConnector/issues/146).

### 0.7.3

* Fix `GetDataTypeName` for `ENUM` and `SET` columns: [#52](https://github.com/mysql-net/MySqlConnector/issues/52), [#71](https://github.com/mysql-net/MySqlConnector/issues/71).

### 0.7.2

* Fix authentication for MySQL Server 5.1: [#139](https://github.com/mysql-net/MySqlConnector/issues/139).

### 0.7.1

* Fix `NextResult` incorrectly returning `true`, which may cause problems with Dapper's `QueryMultiple`: [#135](https://github.com/mysql-net/MySqlConnector/issues/135).
* Reduce memory usage related to `Enum.HasFlag`: [#137](https://github.com/mysql-net/MySqlConnector/issues/137).

### 0.7.0

* Implement stored procedure support: [#19](https://github.com/mysql-net/MySqlConnector/issues/19).
 * Known issue: `NextResult` incorrectly returns `true`, which may cause problems with Dapper's `QueryMultiple`: [#135](https://github.com/mysql-net/MySqlConnector/issues/135).

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
* Thanks to [Ed Ball](https://github.com/ejball) for contributions to this release.

### 0.1.0

* First non-alpha release. Supports core data access scenarios with common ORMs.
