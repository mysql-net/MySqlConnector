# Tests

## Side-by-side Tests

The `SideBySide` project is intended to verify that MySqlConnector doesn't break compatibility
with Connector/NET and that [known bugs have been fixed](https://mysql-net.github.io/MySqlConnector/tutorials/migrating-from-connector-net/#fixed-bugs).

The tests require a MySQL server. The simplest way to run one is with [Docker](https://www.docker.com/community-edition):

    docker run -d --rm --name mysqlconnector -e MYSQL_ROOT_PASSWORD=pass -p 3306:3306 mysql:5.7 --max-allowed-packet=96M --character-set-server=utf8mb4

Copy the file `SideBySide/config.json.example` to `SideBySide/config.json`, then edit
the `config.json` file in order to connect to your server. If you are using the Docker
command above, then the default options will work and do not need to be modified.
Otherwise, set the following options appropriately:

* `Data.ConnectionString`: The full connection string to your server. You should specify a database name. If the database does not exist, the test will attempt to create it.
* `Data.PasswordlessUser`: (Optional) A user account in your database with no password and no roles.
* `Data.SecondaryDatabase`: (Optional) A second database on your server that the test user has permission to access.
* `Data.CertificatesPath`: (Optional) The absolute path to the server and client certificates folder (i.e., the `.ci/server/certs` folder in this repo).
* `Data.MySqlBulkLoaderLocalCsvFile`: (Optional) The path to a test CSV file.
* `Data.MySqlBulkLoaderLocalTsvFile`: (Optional) The path to a test TSV file.
* `Data.UnsupportedFeatures`: A comma-delimited list of `ServerFeature` enum values that your test database server does *not* support
  * `Json`: the `JSON` data type (MySQL 5.7 and later)
  * `StoredProcedures`: create and execute stored procedures
  * `Sha256Password`: a user named `sha256user` exists on your server and uses the `sha256_password` auth plugin
  * `RsaEncryption`: server supports RSA public key encryption (for `sha256_password` and `caching_sha2_password`)
  * `LargePackets`: large packets (over 4MB)
  * `CachingSha2Password`: a user named `caching-sha2-user` exists on your server and uses the `caching_sha2_password` auth plugin
  * `SessionTrack`: server supports `CLIENT_SESSION_TRACK` capability (MySQL 5.7 and later)
  * `Timeout`: server can cancel queries promptly (so timed tests don't time out)
  * `ErrorCodes`: server returns error codes in error packet (some MySQL proxies do not)
  * `Tls11`: server supports TLS 1.1
  * `Tls12`: server supports TLS 1.2
  * `RoundDateTime`: server rounds `datetime` values to the specified precision (not implemented in MariaDB)
  * `UuidToBin`: server supports `UUID_TO_BIN` (MySQL 8.0 and later)

## Running Tests

There are two ways to run the tests: command line and Visual Studio.

### Visual Studio 2017

After building the solution, you should see a list of tests in the Test Explorer.  Click "Run All" to run them.

### Command Line

To run the tests against MySqlConnector:

```
cd tests\SideBySide
dotnet test -c Release
```

To run the tests against MySql.Data:

```
cd tests\SideBySide
dotnet restore /p:Configuration=Baseline && dotnet test -c Baseline
```
