---
lastmod: 2017-11-06
date: 2016-10-16
title: Connection Options
weight: 30
menu:
  main:
---

Connection Options
==================

MySqlConnector supports a subset of Oracle's [Connector/NET connection options](https://dev.mysql.com/doc/connector-net/en/connector-net-connection-options.html).

Base Options
------------

These are the basic options that need to be defined to connect to a MySQL database.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Description</th>
  </thead>
  <tr>
    <td>Host, Server, Data Source, DataSource, Address, Addr, Network Address</td>
    <td>localhost</td>
    <td>The host name or network address of the MySQL Server to which to connect. Multiple hosts can be specified in a comma-delimited list.<br>On Unix-like systems, this can be a fully qualified path to a MySQL socket file, which will cause a Unix socket to be used instead of a TCP/IP socket. Only a single socket name can be specified.</td>
  </tr>
    <tr>
    <td>Port</td>
    <td>3306</td>
    <td>The TCP port on which MySQL Server is listening for connections.</td>
  </tr>
  <tr>
    <td>User Id, UserID, Username, Uid, User name, User</td>
    <td></td>
    <td>The MySQL user ID.</td>
  </tr>
  <tr>
    <td>Password, pwd</td>
    <td></td>
    <td>The password for the MySQL user.</td>
  </tr>
  <tr>
    <td>Database, Initial Catalog</td>
    <td></td>
    <td>(Optional) The case-sensitive name of the initial database to use. This may be required if the MySQL user account only has access rights to particular databases on the server.</td>
  </tr>
</table>

SSL/TLS Options
-----------

These are the options that need to be used in order to configure a connection to use SSL/TLS.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Description</th>
  </thead>
  <tr>
    <td>SSL Mode, SslMode</td>
    <td>Preferred</td>
    <td>This option has the following values:
      <ul>
        <li><b>Preferred</b> - (this is the default). Use SSL if the server supports it.</li>
        <li><b>None</b> - Do not use SSL.</li>
        <li><b>Required</b> - Always use SSL. Deny connection if server does not support SSL. Does not validate CA or hostname.</li>
        <li><b>VerifyCA</b> - Always use SSL. Validates the CA but tolerates hostname mismatch.</li>
        <li><b>VerifyFull</b> - Always use SSL. Validates CA and hostname.</li>
      </ul>
    </td>
  </tr>
  <tr>
    <td>Certificate File, CertificateFile</td>
    <td></td>
    <td>Specifies the path to a certificate file in PKCS #12 (.pfx) format containing a bundled Certificate and Private Key used for Mutual Authentication.  To create a PKCS #12 bundle from a PEM encoded Certificate and Key, use <code>openssl pkcs12 -in cert.pem -inkey key.pem -export -out bundle.pfx</code></td>
  </tr>
  <tr>
    <td>Certificate Password, CertificatePassword</td>
    <td></td>
    <td>Specifies the password for the certificate specified using the <code>CertificateFile</code> option. Not required if the certificate file is not password protected.</td>
  </tr>
  <tr>
    <td>CA Certificate File, CACertificateFile</td>
    <td></td>
    <td>This option specifies the path to a CA certificate file in a PEM Encoded (.pem) format. This should be used in with <code>SslMode=VerifyCA</code> or <code>SslMode=VerifyFull</code> to enable verification of a CA certificate that is not trusted by the Operating System's certificate store.</td>
  </tr>
</table>

Connection Pooling Options
--------------------------

Connection pooling is enabled by default. These options are used to configure it.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Description</th>
  </thead>
  <tr>
    <td>Pooling</td>
    <td>true</td>
    <td>Enables connection pooling. When pooling is enabled, <code>MySqlConnection.Open</code> retrieves an open connection from the pool if one is available (opening a new connection if not), and <code>Close</code>/<code>Dispose</code> returns the open connection to the pool.</td>
  </tr>
  <tr>
    <td>Connection Lifetime, ConnectionLifeTime</td>
    <td>0</td>
    <td>Controls the maximum length of time a connection to the server can be open. Connections that are returned to the pool are destroyed if it's been more than <code>ConnectionLifeTime</code> seconds since the connection was created. The default value of zero (0) means pooled connections will never incur a ConnectionLifeTime timeout.</td>
  </tr>
  <tr>
    <td>Connection Reset, ConnectionReset</td>
    <td><code>true</code></td>
    <td>If <code>true</code>, the connection state is reset when it is retrieved from the pool. The default value of <code>true</code> ensures that the connection is in the same state whether it's newly created or retrieved from the pool. A value of <code>false</code> avoids making an additional server round trip when obtaining a connection, but the connection state is not reset, meaning that session variables and other session state changes from any previous use of the connection are carried over.</td>
  </tr>
  <tr>
    <td>Connection Idle Ping Time, Connection Idle Ping Time <em>(Experimental)</em></td>
    <td>0</td>
    <td>When a connection is retrieved from the pool, and <code>ConnectionReset</code> is <code>false</code>, the server
      will be pinged if the connection has been idle in the pool for longer than <code>ConnectionIdlePingTime</code> seconds.
      If pinging the server fails, a new connection will be opened automatically by the connection pool. This ensures that the
      <code>MySqlConnection</code> is in a valid, open state after the call to <code>Open</code>/<code>OpenAsync</code>,
      at the cost of an extra server roundtrip. For high-performance scenarios, you may wish to set <code>ConnectionIdlePingTime</code>
      to a non-zero value to make the connection pool assume that recently-returned connections are still open. If the
      connection is broken, it will throw from the first call to <code>ExecuteNonQuery</code>, <code>ExecuteReader</code>,
      etc.; your code should handle that failure and retry the connection. This option has no effect if <code>ConnectionReset</code>
      is <code>true</code>, as that will cause a connection reset packet to be sent to the server, making ping redundant.
  </tr>
  <tr>
    <td>Connection Idle Timeout, ConnectionIdleTimeout</td>
    <td>180</td>
    <td>The amount of time (in seconds) that a connection can remain idle in the pool. Any connection above <code>MinimumPoolSize</code> connections that is idle for longer than <code>ConnectionIdleTimeout</code> is subject to being closed by a background task. The background task runs every minute, or half of <code>ConnectionIdleTimeout</code>, whichever is more frequent. A value of zero (0) means pooled connections will never incur a ConnectionIdleTimeout, and if the pool grows to its maximum size, it will never get smaller.</td>
  </tr>
  <tr>
    <td>Maximum Pool Size, Max Pool Size, MaximumPoolsize, maxpoolsize</td>
    <td>100</td>
    <td>The maximum number of connections allowed in the pool.</td>
  </tr>
  <tr>
    <td>Minimum Pool Size, Min Pool Size, MinimumPoolSize, minpoolsize</td>
    <td>0</td>
    <td>The minimum number of connections to leave in the pool if ConnectionIdleTimeout is reached.</td>
  </tr>
</table>

Other Options
-------------

These are the other options that MySqlConnector supports. They are set to sensible defaults and typically do not need to be tweaked.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Description</th>
  </thead>
  <tr>
    <td>AllowPublicKeyRetrieval, Allow Public Key Retrieval</td>
    <td>false</td>
    <td>If the user account uses <code>sha256_password</code> authentication, the password must be protected during transmission; TLS is the preferred mechanism for this,
      but if it is not available then RSA public key encryption will be used. To specify the server's RSA public key, use the <code>ServerRSAPublicKeyFile</code> connection
      string setting, or set <code>AllowPublicKeyRetrieval=True</code> to allow the client to automatically request the public key from the server. Note that <code>AllowPublicKeyRetrieval=True</code>
      could allow a malicious proxy to perform a MITM attack to get the plaintext password, so it is <code>False</code> by default and must be explicitly enabled.</td>
  </tr>
  <tr>
    <td>AllowUserVariables, Allow User Variables</td>
    <td>false</td>
    <td>Allows user-defined variables (prefixed with <code>@</code>) to be used in SQL statements. The default value (<code>false</code>)
    only allows <code>@</code>-prefixed names to refer to command parameters.</td>
  </tr>
  <tr>
    <td>Compress, Use Compression, UseCompression</td>
    <td>false</td>
    <td>If true (and if the server supports compression), compresses packets sent between client and server. This option is unlikely to be useful in
      practice unless there is a high-latency or low-bandwidth network link between the application and the database server. You should measure
      performance with and without this option to determine if it's beneficial in your environment.</td>
  </tr>
  <tr>
    <td>Connect Timeout, Connection Timeout, ConnectionTimeout</td>
    <td>15</td>
    <td>The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.</td>
  </tr>
  <tr>
    <td>Convert Zero Datetime, ConvertZeroDateTime</td>
    <td>false</td>
    <td>True to have MySqlDataReader.GetValue() and MySqlDataReader.GetDateTime() return DateTime.MinValue for date or datetime columns that have disallowed values.</td>
  </tr>
  <tr>
    <td>Default Command Timeout, Command Timeout, DefaultCommandTimeout</td>
    <td>30</td>
    <td>The length of time (in seconds) each command can execute before timing out and throwing an exception, or zero to disable timeouts.
      See the note in the <a href="https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlcommand.commandtimeout">Microsoft documentation</a>
      for more explanation of how this is determined.</td>
  </tr>
  <tr>
    <td>Keep Alive, Keepalive</td>
    <td>0</td>
    <td>TCP Keepalive idle time. A value of 0 indicates that the OS Default keepalive settings are used.
    On Windows, a value greater than 0 is the idle connection time, measured in seconds, before the first keepalive packet is sent.
    Due to limitations in .NET Core, Unix-based Operating Systems will always use the OS Default keepalive settings.</td>
  </tr>
  <tr>
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
        <dd>Servers are tried in ascending order of number of currently-open connections in this connection pool. Requires <code>Pooling=True</code>.
      </dl>
  </tr>
  <tr>
    <td>Old Guids, OldGuids</td>
    <td>false</td>
    <td> The backend representation of a GUID type was changed from BINARY(16) to CHAR(36). This was done to allow developers to use the server function UUID() to populate a GUID table - UUID() generates a 36-character string. Developers of older applications can add 'Old Guids=true' to the connection string to use a GUID of data type BINARY(16).</td>
  </tr>
  <tr>
    <td>Persist Security Info, PersistSecurityInfo</td>
    <td>false</td>
    <td>When set to false or no (strongly recommended), security-sensitive information, such as the password, is not returned as part of the connection if the connection is open or has ever been in an open state. Resetting the connection string resets all connection string values, including the password. Recognized values are true, false, yes, and no.</td>
  </tr>
  <tr>
    <td>ServerRSAPublicKeyFile, Server RSA Public Key File</td>
    <td></td>
    <td>For <code>sha256_password</code> authentication. See comments under <code>AllowPublicKeyRetrieval</code>.</td>
  </tr>
  <tr>
    <td>Treat Tiny As Boolean, TreatTinyAsBoolean</td>
    <td>true</td>
    <td>When set to true, tinyint(1) values are returned as booleans. Setting this to false causes tinyint(1) to be returned as sbyte/byte.</td>
  </tr>
  <tr>
    <td>Use Affected Rows, UseAffectedRows</td>
    <td>true</td>
    <td>When false, the connection reports found rows instead of changed (affected) rows.</td>
  </tr>
</table>
