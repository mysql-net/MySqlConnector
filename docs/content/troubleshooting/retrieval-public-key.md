---
lastmod: 2019-07-08
date: 2019-07-07
title: Retrieval of Public Key
weight: 40
menu:
  main:
    parent: troubleshooting
---

# Retrieval of the RSA public key is not enabled for insecure connections

## Problem

When connecting to MySQL Server from a C# program, you may receive one of the following errors:

* **MySqlException (0x80004005): Retrieval of the RSA public key is not enabled for insecure connections.** (Connector/NET)
* **Authentication method 'caching_sha2_password' failed. Either use a secure connection, specify the server's RSA public key with ServerRSAPublicKeyFile, or set AllowPublicKeyRetrieval=True.** (MySqlConnector)
* **Authentication method 'sha256_password' failed. Either use a secure connection, specify the server's RSA public key with ServerRSAPublicKeyFile, or set AllowPublicKeyRetrieval=True.** (MySqlConnector)

## Fix

Use one of the following fixes. (Note: if using `MySql.Data` (Connector/NET), uninstall it first then [install MySqlConnector](/overview/installing).)

* (Preferred) Use a secure connection by adding `;SslMode=Required` to the connection string.
* Specify the (local) path that contains a copy of the server’s public key by adding `;ServerRSAPublicKeyFile=path/to/file.pem` to the connection string. To retrieve the server’s public key, connect securely to the server and execute the following query, saving the results in a file: `SHOW STATUS LIKE 'Caching_sha2_password_rsa_public_key';`
* (Not recommended) Automatically retrieve the server’s public key by adding `;AllowPublicKeyRetrieval=true` to the connection string; this is potentially insecure.

## Background

MySQL Server 5.7 added the [`sha256_password` authentication plugin](https://dev.mysql.com/doc/refman/8.0/en/sha256-pluggable-authentication.html).
MySQL Server 8.0 adds the [`caching_sha2_password` authentication plugin](https://dev.mysql.com/doc/refman/8.0/en/caching-sha2-pluggable-authentication.html)
and makes it the default. These plugins use RSA public key encryption to protect the user's password in transit.

As the [MySQL Server Team writes](http://mysqlserverteam.com/protecting-mysql-passwords-with-the-sha256_password-plugin/):

> Distributing keys securely can be an operational headache. MySQL Server will supply its own RSA public key upon request from the client, so that the key doesn’t have to be explicitly distributed and configured for each client. But this introduces another security concern – a proxy in the middle may substitute an RSA public key for which it has the private key, decrypt and harvest the plain-text password, then re-encrypting the password with the actual server RSA public key for the connection attempt to continue. For this reason, it’s strongly recommended that clients define a local RSA public key to use instead of request the server RSA key during the handshake.

By default, client libraries will not send the password unless a secure connection (using TLS or RSA public key encryption) can
be established. To avoid a MITM attack, the RSA public key will not be sent in plain text. For Connector/NET, you can use TLS (`SslMode=Required`)
to protect the RSA public key. With MySqlConnector, you also have the option of specifying the server’s public key directly,
using the `ServerRSAPublicKeyFile` option, or allowing a potentially-insecure connection by using `AllowPublicKeyRetrieval=true`.

## Further Reading

* [Protecting MySQL Passwords With the sha256_password Plugin](http://mysqlserverteam.com/protecting-mysql-passwords-with-the-sha256_password-plugin/)
