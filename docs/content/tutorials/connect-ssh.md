---
lastmod: 2021-07-05
date: 2019-07-30
menu:
  main:
    parent: tutorials
title: Connecting with SSH
customtitle: "Tutorial: Connecting to MySQL Server with SSH from C#"
weight: 15
---

# Connecting to MySQL Server with SSH from C\#

This tutorial demonstrates how to use SSH.NET and MySqlConnector to connect to a MySQL database over SSH.

The arguments below are defined as follows:

* `sshHostName`: (required) the host name or IP address of the SSH server
* `sshUserName`: (required) the user name on the SSH server
* `sshPassword`: (optional) the password of the SSH user
* `sshKeyFile`: (optional) the path to the private key file to use for SSH authentication; if specified, this option takes precedence over `sshPassword`
* `sshPassPhrase`: (optional) the passphrase to unlock the SSH private key file specified by `sshKeyFile`
* `sshPort`: (optional) the port of the SSH server (default `22`)
* `databaseServer`: (optional) the host name or IP address of the database server (relative to the SSH server, default `localhost`)
* `databasePort`: (optional) the port the MySQL server is listening on (default `3306`)

## Prerequisites

You must install the [Renci SSH.NET NuGet package](https://www.nuget.org/packages/SSH.NET/): `dotnet add package SSH.NET`

Define the following method in your C# code; this will set up the SSH connection:

```csharp
public static (SshClient SshClient, uint Port) ConnectSsh(string sshHostName, string sshUserName, string sshPassword = null,
	string sshKeyFile = null, string sshPassPhrase = null, int sshPort = 22, string databaseServer = "localhost", int databasePort = 3306)
{
	// check arguments
	if (string.IsNullOrEmpty(sshHostName))
		throw new ArgumentException($"{nameof(sshHostName)} must be specified.", nameof(sshHostName));
	if (string.IsNullOrEmpty(sshHostName))
		throw new ArgumentException($"{nameof(sshUserName)} must be specified.", nameof(sshUserName));
	if (string.IsNullOrEmpty(sshPassword) && string.IsNullOrEmpty(sshKeyFile))
		throw new ArgumentException($"One of {nameof(sshPassword)} and {nameof(sshKeyFile)} must be specified.");
	if (string.IsNullOrEmpty(databaseServer))
		throw new ArgumentException($"{nameof(databaseServer)} must be specified.", nameof(databaseServer));

	// define the authentication methods to use (in order)
	var authenticationMethods = new List<AuthenticationMethod>();
	if (!string.IsNullOrEmpty(sshKeyFile))
	{
		authenticationMethods.Add(new PrivateKeyAuthenticationMethod(sshUserName,
			new PrivateKeyFile(sshKeyFile, string.IsNullOrEmpty(sshPassPhrase) ? null : sshPassPhrase)));
	}
	if (!string.IsNullOrEmpty(sshPassword))
	{
		authenticationMethods.Add(new PasswordAuthenticationMethod(sshUserName, sshPassword));
	}

	// connect to the SSH server
	var sshClient = new SshClient(new ConnectionInfo(sshHostName, sshPort, sshUserName, authenticationMethods.ToArray()));
	sshClient.Connect();

	// forward a local port to the database server and port, using the SSH server
	var forwardedPort = new ForwardedPortLocal("127.0.0.1", databaseServer, (uint) databasePort);
	sshClient.AddForwardedPort(forwardedPort);
	forwardedPort.Start();

	return (sshClient, forwardedPort.BoundPort);
}
```

## Example Use

Note that these examples dispose `sshClient`, which shuts down the forwarded port. In practice, you will
want to keep the `SshClient` and forwarded port alive for the lifetime of your application.

### If MySQL and SSH Server are the same

If the MySQL Server and SSH Server are running on the same computer, use the following C# code:

```csharp
var server = "your db & ssh server";
var sshUserName = "your SSH user name";
var sshPassword = "your SSH password";
var databaseUserName = "your database user name";
var databasePassword = "your database password";

var (sshClient, localPort) = ConnectSsh(server, sshUserName, sshPassword);
using (sshClient)
{
	MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder
	{
		Server = "127.0.0.1",
		Port = localPort,
		UserID = databaseUserName,
		Password = databasePassword,
	};

	using var connection = new MySqlConnection(csb.ConnectionString);
	connection.Open();
}
```

### If MySQL and SSH Server are different

If the MySQL Server and SSH Server are running on different computers (and the MySQL Server
is reachable from the SSH Server, but not from the client computer), use the following C# code:

```csharp
var sshServer = "your ssh server";
var sshUserName = "your SSH user name";
var sshPassword = "your SSH password";
var databaseServer = "your database server";
var databaseUserName = "your database user name";
var databasePassword = "your database password";

var (sshClient, localPort) = ConnectSsh(sshServer, sshUserName, sshPassword, databaseServer: databaseServer);
using (sshClient)
{
	MySqlConnectionStringBuilder csb = new MySqlConnectionStringBuilder
	{
		Server = "127.0.0.1",
		Port = localPort,
		UserID = databaseUserName,
		Password = databasePassword,
	};

	using var connection = new MySqlConnection(csb.ConnectionString);
	connection.Open();
}
```