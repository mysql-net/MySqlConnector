---
lastmod: 2016-10-16
date: 2016-10-16
title: Connection Options
weight: 30
menu:
  main:
    pre: "<i class='fa fa-bolt'></i>"
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
    <th style="width: 70%">Descriotion</th>
  </thead>
  <tr>
    <td>Host, Server, Data Source, DataSource, Address, Addr, Network Address</td>
    <td>localhost</td>
    <td>The name or network address of the instance of MySQL to which to connect. Multiple hosts can be specified separated by commas. This can be useful where multiple MySQL servers are configured for replication and you are not concerned about the precise server you are connecting to. No attempt is made by the provider to synchronize writes to the database, so take care when using this option. In Unix environment with Mono, this can be a fully qualified path to a MySQL socket file. With this configuration, the Unix socket is used instead of the TCP/IP socket. Currently, only a single socket name can be given, so accessing MySQL in a replicated environment using Unix sockets is not currently supported.</td>
  </tr>
    <tr>
    <td>Port</td>
    <td>3306</td>
    <td>The port MySQL is using to listen for connections.</td>
  </tr>
  <tr>
    <td>User Id, UserID, Username, Uid, User name, User</td>
    <td></td>
    <td>The MySQL login account being used.</td>
  </tr>
  <tr>
    <td>Password, pwd</td>
    <td></td>
    <td>The password for the MySQL account being used.</td>
  </tr>
  <tr>
    <td>Database, Initial Catalog</td>
    <td></td>
    <td>The case-sensitive name of the database to use initially.  This is not required.</td>
  </tr>
</table>

SSL/TLS Options
-----------

These are the options that need to be used in order to configure a connection to use SSL/TLS.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Descriotion</th>
  </thead>
  <tr>
    <td>SSL Mode, SslMode</td>
    <td>None</td>
    <td>This option has the following values:
      <ul>
        <li><b>None</b> - do not use SSL.</li>
        <li><b>Required</b> - Always use SSL. Deny connection if server does not support SSL.  Do not validate CA or hostname.</li>
        <li><b>VerifyCA</b> - Always use SSL. Validate the CA but tolerate hostname mismatch.</li>
        <li><b>VerifyFull</b> - Always use SSL. Validate CA and hostname.</li>
      </ul>
    </td>
  </tr>
  <tr>
    <td>Certificate File, CertificateFile</td>
    <td></td>
    <td>This option specifies the path to a certificate file in PKCS #12 format (.pfx). </td>
  </tr>
  <tr>
    <td>Certificate Password, CertificatePassword	</td>
    <td></td>
    <td>Specifies a password that is used in conjunction with a certificate specified using the option CertificateFile.  Not required if the certificate file is not password protected.</td>
  </tr>
</table>

Connection Pooling Options
--------------------------

Connection pooling is enabled by default.  These options are used to configure it.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Descriotion</th>
  </thead>
  <tr>
    <td>Pooling</td>
    <td>true</td>
    <td>When true, the MySqlConnection object is drawn from the appropriate pool, or if necessary, is created and added to the appropriate pool. Recognized values are true, false, yes, and no.</td>
  </tr>
  <tr>
    <td>Connection Reset, ConnectionReset	</td>
    <td>false</td>
    <td>If true, the connection state is reset when it is retrieved from the pool. The default value of false avoids making an additional server round trip when obtaining a connection, but the connection state is not reset.</td>
  </tr>
  <tr>
    <td>Maximum Pool Size, Max Pool Size, MaximumPoolsize, maxpoolsize</td>
    <td>100</td>
    <td>The maximum number of connections allowed in the pool.</td>
  </tr>
</table>

Other Options
-------------

These are the other options that MySqlConnector supports.  They are set to sensible defaults and typically do not need to be tweaked.

<table class="table table-striped table-hover">
  <thead>
    <th style="width: 20%">Name</th>
    <th style="width: 10%">Default</th>
    <th style="width: 70%">Descriotion</th>
  </thead>
  <tr>
    <td>AllowUserVariables</td>
    <td>true</td>
    <td>Setting this to true indicates that the provider expects user variables in the SQL.</td>
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
    <td>Use Affected Rows, UseAffectedRows</td>
    <td>true</td>
    <td>When false, the connection reports found rows instead of changed (affected) rows.</td>
  </tr>
</table>
