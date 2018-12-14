---
date: 2018-01-20
menu:
  main:
    parent: getting started
title: Logging
weight: 50
---

Logging
=======

MySqlConnector implements a custom logging framework with the `IMySqlConnectorLogger` and `IMySqlConnectorLoggerProvider` interfaces.
To connect MySqlConnector to an existing logging framework, write an implementation of `IMySqlConnectorLoggerProvider` that adapts
the existing logging framework, and install it by setting `MySqlConnector.Logging.MySqlConnectorLogManager.Provider = provider;`.

The `MySqlConnectorLogManager.Provider` property may only be set once, and must be set before any other MySqlConnector library methods are called.

Debug-level logging is useful for diagnosing problems with MySqlConnector itself; it is recommend that applications limit MySqlConnector
logging to Info or higher.

### Existing Logging Providers

There are NuGet packages that adapt MySqlConnector logging for popular logging frameworks.

#### log4net

Install [MySqlConnector.Logging.log4net](https://www.nuget.org/packages/MySqlConnector.Logging.log4net/).

Add the following line of code to your application startup routine:

```csharp
MySqlConnectorLogManager.Provider = new Log4netLoggerProvider();
```

To reduce the verbosity of MySqlConnector logging, add the following element to your log4net config:

```xml
<log4net>
  ...
  <logger name="MySqlConnector">
    <level value="WARN" /> <!-- or "INFO" -->
  </logger>
```

#### Microsoft.Extensions.Logging

Install [MySqlConnector.Logging.Microsoft.Extensions.Logging](https://www.nuget.org/packages/MySqlConnector.Logging.Microsoft.Extensions.Logging/).

Add the following line of code to your `Startup.Configure` method:

```csharp
MySqlConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
```

#### Serilog

Install [MySqlConnector.Logging.Serilog](https://www.nuget.org/packages/MySqlConnector.Logging.Serilog/).

Add the following line of code to your application startup routine:

```csharp
MySqlConnectorLogManager.Provider = new SerilogLoggerProvider(loggerFactory);
```
