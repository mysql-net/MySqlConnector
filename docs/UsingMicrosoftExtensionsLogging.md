## How to Use

> [!WARNING]
> The following information is for MySqlConnector versions prior to 2.3.0.
> The global logging interface that is described is deprecated and shouldn't be used in new code.
> For current logging guidance, see [Logging in MySqlConnector](https://mysqlconnector.net/diagnostics/logging/).

To integrate MySqlConnector with Microsoft.Extensions.Logging, add the following line of code to `Program.cs` method (before any `MySqlConnector` objects have been used):

```csharp
app.Services.UseMySqlConnectorLogging();
```

Alternatively, obtain an `ILoggerFactory` through dependency injection and add:

```csharp
MySqlConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
```
