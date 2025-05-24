## About

This package implements the following authentication plugins for MariaDB:

* [`client_ed25519`](https://mariadb.com/kb/en/authentication-plugin-ed25519/).
* [PARSEC](https://mariadb.com/kb/en/authentication-plugin-parsec/)

## How to Use

Call either of the following methods from your application startup code to enable the corresponding authentication plugin:

* `Ed25519AuthenticationPlugin.Install()`
* `ParsecAuthenticationPlugin.Install()`
