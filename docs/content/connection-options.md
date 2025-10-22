---
lastmod: 2024-01-22
date: 2016-10-16
title: Connection Options
customtitle: MySQL Connection String Options for .NET/C#
description: A MySqlConnection is configured using a connection string. The connection string options are key/value pairs described on this page.
weight: 30
menu:
  main:
---

# MySQL .NET Connection String Options

The simplest MySQL connection string for C# is:

`new MySqlConnection("Server=YOURSERVER;User ID=YOURUSERID;Password=YOURPASSWORD")`

For all the other options, see the tables below. MySqlConnector supports most of Oracle’s
[Connector/NET connection options](https://dev.mysql.com/doc/connector-net/en/connector-net-8-0-connection-options.html).

There are also several unique options that are supported only by MySqlConnector, a replacement for `MySql.Data` that [fixes bugs](/tutorials/migrating-from-connector-net/#fixed-bugs),
adds new features, and improves database access performance. [Install it now](/overview/installing/).

## Connection Options

These options specify how to connect and authenticate to a MySQL database.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Description</th>
  </thead>
  <tr id="Host">
    <td><a name="Server"></a>Server, Host, Data Source, DataSource, Address, Addr, Network Address</td>
    <td>localhost</td>
    <td>
      <p>The host name or network address of the MySQL Server to which to connect. Multiple hosts can be specified in a comma-delimited list.</p>
      <p>On Unix-like systems, this can be a fully qualified path to a MySQL socket file, which will cause a Unix socket to be used instead of a TCP/IP socket. Only a single socket name can be specified.</p>
    </td>
  </tr>
  <tr id="Port">
    <td>Port</td>
    <td>3306</td>
    <td>The TCP port on which MySQL Server is listening for connections.</td>
  </tr>
  <tr id="UserId">
    <td>User ID, UserID, Username, Uid, User name, User</td>
    <td></td>
    <td>The MySQL user ID.</td>
  </tr>
  <tr id="Password">
    <td>Password, pwd</td>
    <td></td>
    <td>
      <p>The password for the MySQL user.</p>
      <p>For systems that use frequently-updated authentication tokens (such as Amazon Aurora RDS with IAM Authentication), leave this value empty, and set <a href="/api/mysqlconnector/mysqlconnection/providepasswordcallback/"><code>MySqlConnection.ProvidePasswordCallback</code></a> to a delegate that will provide the password (or authentication token) on demand before calling <a href="/api/mysqlconnector/mysqlconnection/open/"><code>MySqlConnection.Open</code></a>. This retains the benefits of connection pooling.</p>
    </td>
  </tr>
  <tr id="Database">
    <td>Database, Initial Catalog</td>
    <td></td>
    <td>(Optional) The case-sensitive name of the initial database to use. This may be required if the MySQL user account only has access rights to particular databases on the server.</td>
  </tr>
  <tr id="LoadBalance">
    <td>Load Balance, LoadBalance</td>
    <td>RoundRobin</td>
    <td><p>The load-balancing strategy to use when <code>Host</code> contains multiple, comma-delimited, host names.
      The options include:</p>
      <dl>
        <dt>RoundRobin</dt>
        <dd>Each new connection opened for this connection pool uses the next host name (sequentially with wraparound). Requires <code>Pooling=True</code>. This is the default if <code>Pooling=True</code>.</dd>
        <dt>FailOver</dt>
        <dd>Each new connection tries to connect to the first host; subsequent hosts are used only if connecting to the first one fails. This is the default if <code>Pooling=False</code>.</dd>
        <dt>Random</dt>
        <dd>Servers are tried in a random order.</dd>
        <dt>LeastConnections</dt>
        <dd>Servers are tried in ascending order of number of currently-open connections in this connection pool. Requires <code>Pooling=True</code>.</dd>
      </dl>
    </td>
  </tr>
  <tr id="ConnectionProtocol">
    <td>Connection Protocol, ConnectionProtocol, Protocol</td>
    <td>Socket</td>
    <td>How to connect to the MySQL Server. This option has the following values:
      <ul>
        <li><b>Socket</b> (default): Use TCP/IP sockets.</li>
        <li><b>Unix</b>: Use a Unix socket.</li>
        <li><b>Pipe</b>: Use a Windows named pipe.</li>
      </ul>
    </td>
  </tr>
  <tr id="PipeName">
    <td>Pipe Name, Pipe, PipeName</td>
    <td>MYSQL</td>
    <td>The name of the Windows named pipe to use to connect to the server. You must also set <code>ConnectionProtocol=pipe</code> to used named pipes.</td>
  </tr>
</table>

### Connecting to Multiple Servers

The `Server` option supports multiple comma-delimited host names.
The `LoadBalance` option controls how load is distributed across backend servers.
Some of these options (`RoundRobin`, `LeastConnections`) only take effect if `Pooling=True`; however `Random` and `FailOver` can be used with `Pooling=False`.

* `RoundRobin` (default), `Random`: A total of `MaximumPoolSize` connections will be opened, but they
may be unevenly distributed across back ends.
* `LeastConnections`: A total of `MaximumPoolSize` connections will be opened, and they will be evenly
distributed across back ends. The active connections will be selected from the pool in least-recently-used
order, which does not ensure even load across the back ends. You should set `MaximumPoolSize` to the
number of servers multiplied by the desired maximum number of open connections per backend server.
* `Failover`: All connections will initially be made to the first server in the list. You should set `MaximumPoolSize`
to the maximum number of open connections you want per server.

## SSL/TLS Options

These are the options that need to be used in order to configure a connection to use SSL/TLS.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Description</th>
  </thead>
  <tr id="SslMode">
    <td>SSL Mode, SslMode</td>
    <td>Preferred</td>
    <td>This option has the following values:
      <ul>
        <li><b>Preferred</b> - (this is the default). Use SSL if the server supports it.</li>
        <li><b>Disabled</b> (or <b>None</b>) - Do not use SSL.</li>
        <li><b>Required</b> - Always use SSL. Deny connection if server does not support SSL. Does not validate CA or hostname.</li>
        <li><b>VerifyCA</b> - Always use SSL. Validates the CA but tolerates hostname mismatch.</li>
        <li><b>VerifyFull</b> - Always use SSL. Validates CA and hostname.</li>
      </ul>
    </td>
  </tr>
  <tr id="CertificateFile">
    <td>Certificate File, CertificateFile</td>
    <td></td>
    <td>
      <p>The path to a certificate file in PKCS #12 (.pfx) format containing a bundled Certificate and Private Key used for mutual authentication. To create a PKCS #12 bundle from a PEM encoded Certificate and Key, use <code>openssl pkcs12 -in cert.pem -inkey key.pem -export -out bundle.pfx</code>. This option should not be specified if <code>SslCert</code> and <code>SslKey</code> are used.</p>
      <p>If the certificate can't be loaded from a file path, leave this value empty and set <a href="/api/mysqlconnector/mysqlconnection/provideclientcertificatescallback/"><code>MySqlConnection.ProvideClientCertificatesCallback</code></a> before calling <a href="/api/mysqlconnector/mysqlconnection/open/"><code>MySqlConnection.Open</code></a>. The property should be set to an async delegate that will populate a <code>X509CertificateCollection</code> with the client certificate(s) needed to connect.</p>
    </td>
  </tr>
  <tr id="CertificatePassword">
    <td>Certificate Password, CertificatePassword</td>
    <td></td>
    <td>The password for the certificate specified using the <code>CertificateFile</code> option. Not required if the certificate file is not password protected.</td>
  </tr>
  <tr id="CertificateStoreLocation">
    <td>Certificate Store Location, CertificateStoreLocation</td>
    <td>None</td>
    <td>Specifies whether the connection should be encrypted with a certificate from the Certificate Store on the machine. The default value of <code>None</code> means the certificate store is not used; a value of <code>CurrentUser</code> or <code>LocalMachine</code> uses the specified store.</td>
  </tr>
  <tr id="CertificateThumbprint">
    <td>Certificate Thumbprint, CertificateThumbprint</td>
    <td></td>
    <td>Specifies which certificate should be used from the certificate store specified in the setting above. This option must be used to indicate which certificate in the store should be used for authentication.</td>
  </tr>
  <tr id="SslCert">
    <td>SSL Cert, SslCert, Ssl-Cert</td>
    <td></td>
    <td>The path to the client’s SSL certificate file in PEM format. <code>SslKey</code> must also be specified, and <code>CertificateFile</code> should not be. This option is not supported on <code>netstandard2.0</code>.</td>
  </tr>
  <tr id="SslKey">
    <td>SSL Key, SslKey, Ssl-Key</td>
    <td></td>
    <td>The path to the client’s SSL private key in PEM format. <code>SslCert</code> must also be specified, and <code>CertificateFile</code> should not be.</td>
  </tr>
  <tr id="SslCa">
    <td>SSL CA, CA Certificate File, CACertificateFile, SslCa, Ssl-Ca</td>
    <td></td>
    <td>
      <p>The path to a CA certificate file in a PEM Encoded (.pem) format. This should be used with <code>SslMode=VerifyCA</code> or <code>SslMode=VerifyFull</code> to enable verification of a CA certificate that is not trusted by the operating system’s certificate store.</p>
      <p>To provide a custom callback to validate the remote certificate, leave this option empty and set <code>SslMode</code> to <code>Required</code> (or <code>Preferred</code>), then set <a href="/api/mysqlconnector/mysqlconnection/remotecertificatevalidationcallback/"><code>MySqlConnection.RemoteCertificateValidationCallback</code></a> before calling <a href="/api/mysqlconnector/mysqlconnection/open/"><code>MySqlConnection.Open</code></a>. The property should be set to a delegate that will validate the remote certificate, as per <a href="https://docs.microsoft.com/en-us/dotnet/api/system.net.security.remotecertificatevalidationcallback" title="RemoteCertificateValidationCallback Delegate (MSDN)">the documentation</a>.</p>
    </td>
  </tr>
  <tr id="TlsVersion">
    <td>TLS Version, TlsVersion, Tls-Version</td>
    <td></td>
    <td>The TLS versions which may be used during TLS negotiation. The default value of <code>null</code> allows the OS to determine the TLS version to use (see <a href="https://docs.microsoft.com/en-us/dotnet/framework/network-programming/tls" title="Transport Layer Security (TLS) best practices with the .NET Framework">documentation</a>); this is the recommended setting. Otherwise, to restrict the versions that can be used, specify a comma-delimited list of versions taken from the following: <code>TLS 1.0</code>, <code>TLS 1.1.</code>, <code>TLS 1.2</code>, <code>TLS 1.3</code>. (This option allows each version to be specified in a few different formats: <code>Tls12</code>, <code> Tlsv1.2</code>, <code>TLS 1.2</code>, <code>Tls v1.2</code>; they are treated equivalently.)</td>
  </tr>
  <tr id="TlsCipherSuites">
    <td>TLS Cipher Suites,TlsCipherSuites</td>
    <td></td>
    <td>The TLS cipher suites which may be used during TLS negotiation. The default value (the empty string) allows the OS to determine the TLS cipher suites to use; this is the recommended setting. Otherwise, specify a comma-delimited list of <a href="https://docs.microsoft.com/en-us/dotnet/api/system.net.security.tlsciphersuite"><code>TlsCipherSuite</code> enum values</a> to allow just those cipher suites. (This option is only supported on Linux when using .NET Core 3.1 or .NET 5.0 or later.)</td>
  </tr>
</table>

## Connection Pooling Options

Connection pooling is enabled by default. These options are used to configure it.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Description</th>
  </thead>
  <tr id="Pooling">
    <td>Pooling</td>
    <td>true</td>
    <td>Enables connection pooling. When pooling is enabled, <code>MySqlConnection.Open</code>/<code>OpenAsync</code> retrieves an open connection from the pool if one is available, and <code>Close</code>/<code>Dispose</code>/<code>DisposeAsync</code> returns the open connection to the pool. If there are no available connections in the pool, and the pool hasn’t reached <code>MaximumPoolSize</code> connections, a new connection will be opened; otherwise, the call to <code>Open</code>/<code>OpenAsync</code> blocks until a connection becomes available or <code>ConnectionTimeout</code> is reached.</td>
  </tr>
  <tr id="ConnectionLifeTime">
    <td>Connection Lifetime, ConnectionLifeTime</td>
    <td>0</td>
    <td>Connections that are returned to the pool will be closed if it’s been more than <code>ConnectionLifeTime</code> seconds since the connection was created. The default value of zero (0) means pooled connections will never incur a <code>ConnectionLifeTime</code> timeout. This can be useful when multiple database servers are being used, as it will force existing connections to be closed, which may spread load more evenly.</td>
  </tr>
  <tr id="ConnectionReset">
    <td>Connection Reset, ConnectionReset</td>
    <td><code>true</code></td>
    <td>If <code>true</code>, all connections retrieved from the pool will have been reset. The default value of <code>true</code> ensures that the connection is in the same state whether it’s newly created or retrieved from the pool. A value of <code>false</code> avoids making an additional server round trip to reset the connection, but the connection state is not reset, meaning that session variables and other session state changes from any previous use of the connection are carried over. Additionally (if <code>Connection Reset</code> is <code>false</code>), when <code>MySqlConnection.Open</code> returns a connection from the pool (instead of opening a new one), the connection may be invalid (and throw an exception on first use) if the server has closed the connection.</td>
  </tr>
  <tr id="DeferConnectionReset">
    <td>Defer Connection Reset, DeferConnectionReset</td>
    <td></td>
    <td>This option was obsoleted in MySqlConnector 2.0.</td>
  </tr>
  <tr id="ConnectionIdleTimeout">
    <td>Connection Idle Timeout, ConnectionIdleTimeout</td>
    <td>180</td>
    <td>The amount of time (in seconds) that a connection can remain idle in the pool. Any connection above <code>MinimumPoolSize</code> connections that is idle for longer than <code>ConnectionIdleTimeout</code> is subject to being closed by a background task. The background task runs every minute, or half of <code>ConnectionIdleTimeout</code>, whichever is more frequent. A value of zero (0) means pooled connections will never incur a ConnectionIdleTimeout, and if the pool grows to its maximum size, it will never get smaller.</td>
  </tr>
  <tr id="MinimumPoolSize">
    <td>Minimum Pool Size, Min Pool Size, MinimumPoolSize, minpoolsize</td>
    <td>0</td>
    <td>The minimum number of connections to leave in the pool if ConnectionIdleTimeout is reached.</td>
  </tr>
  <tr id="MaximumPoolSize">
    <td>Maximum Pool Size, Max Pool Size, MaximumPoolSize, maxpoolsize</td>
    <td>100</td>
    <td>The maximum number of connections allowed in the pool.</td>
  </tr>
  <tr id="DnsCheckInterval">
    <td>DNS Check Interval, DnsCheckInterval</td>
    <td>0</td>
    <td>The number of seconds between checks for DNS changes, or 0 to disable periodic checks.
    If the periodic check determines that one of the <code>Server</code> hostnames resolves to a different IP address, the pool will be cleared.
    This is useful in HA scenarios where failover is accomplished by changing the IP address to which a hostname resolves.
    Existing connections in the pool may have valid TCP connections to a server that is no longer responding or has been marked readonly;
    clearing the pool (when DNS changes) forces all these existing connections to be reestablished.</td>
  </tr>
</table>

## Other Options

These are the other options that MySqlConnector supports. They are set to sensible defaults and typically do not need to be tweaked.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Description</th>
  </thead>
  <tr id="AllowLoadLocalInfile">
    <td>Allow Load Local Infile, AllowLoadLocalInfile</td>
    <td>false</td>
    <td>Allows the <code>LOAD DATA LOCAL</code> command to request files from the client. This is disabled by
      default as a <a href="/troubleshooting/load-data-local-infile/" title="Using Load Data Local Infile">security precaution</a>.
      In order to use <code>MySqlBulkLoader</code> and set its <code>Local</code> property to <code>true</code>, you
      must set this option to <code>True</code> in  your connection string.</td>
  </tr>
  <tr id="AllowPublicKeyRetrieval">
    <td>Allow Public Key Retrieval, AllowPublicKeyRetrieval</td>
    <td>false</td>
    <td>If the user account uses <code>sha256_password</code> authentication, the password must be protected during transmission; TLS is the preferred mechanism for this,
      but if it is not available then RSA public key encryption will be used. To specify the server’s RSA public key, use the <code>ServerRSAPublicKeyFile</code> connection
      string setting, or set <code>AllowPublicKeyRetrieval=True</code> to allow the client to automatically request the public key from the server. Note that <code>AllowPublicKeyRetrieval=True</code>
      could allow a malicious proxy to perform a MITM attack to get the plaintext password, so it is <code>False</code> by default and must be explicitly enabled.</td>
  </tr>
  <tr id="AllowUserVariables">
    <td>Allow User Variables, AllowUserVariables</td>
    <td>false</td>
    <td>Allows user-defined variables (prefixed with <code>@</code>) to be used in SQL statements. The default value (<code>false</code>)
    only allows <code>@</code>-prefixed names to refer to command parameters.</td>
  </tr>
  <tr id="AllowZeroDateTime">
    <td>Allow Zero DateTime, AllowZeroDateTime</td>
    <td>false</td>
    <td>If set to <c>true</c> all <code>DATE</code>, <code>DATETIME</code> and <code>TIMESTAMP</code> columns are returned as <code>MySqlDateTime</code> objects instead of <code>DateTime</code>.
    This allows the special “zero” date value <code>0000-00-00</code> to be retrieved from the database. If <code>false</code> (the default)
    date columns are returned as <code>DateTime</code> values, and an exception is thrown for unrepresentable dates.</td>
  </tr>
  <tr id="ApplicationName">
    <td>Application Name, ApplicationName</td>
    <td>null</td>
    <td><p>Sets the <c>program_name</c> connection attribute passed to MySQL Server. This value may be displayed by diagnostic tools,
    e.g., as the “Program” column in “Client Connections” in <a href="https://www.mysql.com/products/workbench/">MySQL Workbench</a>.
    It also sets the connection pool name reported by the <code>pool.name</code> tag associated with connection pool metrics.</p>
    <p>This connection string option is deprecated and is provided for backwards compatibility. Newer applications should
    use <a href="/api/MySqlConnector/MySqlDataSourceBuilder/UseName/"><code>MySqlDataSourceBuilder.UseName</code></a> instead.
    </p></td>
    </td>
  </tr>
  <tr id="AutoEnlist">
    <td>Auto Enlist, AutoEnlist</td>
    <td>true</td>
    <td>If <code>true</code> (default), <code>MySqlConnection</code> will detect if there is an active <code>TransactionScope</code> when it's opened and automatically enlist in it. If <code>false</code>, connections must be manually enlisted by calling <code>EnlistTransaction</code>.</td>
  </tr>
  <tr id="CancellationTimeout">
    <td>Cancellation Timeout, CancellationTimeout</td>
    <td>2</td>
    <td>The length of time (in seconds) to wait for a query to be canceled when <code>MySqlCommand.CommandTimeout</code> expires, or zero for no timeout. If a response isn’t received from the server in this
    time, the local socket will be closed and a <code>MySqlException</code> will be thrown.</td>
  </tr>
  <tr id="CharacterSet">
    <td>Character Set, CharSet, CharacterSet</td>
    <td>utf8mb4</td>
    <td>MySqlConnector always uses <code>utf8mb4</code> to send and receive strings from MySQL Server. This option may be specified (for backwards compatibility) but it will be ignored.</td>
  </tr>
  <tr id="ConnectionTimeout">
    <td>Connection Timeout, Connect Timeout, ConnectionTimeout</td>
    <td>15</td>
    <td>The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.</td>
  </tr>
  <tr id="ConvertZeroDateTime">
    <td>Convert Zero DateTime, ConvertZeroDateTime</td>
    <td>false</td>
    <td>True to have <code>MySqlDataReader.GetValue()</code> and <code>MySqlDataReader.GetDateTime()</code> return <code>DateTime.MinValue</code> for date or datetime columns that have disallowed values.</td>
  </tr>
  <tr id="DateTimeKind">
    <td>DateTime Kind, DateTimeKind</td>
    <td>Unspecified</td>
    <td>The <code>DateTimeKind</code> used when <code>MySqlDataReader</code> returns a <code>DateTime</code>. If set to <code>Utc</code> or <code>Local</code>,
    a <code>MySqlException</code> will be thrown if a <code>DateTime</code> command parameter has a <code>Kind</code> of <code>Local</code> or <code>Utc</code>,
    respectively.</td>
  </tr>
  <tr id="DefaultCommandTimeout">
    <td>Default Command Timeout, Command Timeout, DefaultCommandTimeout</td>
    <td>30</td>
    <td>The length of time (in seconds) each command can execute before the query is cancelled on the server, or zero to disable timeouts.
      See the note in the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.commandtimeout">Microsoft documentation</a>
      for more explanation of how this is determined.</td>
  </tr>
  <tr id="GuidFormat">
    <td>GUID Format, GuidFormat</td>
    <td>Default</td>
    <td><p>Determines which column type (if any) should be read as a <code>System.Guid</code>.
      The options include:</p>
      <dl>
        <dt>Char36</dt>
        <dd>All <code>CHAR(36)</code> columns are read/written as a <code>Guid</code> using lowercase hex with hyphens, which matches <a href="https://dev.mysql.com/doc/refman/8.0/en/miscellaneous-functions.html#function_uuid"><code>UUID()</code></a>.</dd>
        <dt>Char32</dt>
        <dd>All <code>CHAR(32)</code> columns are read/written as a <code>Guid</code> using lowercase hex without hyphens.</dd>
        <dt>Binary16</dt>
        <dd>All <code>BINARY(16)</code> columns are read/written as a <code>Guid</code> using big-endian byte order, which matches <a href="https://dev.mysql.com/doc/refman/8.0/en/miscellaneous-functions.html#function_uuid-to-bin"><code>UUID_TO_BIN(x)</code></a>.</dd>
        <dt>TimeSwapBinary16</dt>
        <dd>All <code>BINARY(16)</code> columns are read/written as a <code>Guid</code> using big-endian byte order with time parts swapped, which matches <a href="https://dev.mysql.com/doc/refman/8.0/en/miscellaneous-functions.html#function_uuid-to-bin"><code>UUID_TO_BIN(x,1)</code></a>.</dd>
        <dt>LittleEndianBinary16</dt>
        <dd>All <code>BINARY(16)</code> columns are read/written as a <code>Guid</code> using little-endian byte order, i.e. the byte order used by <code>Guid.ToByteArray()</code> and the <code>Guid(byte[])</code> constructor.</dd>
        <dt>None</dt>
        <dd>No column types are automatically read as a <code>Guid</code>.</dd>
        <dt>Default</dt>
        <dd>Same as <code>Char36</code> if <code>OldGuids=False</code>; same as <code>LittleEndianBinary16</code> if <code>OldGuids=True</code>.</dd>
      </dl>
    </td>
  </tr>
  <tr id="IgnoreCommandTransaction">
    <td>Ignore Command Transaction, IgnoreCommandTransaction</td>
    <td>false</td>
    <td>If <code>true</code>, the value of <code>MySqlCommand.Transaction</code> is ignored when commands are executed.
    This matches the Connector/NET behaviour and can make porting code easier. For more information, see <a href="troubleshooting/transaction-usage/">Transaction Usage</a>.</td>
  </tr>
  <tr id="IgnorePrepare">
    <td>Ignore Prepare, IgnorePrepare</td>
    <td>false</td>
    <td>If <code>true</code>, calls to <code>MySqlCommand.Prepare(Async)</code> are ignored (and will be no-ops).
    This option is provided for backwards compatibility with MySQL Connector/NET (before 8.0.23) and should not be used.</td>
  </tr>
  <tr id="InteractiveSession">
    <td>Interactive Session, Interactive, InteractiveSession</td>
    <td>false</td>
    <td>If <code>true</code>, the session <code>wait_timeout</code> variable is initialized from the global <code>interactive_timeout</code> value instead of the global <code>wait_timeout</code> value.</td>
  </tr>
  <tr id="Keepalive">
    <td>Keep Alive, Keepalive</td>
    <td>0</td>
    <td><p>TCP Keepalive idle time (in seconds). A value of 0 indicates that the OS default keepalive settings are used; a value greater than 0 is the idle connection time (in seconds) before the first keepalive packet is sent.</p>
    <p>On Windows, this option is always supported. On non-Windows platforms, this option only takes effect in .NET Core 3.0 and later. For earlier versions of .NET Core, the OS Default keepalive settings are used instead.</p></td>
  </tr>
  <tr id="NoBackslashEscapes">
    <td>No Backslash Escapes, NoBackslashEscapes</td>
    <td>false</td>
    <td>If <code>true</code>, backslashes are not escaped in string parameters. Set this to <code>true</code> if the server’s SQL mode includes <a href="https://dev.mysql.com/doc/refman/8.0/en/sql-mode.html#sqlmode_no_backslash_escapes"><code>NO_BACKSLASH_ESCAPES</code></a>.</td>
  </tr>
  <tr id="OldGuids">
    <td>Old Guids, OldGuids</td>
    <td>false</td>
    <td>Obsolete; use the <code>GuidFormat</code> option instead.</td>
  </tr>
  <tr id="PersistSecurityInfo">
    <td>Persist Security Info, PersistSecurityInfo</td>
    <td>false</td>
    <td>When set to <code>false</code> or no (strongly recommended), security-sensitive information, such as the password, is not returned as part of the connection string if the connection is open or has ever been in an open state. Resetting the connection string resets all connection string values, including the password. Recognized values are true, false, yes, and no.</td>
  </tr>
  <tr id="Pipelining">
    <td>Pipelining</td>
    <td>true</td>
    <td>When set to <code>true</code>, queries will be "pipelined" (when possible) by sending multiple packets to the server before waiting for a response. This improves performance (by reducing latency) but is not compatible with some servers (most notably Amazon Aurora RDS). Set to <code>false</code> to disable this behavior.</td>
  </tr>
  <tr id="ServerRedirectionMode">
    <td>Server Redirection Mode, ServerRedirectionMode</td>
    <td>Disabled</td>
    <td><p>Whether to use server redirection. The options include:</p>
      <dl>
        <dt>Disabled</dt>
        <dd>Server redirection is not used. All connections go through the proxy server (if there is one).</dd>
        <dt>Preferred</dt>
        <dd>If the server supports redirection, a redirected connection will be attempted. If it’s successful, the redirected connection will be used; otherwise, the original connection will be used.</dd>
        <dt>Required</dt>
        <dd>The server must support redirection, and making a redirected connection must be successful; otherwise, an exception will be thrown.</dd>
      </dl>
      <p>Server Redirection is supported by Azure Database for MySQL if the <code>redirect_enabled</code> server parameter is set to <code>ON</code>.</p>
      <p>This option is only respected if <code>Pooling=True</code>.
    </td>
  </tr>
  <tr id="ServerRsaPublicKeyFile">
    <td>Server RSA Public Key File, ServerRsaPublicKeyFile</td>
    <td></td>
    <td>For <code>sha256_password</code> authentication. See comments under <code>AllowPublicKeyRetrieval</code>.</td>
  </tr>
  <tr id="ServerSPN">
    <td>Server SPN, ServerSPN</td>
    <td></td>
    <td>For MariaDB <a href="https://mariadb.com/kb/en/library/authentication-plugin-gssapi/"><code>auth_gssapi_client</code></a> authentication. Specifies the server’s Service Principal Name (to verify that authentication is occurring with the correct server).</td>
  </tr>
  <tr id="TreatTinyAsBoolean">
    <td>Treat Tiny As Boolean, TreatTinyAsBoolean</td>
    <td>true</td>
    <td>When set to <code>true</code>, <code>TINYINT(1)</code> values are returned as booleans. Setting this to <code>false</code> causes <code>TINYINT(1)</code> to be returned as sbyte/byte.</td>
  </tr>
  <tr id="UseAffectedRows">
    <td>Use Affected Rows, UseAffectedRows</td>
    <td>false</td>
    <td>When <code>false</code> (default), the connection reports found rows instead of changed (affected) rows. Set to <code>true</code> to report only the number of rows actually changed by <code>UPDATE</code> or <code>INSERT … ON DUPLICATE KEY UPDATE</code> statements.</td>
  </tr>
  <tr id="UseCompression">
    <td>Use Compression, Compress, UseCompression</td>
    <td>false</td>
    <td>If true (and if the server supports compression), compresses packets sent between client and server. This option is unlikely to be useful in
      practice unless there is a high-latency or low-bandwidth network link between the application and the database server. You should measure
      performance with and without this option to determine if it’s beneficial in your environment.</td>
  </tr>
  <tr id="UseXaTransactions">
    <td>Use XA Transactions, UseXaTransactions</td>
    <td>true</td>
    <td>When <code>true</code> (default), using <code>TransactionScope</code> or <code>MySqlConnection.EnlistTransaction</code>
    will use a <a href="https://dev.mysql.com/doc/refman/8.0/en/xa.html">XA Transaction</a>. This allows true
    distributed transactions, but may not be compatible with server replication; there are <a href="https://dev.mysql.com/doc/refman/8.0/en/xa-restrictions.html">other limitations</a>.
    When set to <code>false</code>, regular MySQL transactions are used, just like Connector/NET.</td>
  </tr>
</table>

## Unsupported Options

These options are used by Connector/NET but not supported by MySqlConnector. In general, they should be removed
from your connection string when migrating from Connector/NET to MySqlConnector.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Description</th>
  </thead>
  <tr id="AllowBatch">
    <td>AllowBatch, Allow Batch</td>
    <td>true</td>
    <td>MySqlConnector always allows batch statements.</td>
  </tr>
  <tr id="CacheServerProperties">
    <td>CacheServerProperties, Cache Server Properties</td>
    <td></td>
    <td>MySqlConnector doesn’t need this optimization.</td>
  </tr>
  <tr id="CheckParameters">
    <td>CheckParameters, Check Parameters</td>
    <td>true</td>
    <td>MySqlConnector always checks stored procedure parameters efficiently; there’s no need to disable this.</td>
  </tr>
  <tr id="CommandInterceptors">
    <td>CommandInterceptors, Command Interceptors</td>
    <td></td>
    <td>MySqlConnector doesn’t support this extensibility mechanism, which is not compatible with async operations.</td>
  </tr>
  <tr id="DnsSrv">
    <td>DnsSrv, Dns-Srv</td>
    <td></td>
    <td>MySqlConnector doesn’t support discovering server addresses from DNS SRV records.</td>
  </tr>
  <tr id="ExceptionInterceptors">
    <td>ExceptionInterceptors, Exception Interceptors</td>
    <td></td>
    <td>MySqlConnector doesn’t support this extensibility mechanism.</td>
  </tr>
  <tr id="FunctionsReturnString">
    <td>FunctionsReturnString, Functions Return String</td>
    <td>false</td>
    <td>Not supported. BLOBs are always returned as <code>byte[]</code>.</td>
  </tr>
  <tr id="IncludeSecurityAsserts">
    <td>IncludeSecurityAsserts, Include Security Asserts</td>
    <td>false</td>
    <td>Not supported. For partial trust environments.</td>
  </tr>
  <tr id="IntegratedSecurity">
    <td>IntegratedSecurity, Integrated Security</td>
    <td>false</td>
    <td>Windows authentication is not supported.</td>
  </tr>
  <tr id="Logging">
    <td>Logging</td>
    <td>false</td>
    <td>Use <a href="/overview/logging/">MySqlConnector logging</a> (which is more flexible) instead.</td>
  </tr>
  <tr id="OldGetStringBehavior">
    <td>OldGetStringBehavior</td>
    <td>false</td>
    <td>This option is temporary in Connector/NET and unsupported in MySqlConnector.</td>
  </tr>
  <tr id="OldSyntax">
    <td>OldSyntax, Old Syntax, UseOldSyntax, Use Old Syntax</td>
    <td>false</td>
    <td>This option is deprecated in Connector/NET and unsupported in MySqlConnector.</td>
  </tr>
  <tr id="Password1">
    <td>Password1, Password2, Password3</td>
    <td></td>
    <td>MySqlConnector does not currently support multifactor authentication; see <a href="https://github.com/mysql-net/MySqlConnector/issues/1068" title="Support multifactor authentication">issue 1068</a>.</td>
  </tr>
  <tr id="ProcedureCacheSize">
    <td>ProcedureCacheSize, Procedure Cache Size, ProcedureCache, Procedure Cache</td>
    <td></td>
    <td>MySqlConnector places no limit on the amount of stored procedure metadata that is cached. It takes a very small amount of memory.</td>
  </tr>
  <tr id="Replication">
    <td>Replication</td>
    <td></td>
    <td>Not supported.</td>
  </tr>
  <tr id="RespectBinaryFlags">
    <td>RespectBinaryFlags, Respect Binary Flags</td>
    <td>true</td>
    <td>The binary type of a column is always respected.</td>
  </tr>
  <tr id="SharedMemoryName">
    <td>SharedMemoryName, Shared Memory Name</td>
    <td>true</td>
    <td>Shared memory (on Windows) is not supported as a connection protocol.</td>
  </tr>
  <tr id="SshHostName">
    <td>SshHostName, SshPort, SshUserName, SshPassword, SshKeyFile, SshPassPhrase</td>
    <td></td>
    <td>Connecting via SSH isn’t built into MySqlConnector, but can be set up easily by following <a href="/tutorials/connect-ssh/" title="Connecting to MySQL Server with SSH from C#">these instructions</a>.</td>
  </tr>
  <tr id="SqlServerMode">
    <td>SqlServerMode, Sql Server Mode</td>
    <td>false</td>
    <td>Not supported.</td>
  </tr>
  <tr id="TreatBlobsAsUtf8">
    <td>TreatBlobsAsUtf8, Treat BLOBs as UTF8</td>
    <td>false</td>
    <td>Not supported. BLOBs are always returned as <code>byte[]</code>.</td>
  </tr>
  <tr id="UsePerformanceMonitory">
    <td>UsePerformanceMonitor, Use Performance Monitor, UserPerfMon, PerfMon</td>
    <td>false</td>
    <td>Not supported.</td>
  </tr>
  <tr id="UseUsageAdvisor">
    <td>UseUsageAdvisor, Use Usage Advisor, Usage Advisor</td>
    <td>false</td>
    <td>Not supported.</td>
  </tr>
</table>
