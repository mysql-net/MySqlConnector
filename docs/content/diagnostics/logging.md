---
lastmod: 2023-11-11
date: 2018-01-20
menu:
  main:
    parent: diagnostics
title: Logging
weight: 10
---

# Logging

MySqlConnector 2.3.0 and later supports logging through the standard [Microsoft.Extensions.Logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging) interfaces.

## Console Programs

To set up logging in MySqlConnector, create your `ILoggerFactory` as usual, and then configure a `MySqlDataSource` with it.
Any use of connections handed out by the data source will log via your provided logger factory.

The following shows a minimal console application logging to the console via [Microsoft.Extensions.Logging.Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console):

```csharp
using Microsoft.Extensions.Logging;
using MySqlConnector;

// Create a Microsoft.Extensions.Logging LoggerFactory, configuring it with the providers,
// log levels and other desired configuration.
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

// Create a MySqlDataSourceBuilder, configuring it with our LoggerFactory.
var dataSourceBuilder = new MySqlDataSourceBuilder("Host=localhost;User ID=root;Password=pass");
dataSourceBuilder.UseLoggerFactory(loggerFactory);
await using var dataSource = dataSourceBuilder.Build();

// Any connections handed out by the data source will log via the LoggerFactory:
await using var connection = await dataSource.OpenConnectionAsync();
await using var command = new MySqlCommand("SELECT 1", connection);
_ = await command.ExecuteScalarAsync();
```

## ASP.NET and Dependency Injection

If you're using ASP.NET, you can use the additional [MySqlConnector.DependencyInjection package](https://www.nuget.org/packages/MySqlConnector.DependencyInjection), which provides seamless integration with dependency injection and logging:

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole();
builder.Services.AddMySqlDataSource("Host=localhost;User ID=root;Password=pass");
```

The `AddMySqlDataSource` method registers a data source with the DI container.
This data source automatically uses the logger factory configured by ASP.NET by default.
This allows your endpoints to get injected with MySQL connections which log to the application's logger factory.

## Global Logging

<blockquote class="warning">
The following information is for MySqlConnector versions prior to 2.3.0.
The global logging interface that is described is deprecated and shouldn't be used in new code.
</blockquote>

MySqlConnector implements a custom logging framework with the `IMySqlConnectorLogger` and `IMySqlConnectorLoggerProvider` interfaces.
To connect MySqlConnector to an existing logging framework, write an implementation of `IMySqlConnectorLoggerProvider` that adapts
the existing logging framework, and install it by setting `MySqlConnector.Logging.MySqlConnectorLogManager.Provider = provider;`.

The `MySqlConnectorLogManager.Provider` property may only be set once, and must be set before any other MySqlConnector library methods are called.

Trace-level logging is useful for diagnosing problems with MySqlConnector itself; it is recommended that applications limit MySqlConnector
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

#### NLog

Install [MySqlConnector.Logging.NLog](https://www.nuget.org/packages/MySqlConnector.Logging.NLog/).

Add the following line of code to your application startup routine:

```csharp
MySqlConnectorLogManager.Provider = new NLogLoggerProvider();
```

#### Serilog

Install [MySqlConnector.Logging.Serilog](https://www.nuget.org/packages/MySqlConnector.Logging.Serilog/).

Add the following line of code to your application startup routine:

```csharp
MySqlConnectorLogManager.Provider = new SerilogLoggerProvider();
```
