---
lastmod: 2019-05-23
date: 2016-10-16
menu:
  main:
    parent: tutorials
title: Migrating from Connector/NET
weight: 20
---

Migrating from Connector/NET
============================

### Connection String Differences

MySqlConnector has some different default connection string options:

<table class="table table-striped table-hover">
  <thead>
    <th style="width:20%">Option</th>
    <th style="width:20%">MySqlConnector</th>
    <th style="width:20%">Oracle’s Connector/NET</th>
    <th style="width:40%">Notes</th>
  </thead>
  <tr>
    <td><code>AllowPublicKeyRetrieval</code></td>
    <td>Default is <code>false</code></td>
    <td>(not configurable)</td>
    <td>When using <code>sha256_password</code> authentication, this option allows the RSA public key to be retrieved from the server
    (when not using a secure connection). It’s <code>false</code> by default to avoid disclosing the password to a malicious proxy.</td>
  </tr>
  <tr>
    <td><code>CharacterSet</code>, <code>CharSet</code></td>
    <td>Ignored; <code>utf8mb4</code> is always used</td>
    <td>(server-defined)</td>
    <td>MySqlConnector always uses <code>utf8mb4</code> to send and receive strings from MySQL Server. This option may be specified (for backwards compatibility) but it will be ignored.</td>
  </tr>
  <tr>
    <td><code>ConnectionReset</code></td>
    <td>Default is <code>true</code></td>
    <td>Default is <code>false</code></td>
    <td>MySqlConnector always resets pooled connections by default so that the connection is in a known state. This fixes <a href="https://bugs.mysql.com/bug.php?id=77421">MySQL Bug 77421</a>.</td>
  </tr>
  <tr>
    <td><code>IgnoreCommandTransaction</code></td>
    <td>Default is <code>false</code></td>
    <td>(not configurable, effective default is <code>true</code>)</td>
    <td>See remarks under <a href="#mysqlcommand">MySqlCommand</a> below.</td>
  </tr>
  <tr>
    <td><code>LoadBalance</code></td>
    <td>Default is <code>RoundRobin</code></td>
    <td>(not configurable, effective default is <code>FailOver</code>)</td>
    <td>Connector/NET currently has <a href="https://bugs.mysql.com/bug.php?id=81650" title="MySQL bug #81650">a bug</a> that prevents multiple host names being used.</td>
  </tr>
  <tr>
    <td><code>ServerRSAPublicKeyFile</code></td>
    <td>(no default)</td>
    <td>(not configurable)</td>
    <td>Specify a file containing the server’s RSA public key to allow <code>sha256_password</code> authentication over an insecure connection.</td>
  </tr>
</table>

Connector/NET uses `CertificateFile` to specify the client’s private key, unless `SslCert` and `SslKey` are specified, in which case
it is used to specify the server’s CA certificate file; `SslCa` is just an alias for this option. MySqlConnector always uses `CertificateFile`
for the client’s private key (in PFX format); `SslCa` (aka `CACertificateFile`) is a separate option to specify the server’s CA certificate.

Some connection string options that are supported in Connector/NET are not supported in MySqlConnector. For a full list of options that are
supported in MySqlConnector, see the [Connection Options](connection-options).

### Async

Connector/NET implements the standard ADO.NET async methods, and adds some new ones (e.g., `MySqlConnection.BeginTransactionAsync`,
`MySqlDataAdapter.FillAsync`) that don't exist in ADO.NET. None of these methods have an asynchronous implementation,
but all execute synchronously then return a completed `Task`. This is a [longstanding known bug](https://bugs.mysql.com/bug.php?id=70111)
in Connector/NET.

Because the Connector/NET methods aren't actually asynchronous, porting client code to MySqlConnector (which is asynchronous)
can expose bugs that only occur when an async method completes asynchronously and resumes the `await`-ing code
on a background thread. To avoid deadlocks, make sure to [never block on async code](https://blog.stephencleary.com/2012/07/dont-block-on-async-code.html) (e.g., with `.Result`), use async all the way, use `ConfigureAwait` correctly,
and follow the [best practices in async programming](https://msdn.microsoft.com/en-us/magazine/jj991977.aspx).

### Implicit Conversions

Connector/NET allows `MySqlDataReader.GetString()` to be called on many non-textual columns, and will implicitly
convert the value to a `string` (using the current locale). This is a frequent source of locale-dependent bugs, so
MySqlConnector follows typical ADO.NET practice (e.g., SqlClient, npgsql) and disallows this (by throwing an `InvalidCastException`).

To fix this, use the accessor method (e.g., `GetInt32`, `GetDouble`) that matches the column type, or perform an
explicit conversion to `string` by calling `GetValue(x).ToString()` (optionally supplying the right `CultureInfo` to use
for formatting).

### TransactionScope

MySqlConnector adds full distributed transaction support (for client code using [`System.Transactions.Transaction`](https://docs.microsoft.com/en-us/dotnet/api/system.transactions.transaction
)),
while Connector/NET uses regular database transactions. As a result, code that uses `TransactionScope` or `MySqlConnection.EnlistTransaction`
may execute differently with MySqlConnector. To get Connector/NET-compatible behavior, set
`UseXaTransactions=false` in your connection string.

### MySqlConnection

Connector/NET allows a `MySqlConnection` object to be reused after it has been disposed. MySqlConnector requires a new `MySqlConnection`
object to be created. See [#331](https://github.com/mysql-net/MySqlConnector/issues/331) for more details.

The return value of `MySqlConnection.BeginTransactionAsync` has changed from `Task<MySqlTransaction>` to
`ValueTask<MySqlTransaction>` to match the [standard API in .NET Core 3.0](https://github.com/dotnet/corefx/issues/35012).
(This method does always perform I/O, so `ValueTask` is not an optimization for MySqlConnector.)

### MySqlCommand

Connector/NET allows a command to be executed even when `MySqlCommand.Transaction` references a commited, rolled back, or
disposed `MySqlTransaction`. MySqlConnector will throw an `InvalidOperationException` if the `MySqlCommand.Transaction`
property doesn’t reference the active transaction. This fixes <a href="https://bugs.mysql.com/bug.php?id=88611">MySQL Bug 88611</a>.
To disable this strict validation, set <code>IgnoreCommandTransaction=true</code>
in the connection string. See [Transaction Usage](troubleshooting/transaction-usage/) for more details.

### MySqlDataAdapter

Connector/NET provides `MySqlDataAdapter.FillAsync`, `FillSchemaAsync`, and `UpdateAsync` methods, but these methods
have a synchronous implementation. MySqlConnector only adds “Async” methods when they can be implemented asynchronously.
This functionality depends on [dotnet/corefx#20658](https://github.com/dotnet/corefx/issues/20658) being implemented first.
To migrate code, change it to call the synchronous methods instead.

### MySqlGeometry

The Connector/NET `MySqlGeometry` type assumes that the geometry can only be a simple point. MySqlConnector
removes most of the API that is based on those assumptions.

To avoid ambiguity, there are two different factory methods for constructing a `MySqlGeometry`. Use the static factory method `MySqlGeometry.FromMySql` (if you have a byte array containing MySQL's internal format), or `FromWkb` if you have
Well-known Binary bytes.

### Exceptions

For consistency with other ADO.NET providers, MySqlConnector will throw `InvalidOperationException` (instead of `MySqlException`)
for various precondition checks that indicate misuse of the API (and not a problem related to MySQL Server).

### Fixed Bugs

The following bugs in Connector/NET are fixed by switching to MySqlConnector. (~~Strikethrough~~ indicates bugs that have since been fixed in a newer version of Connector/NET, but were fixed first in MySqlConnector.)

* [#14115](https://bugs.mysql.com/bug.php?id=14115): Compound statements are not supported by `MySqlCommand.Prepare`
* [#37283](https://bugs.mysql.com/bug.php?id=37283), [#70587](https://bugs.mysql.com/bug.php?id=70587): Distributed transactions are not supported
* [#50773](https://bugs.mysql.com/bug.php?id=50773): Can’t use multiple connections within one TransactionScope
* [#61477](https://bugs.mysql.com/bug.php?id=61477): `ColumnOrdinal` in schema table is 1-based
* [#66476](https://bugs.mysql.com/bug.php?id=66476): Connection pool uses queue instead of stack
* [#70111](https://bugs.mysql.com/bug.php?id=70111): `Async` methods execute synchronously
* ~~[#70686](https://bugs.mysql.com/bug.php?id=70686): `TIME(3)` and `TIME(6)` fields serialize milliseconds incorrectly~~
* [#72494](https://bugs.mysql.com/bug.php?id=72494), [#83330](https://bugs.mysql.com/bug.php?id=83330): EndOfStreamException inserting large blob with UseCompression=True
* [#73610](https://bugs.mysql.com/bug.php?id=73610): Invalid password exception has wrong number
* [#73788](https://bugs.mysql.com/bug.php?id=73788): Can’t use `DateTimeOffset`
* [#75604](https://bugs.mysql.com/bug.php?id=75604): Crash after 29.4 days of uptime
* [#75917](https://bugs.mysql.com/bug.php?id=75917), [#76597](https://bugs.mysql.com/bug.php?id=76597), [#77691](https://bugs.mysql.com/bug.php?id=77691), [#78650](https://bugs.mysql.com/bug.php?id=78650), [#78919](https://bugs.mysql.com/bug.php?id=78919), [#80921](https://bugs.mysql.com/bug.php?id=80921), [#82136](https://bugs.mysql.com/bug.php?id=82136): “Reading from the stream has failed” when connecting to a server
* [#77421](https://bugs.mysql.com/bug.php?id=77421): Connection is not reset when pulled from the connection pool
* [#78426](https://bugs.mysql.com/bug.php?id=78426): Unknown database exception has wrong number
* [#78760](https://bugs.mysql.com/bug.php?id=78760): Error when using tabs and newlines in SQL statements
* ~~[#78917](https://bugs.mysql.com/bug.php?id=78917), [#79196](https://bugs.mysql.com/bug.php?id=79196), [#82292](https://bugs.mysql.com/bug.php?id=82292), [#89040](https://bugs.mysql.com/bug.php?id=89040): `TINYINT(1)` values start being returned as `sbyte` after `NULL`~~
* ~~[#80030](https://bugs.mysql.com/bug.php?id=80030): Slow to connect with pooling disabled~~
* ~~[#81650](https://bugs.mysql.com/bug.php?id=81650), [#88962](https://bugs.mysql.com/bug.php?id=88962): `Server` connection string option may now contain multiple, comma separated hosts that will be tried in order until a connection succeeds~~
* [#83229](https://bugs.mysql.com/bug.php?id=83329): “Unknown command” exception inserting large blob with UseCompression=True
* ~~[#83649](https://bugs.mysql.com/bug.php?id=83649): Connection cannot be made using IPv6~~
* [#84220](https://bugs.mysql.com/bug.php?id=84220): Cannot call a stored procedure with `.` in its name
* [#84701](https://bugs.mysql.com/bug.php?id=84701): Can’t create a parameter using a 64-bit enum with a value greater than int.MaxValue
* [#85185](https://bugs.mysql.com/bug.php?id=85185): `ConnectionReset=True` does not preserve connection charset
* [#86263](https://bugs.mysql.com/bug.php?id=86263): Transaction isolation level affects all transactions in session
* ~~[#87307](https://bugs.mysql.com/bug.php?id=87307): NextResult hangs instead of timing out~~
* [#87316](https://bugs.mysql.com/bug.php?id=87316): MySqlCommand.CommandTimeout can be set to a negative value
* ~~[#87868](https://bugs.mysql.com/bug.php?id=87868): `ColumnSize` in schema table is incorrect for `CHAR(36)` and `BLOB` columns~~
* ~~[#87876](https://bugs.mysql.com/bug.php?id=87876): `IsLong` is schema table is incorrect for `LONGTEXT` and `LONGBLOB` columns~~
* ~~[#88058](https://bugs.mysql.com/bug.php?id=88058): `decimal(n, 0)` has wrong `NumericPrecision`~~
* [#88124](https://bugs.mysql.com/bug.php?id=88124): CommandTimeout isn’t reset when calling Read/NextResult
* ~~[#88472](https://bugs.mysql.com/bug.php?id=88472): `TINYINT(1)` is not returned as `bool` if `MySqlCommand.Prepare` is called~~
* [#88611](https://bugs.mysql.com/bug.php?id=88611): `MySqlCommand` can be executed even if it has “wrong” transaction
* ~~[#88660](https://bugs.mysql.com/bug.php?id=88660): `MySqlClientFactory.Instance.CreateDataAdapter()` and `CreateCommandBuilder` return `null`~~
* [#89085](https://bugs.mysql.com/bug.php?id=89085): `MySqlConnection.Database` not updated after `USE database;`
* ~~[#89159](https://bugs.mysql.com/bug.php?id=89159), [#97242](https://bugs.mysql.com/bug.php?id=97242): `MySqlDataReader` cannot outlive `MySqlCommand`~~
* [#89335](https://bugs.mysql.com/bug.php?id=89335): `MySqlCommandBuilder.DeriveParameters` fails for `JSON` type
* [#89639](https://bugs.mysql.com/bug.php?id=89639): `ReservedWords` schema contains incorrect data
* [#90086](https://bugs.mysql.com/bug.php?id=90086): `MySqlDataReader` is closed by an unrelated command disposal
* [#91123](https://bugs.mysql.com/bug.php?id=91123): Database names are case-sensitive when calling a stored procedure
* [#91199](https://bugs.mysql.com/bug.php?id=91199): Can't insert `MySqlDateTime` values
* ~~[#91751](https://bugs.mysql.com/bug.php?id=91751): `YEAR` column retrieved incorrectly with prepared command~~
* ~~[#91752](https://bugs.mysql.com/bug.php?id=91752): `00:00:00` is converted to `NULL` with prepared command~~
* [#91753](https://bugs.mysql.com/bug.php?id=91753): Unnamed parameter not supported by `MySqlCommand.Prepare`
* [#91754](https://bugs.mysql.com/bug.php?id=91754): Inserting 16MiB `BLOB` shifts it by four bytes when prepared
* [#91770](https://bugs.mysql.com/bug.php?id=91770): `TIME(n)` column loses microseconds with prepared command
* [#92367](https://bugs.mysql.com/bug.php?id=92367): `MySqlDataReader.GetDateTime` and `GetValue` return inconsistent values
* [#92465](https://bugs.mysql.com/bug.php?id=92465): “There is already an open DataReader” `MySqlException` thrown from `TransactionScope.Dispose`
* [#92734](https://bugs.mysql.com/bug.php?id=92734): `MySqlParameter.Clone` doesn't copy all property values
* [#92789](https://bugs.mysql.com/bug.php?id=92789): Illegal connection attributes written for non-ASCII values
* ~~[#92912](https://bugs.mysql.com/bug.php?id=92912): `MySqlDbType.LongText` values encoded incorrectly with prepared statements~~
* [#92982](https://bugs.mysql.com/bug.php?id=92982), [#93399](https://bugs.mysql.com/bug.php?id=93399): `FormatException` thrown when connecting to MySQL Server 8.0.13
* [#93047](https://bugs.mysql.com/bug.php?id=93047): `MySqlDataAdapter` throws timeout exception when an error occurs
* ~~[#93202](https://bugs.mysql.com/bug.php?id=93202): Connector runs `SHOW VARIABLES` when connection is made~~
* [#93220](https://bugs.mysql.com/bug.php?id=93220): Can’t call FUNCTION when parameter name contains parentheses
* [#93370](https://bugs.mysql.com/bug.php?id=93370): `MySqlParameterCollection.Add` precondition check isn't consistent
* [#93374](https://bugs.mysql.com/bug.php?id=93374): `MySqlDataReader.GetStream` throws `IndexOutOfRangeException`
* [#93825](https://bugs.mysql.com/bug.php?id=93825): `MySqlException` loses data when serialized
* [#94075](https://bugs.mysql.com/bug.php?id=94075): `MySqlCommand.Cancel` throws exception
* [#94760](https://bugs.mysql.com/bug.php?id=94760): `MySqlConnection.OpenAsync(CancellationToken)` doesn’t respect cancellation token
* [#95348](https://bugs.mysql.com/bug.php?id=95348): Inefficient query when executing stored procedures
* [#95436](https://bugs.mysql.com/bug.php?id=95436): Client doesn't authenticate with PEM certificate
* ~~[#95984](https://bugs.mysql.com/bug.php?id=95984): “Incorrect arguments to mysqld_stmt_execute” using prepared statement with `MySqlDbType.JSON`~~
* [#95986](https://bugs.mysql.com/bug.php?id=95986): “Incorrect integer value” using prepared statement with `MySqlDbType.Int24`
* ~~[#96355](https://bugs.mysql.com/bug.php?id=96355), [#96614](https://bugs.mysql.com/bug.php?id=96614): `Could not load file or assembly 'Renci.SshNet'` when opening connection~~
* [#96498](https://bugs.mysql.com/bug.php?id=96498): `WHERE` clause using `MySqlGeometry` as parameter finds no rows
* [#96499](https://bugs.mysql.com/bug.php?id=96499): `MySqlException` when inserting a `MySqlGeometry` value
* [#96500](https://bugs.mysql.com/bug.php?id=96500): `MySqlDataReader.GetFieldValue<MySqlGeometry>` throws `InvalidCastException`
* [#96636](https://bugs.mysql.com/bug.php?id=96636): `MySqlConnection.Open()` slow under load when using SSL
* [#96717](https://bugs.mysql.com/bug.php?id=96717): Not compatible with MySQL Server 5.0
* [#97067](https://bugs.mysql.com/bug.php?id=97067): Aggregate functions on BIT(n) columns return wrong result
* [#97300](https://bugs.mysql.com/bug.php?id=97300): `GetSchemaTable()` returns table for stored procedure with output parameters
* ~~[#97448](https://bugs.mysql.com/bug.php?id=97448): Connecting fails if more than one IP is found in DNS for a named host~~
* [#97473](https://bugs.mysql.com/bug.php?id=97473): `MySqlConnection.Clone` discloses connection password
* [#97738](https://bugs.mysql.com/bug.php?id=97738): Cannot use PEM files when account uses `require subject`
* [#97872](https://bugs.mysql.com/bug.php?id=97872): `KeepAlive` in connection string throws exception on .NET Core
* [#98322](https://bugs.mysql.com/bug.php?id=98322): `new MySqlConnection(null)` throws `NullReferenceException`
* [#99091](https://bugs.mysql.com/bug.php?id=99091): Unexpected return value getting integer for `TINYINT(1)` column
* [#99793](https://bugs.mysql.com/bug.php?id=99793): Prepared stored procedure command doesn't verify parameter types
