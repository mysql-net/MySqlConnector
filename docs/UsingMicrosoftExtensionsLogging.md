## How to Use

To integrate MySqlConnector with Microsoft.Extensions.Logging, add the following line of code to `Program.cs` method (before any `MySqlConnector` objects have been used):

```csharp
app.Services.UseMySqlConnectorLogging();
```

Alternatively, obtain an `ILoggerFactory` through dependency injection and add:

```csharp
MySqlConnectorLogManager.Provider = new MicrosoftExtensionsLoggingLoggerProvider(loggerFactory);
```
