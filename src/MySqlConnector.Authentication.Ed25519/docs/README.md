## About

This package implements the following authentication plugins for MariaDB:

* [ed25519](https://mariadb.com/docs/server/reference/plugins/authentication-plugins/authentication-plugin-ed25519)
* [PARSEC](https://mariadb.com/docs/server/reference/plugins/authentication-plugins/authentication-plugin-parsec)

## How to Use

Call either of the following methods from your application startup code to enable the corresponding authentication plugin:

* `Ed25519AuthenticationPlugin.Install()`
* `ParsecAuthenticationPlugin.Install()`
