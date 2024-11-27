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

### Migrating from Global Logging

<blockquote class="highlight">
In MySqlConnector 2.4.0, the <tt>MySqlConnectorLogManager.Provider</tt> API has been deprecated.
Follow these instructions to migrate to the new logging API.
</blockquote>

With the deprecated logging framework, you may have had code that looked like this:

```csharp
log4net.Config.XmlConfigurator.Configure();
MySqlConnectorLogManager.Provider = new Log4netLoggerProvider();
using var connection = new MySqlConnection(connectionString);
```

To migrate to the new logging API, you will need to:

* Create a `LoggerFactory`
* Connect that `LoggerFactory` to your desired logging framework
* Create a `MySqlDataSource` configurated with that `LoggerFactory`
* Create new `MySqlConnection` objects using `MySqlDataSource.CreateConnection()` (or `OpenConnection[Async]()`).

This will look like the following, depending on your exact configuration:

```csharp
// create a LoggerFactory and configure it with the desired logging framework
// use ONLY ONE of the "Add" methods below, depending on your logging framework
var loggerFactory = LoggerFactory.Create(builder =>
  // if you just want console logging
  builder.AddConsole();

  // connect to log4net via Microsoft.Extensions.Logging.Log4Net.AspNetCore
  builder.AddLog4Net(new Log4NetProviderOptions
  {
    UseWebOrAppConfig = true, // set this if you're storing your settings in Web.config instead of log4net.config
    ExternalConfigurationSetup = true, // set this instead if you're initializing log4net yourself
    // see other options at https://github.com/huorswords/Microsoft.Extensions.Logging.Log4Net.AspNetCore
  }));

  // connect to NLog via NLog.Extensions.Logging
  builder.AddNLog();

  // connect to Serilog via Serilog.Extensions.Logging
  builder.AddSerilog(dispose: true);
);

// now create a MySqlDataSource and configure it with the LoggerFactory
using var dataSource = new MySqlDataSourceBuilder(yourConnectionString)
  .UseLoggerFactory(loggerFactory)
  .Build();

// create all MySqlConnection objects via the MySqlDataSource, not directly
// DON'T: using var connection = new MySqlConnection(yourConnectionString);
using var connection = dataSource.CreateConnection();

// you can also create open connections
using var connection = await dataSource.OpenConnectionAsync();
```

### Deprecated Logging Framework

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
