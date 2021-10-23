## About

This package integrates MySqlConnector logging with the Microsoft.Extensions.Logging framework.

## How to Use

Add the following line of code to `Program.cs` method (before any `MySqlConnector` objects have been used):

```csharp
app.Services.UseMySqlConnectorLogging();
```

Alternatively, obtain an `ILoggerFactory` through dependency injection and add:

```csharp
MySqlConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
```
