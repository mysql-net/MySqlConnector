# Tests

## Integration Tests

The `IntegrationTests` project is intended to verify that MySqlConnector performs as expected with various MySQL-compatible servers.
It also verifies that MySqlConnector and MySQL Connector/NET (MySql.Data) have similar behavior, except where MySqlConnector [fixes known bugs](https://mysqlconnector.net/tutorials/migrating-from-connector-net/#fixed-bugs) or makes other [non-backwards-compatible changes](https://mysqlconnector.net/tutorials/migrating-from-connector-net/).

The tests require a MySQL server. The simplest way to run one is with [Docker](https://www.docker.com/community-edition):

    docker run -d --rm --pull always --name mysqlconnector -e MYSQL_ROOT_PASSWORD=pass -p 3306:3306 --tmpfs /var/lib/mysql mysql:9.5 --max-allowed-packet=96M --character-set-server=utf8mb4 --disable-log-bin --local-infile=1 --max-connections=250
    docker exec mysqlconnector mysql -uroot -ppass -e "INSTALL COMPONENT 'file://component_query_attributes'; CREATE USER 'caching-sha2-user'@'%' IDENTIFIED WITH caching_sha2_password BY 'Cach!ng-Sh@2-Pa55'; GRANT ALL PRIVILEGES ON *.* TO 'caching-sha2-user'@'%';"

Copy the file `IntegrationTests/config.json.example` to `IntegrationTests/config.json`, then edit
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
  * `CachingSha2Password`: a user named `caching-sha2-user` exists on your server and uses the `caching_sha2_password` auth plugin
  * `Ed25519`: a user named `ed25519user` exists on your server and uses the `client_ed25519` auth plugin
  * `ErrorCodes`: server returns error codes in error packet (some MySQL proxies do not)
  * `Json`: the `JSON` data type (MySQL 5.7 and later)
  * `LargePackets`: large packets (over 4MB)
  * `Redirection`: server supports sending redirection information in a server variable in the first OK packet
  * `RoundDateTime`: server rounds `datetime` values to the specified precision (not implemented in MariaDB)
  * `RsaEncryption`: server supports RSA public key encryption (for `sha256_password` and `caching_sha2_password`)
  * `SessionTrack`: server supports `CLIENT_SESSION_TRACK` capability (MySQL 5.7 and later)
  * `Sha256Password`: a user named `sha256user` exists on your server and uses the `sha256_password` auth plugin
  * `StoredProcedures`: create and execute stored procedures
  * `Timeout`: server can cancel queries promptly (so timed tests don't time out)
  * `Tls11`: server supports TLS 1.1
  * `Tls12`: server supports TLS 1.2
  * `Tls13`: server supports TLS 1.3
  * `TlsFingerprintValidation`: server provides a hash of the TLS certificate fingerprint in the first OK packet
  * `UnixDomainSocket`: server is accessible via a Unix domain socket
  * `UuidToBin`: server supports `UUID_TO_BIN` (MySQL 8.0 and later)

## Running Tests

There are two ways to run the tests: command line and Visual Studio.

### Visual Studio 2022

After building the solution, you should see a list of tests in the Test Explorer.  Click "Run All" to run them.

### Command Line

To run the tests against MySqlConnector:

```
cd tests\IntegrationTests
dotnet test -c Release
```

To run the tests against MySql.Data:

```
cd tests\IntegrationTests
dotnet restore /p:Configuration=MySqlData && dotnet test -c MySqlData
```
